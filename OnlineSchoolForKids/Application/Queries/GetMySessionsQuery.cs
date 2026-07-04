using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using MediatR;


namespace Application.Queries;

public record GetMySessionsQuery(string SpecialistId) : IRequest<List<AppointmentDto>>;

public class GetMySessionsQueryHandler : IRequestHandler<GetMySessionsQuery, List<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IUserRepository _userRepo;

    public GetMySessionsQueryHandler(
        IAppointmentRepository appointmentRepo,
        IUserRepository userRepo)
    {
        _appointmentRepo = appointmentRepo;
        _userRepo = userRepo;
    }

    public async Task<List<AppointmentDto>> Handle(
        GetMySessionsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepo.GetBySpecialistIdAsync(request.SpecialistId, cancellationToken);

        var studentIds = appointments.Select(a => a.StudentId).Distinct().ToList();
        var studentNames = new Dictionary<string, string>();
        var studentPics = new Dictionary<string, string?>();

        foreach (var id in studentIds)
        {
            var u = await _userRepo.GetByIdAsync(id, cancellationToken);
            if (u is not null)
            {
                studentNames[id] = u.FullName;
                studentPics[id] = u.ProfilePictureUrl;
            }
        }

        return appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            SpecialistId = a.SpecialistId,
            SpecialistName = "",
            SpecialistProfilePictureUrl = null,
            StudentId = a.StudentId,
            StudentName = studentNames.GetValueOrDefault(a.StudentId, ""),
            StudentProfilePictureUrl = studentPics.GetValueOrDefault(a.StudentId),
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
        }).ToList();
    }
}