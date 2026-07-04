using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Queries;

public record GetBookedSlotsQuery(string SpecialistId, string Date) : IRequest<List<string>>;

public class GetBookedSlotsQueryHandler : IRequestHandler<GetBookedSlotsQuery, List<string>>
{
    private readonly IAppointmentRepository _appointmentRepo;

    public GetBookedSlotsQueryHandler(IAppointmentRepository appointmentRepo)
        => _appointmentRepo = appointmentRepo;

    public async Task<List<string>> Handle(
        GetBookedSlotsQuery request, CancellationToken cancellationToken)
    {
        var slots = await _appointmentRepo.GetBookedSlotsAsync(
            request.SpecialistId, request.Date, cancellationToken);
        return slots.ToList();
    }
}