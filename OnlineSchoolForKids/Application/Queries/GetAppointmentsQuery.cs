using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using MediatR;

namespace Application.Queries;

public record GetMyAppointmentsQuery(string UserId) : IRequest<List<AppointmentDto>>;

public class GetMyAppointmentsQueryHandler : IRequestHandler<GetMyAppointmentsQuery, List<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IUserRepository _userRepo;

    public GetMyAppointmentsQueryHandler(
        IAppointmentRepository appointmentRepo,
        IUserRepository userRepo)
    {
        _appointmentRepo = appointmentRepo;
        _userRepo = userRepo;
    }

    public async Task<List<AppointmentDto>> Handle(
        GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepo.GetByStudentIdAsync(request.UserId, cancellationToken);

        var specialistIds = appointments.Select(a => a.SpecialistId).Distinct().ToList();
        var specialistNames = new Dictionary<string, string>();
        var specialistPics = new Dictionary<string, string?>();
        var specialistRates = new Dictionary<string, decimal?>();

        foreach (var id in specialistIds)
        {
            var u = await _userRepo.GetByIdAsync(id, cancellationToken);
            if (u is not null)
            {
                specialistNames[id] = u.FullName;
                specialistPics[id] = u.ProfilePictureUrl;
                specialistRates[id] = u.HourlyRate;
            }
        }

        return appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            SpecialistId = a.SpecialistId,
            SpecialistName = specialistNames.GetValueOrDefault(a.SpecialistId, ""),
            SpecialistProfilePictureUrl = specialistPics.GetValueOrDefault(a.SpecialistId),
            StudentId = a.StudentId,
            StudentName = "",
            Title = a.Title,
            Description = a.Description,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Status = a.Status.ToString(),
            GoogleMeetLink = a.GoogleMeetLink,
            CanCancel = a.CanCancel(),
            AmountPaid = a.AmountPaid,
            CancellationReason = a.CancellationReason,
            HoldExpiresAtUtc = a.HoldExpiresAtUtc.ToString("o"),
            HourlyRate = specialistRates.GetValueOrDefault(a.SpecialistId),
        }).ToList();
    }
}
public class AppointmentDto
{
    public string Id { get; set; } = string.Empty;
    public string SpecialistId { get; set; } = string.Empty;
    public string SpecialistName { get; set; } = string.Empty;
    public string? SpecialistProfilePictureUrl { get; set; }
    public string? StudentProfilePictureUrl { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AppointmentDate { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GoogleMeetLink { get; set; }
    public bool CanCancel { get; set; }
    public decimal? AmountPaid { get; set; }
    public string? CancellationReason { get; set; }
    public string? HoldExpiresAtUtc { get; set; }
    public decimal? HourlyRate { get; set; }
}