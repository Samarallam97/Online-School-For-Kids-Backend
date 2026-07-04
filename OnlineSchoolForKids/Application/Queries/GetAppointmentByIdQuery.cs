using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using MediatR;


namespace Application.Queries;

public record GetAppointmentByIdQuery(string id, string UserId) : IRequest<AppointmentDto>;

public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IUserRepository _userRepo;

    public GetAppointmentByIdQueryHandler(
        IAppointmentRepository appointmentRepo,
        IUserRepository userRepo)
    {
        _appointmentRepo = appointmentRepo;
        _userRepo = userRepo;
    }

    public async Task<AppointmentDto> Handle(
        GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(request.UserId, cancellationToken);

        var specialist = await _userRepo.GetByIdAsync(appointment.SpecialistId, cancellationToken);
        var student = await _userRepo.GetByIdAsync(appointment.StudentId, cancellationToken);






        return new AppointmentDto
        {
            Id = appointment.Id,
            SpecialistId = appointment.SpecialistId,
            SpecialistName = specialist.FullName,
            SpecialistProfilePictureUrl = specialist.ProfilePictureUrl,
            StudentId = appointment.StudentId,
            StudentName = student.FullName,
            Title = appointment.Title,
            Description = appointment.Description,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status.ToString(),
            GoogleMeetLink = appointment.GoogleMeetLink,
            CanCancel = appointment.CanCancel(),
            AmountPaid = appointment.AmountPaid,
            CancellationReason = appointment.CancellationReason,
            HoldExpiresAtUtc = appointment.HoldExpiresAtUtc.ToString("o"),
            HourlyRate = specialist.HourlyRate,
        };
    }
}