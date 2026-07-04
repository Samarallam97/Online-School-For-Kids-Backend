using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands;

public record BookSessionCommand(
    string StudentId,
    string SpecialistId,
    string Title,
    string? Description,
    string AppointmentDate,   // "yyyy-MM-dd"
    string StartTime,         // "HH:mm"
    string EndTime
) : IRequest<BookSessionResult>;

public class BookSessionCommandHandler : IRequestHandler<BookSessionCommand, BookSessionResult>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IUserRepository _userRepo;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BookSessionCommandHandler(
        IAppointmentRepository appointmentRepo,
        IUserRepository userRepo,
        IStringLocalizer<SharedResource> localizer)
    {
        _appointmentRepo = appointmentRepo;
        _userRepo = userRepo;
        _localizer = localizer;

    }

    public async Task<BookSessionResult> Handle(
        BookSessionCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate specialist exists
        var specialist = await _userRepo.GetByIdAsync(request.SpecialistId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["SpecialistNotFound"]);

        // 2. Validate the requested time falls inside the specialist's availability
        var dayName = DateTime.Parse(request.AppointmentDate).DayOfWeek.ToString(); // "Monday" etc.
        var hasAvailability = (specialist.Availability ?? []).Any(s =>
            s.Day.Equals(dayName, StringComparison.OrdinalIgnoreCase) &&
            string.Compare(s.StartTime, request.StartTime, StringComparison.Ordinal) <= 0 &&
            string.Compare(s.EndTime, request.EndTime, StringComparison.Ordinal) >= 0);

        if (!hasAvailability)
            throw new InvalidOperationException(
                _localizer["TimeOutsideAvailability"]);

        // 3. Check for slot conflicts (Pending or Confirmed only — Cancelled slots are free)
        var hasConflict = await _appointmentRepo.HasConflictAsync(
            request.SpecialistId,
            request.AppointmentDate,
            request.StartTime,
            request.EndTime,
            cancellationToken);

        if (hasConflict)
            throw new InvalidOperationException(_localizer["TimeSlotAlreadyBooked"]);

        // 4. Create the appointment in Pending state with a 30-min hold
        var appointment = new Appointment
        {
            SpecialistId = request.SpecialistId,
            StudentId = request.StudentId,
            Title = request.Title,
            Description = request.Description,
            AppointmentDate = request.AppointmentDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = Domain.Enums.AppointmentStatus.Pending,
            HoldExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
        };

        var created = await _appointmentRepo.CreateAsync(appointment, cancellationToken);
        return new BookSessionResult(created.Id, created.Status.ToString());
    }
}

public record BookSessionResult(string Id, string Status);
