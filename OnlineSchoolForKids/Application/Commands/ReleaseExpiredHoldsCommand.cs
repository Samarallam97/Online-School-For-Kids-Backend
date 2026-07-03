using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Commands;

public record ReleaseExpiredHoldsCommand : IRequest<int>; // returns count released

public class ReleaseExpiredHoldsCommandHandler : IRequestHandler<ReleaseExpiredHoldsCommand, int>
{
    private readonly IAppointmentRepository _appointmentRepo;

    public ReleaseExpiredHoldsCommandHandler(IAppointmentRepository appointmentRepo)
        => _appointmentRepo = appointmentRepo;

    public async Task<int> Handle(ReleaseExpiredHoldsCommand _, CancellationToken cancellationToken)
    {
        var expired = (await _appointmentRepo.GetExpiredPendingAsync(
            DateTime.UtcNow, cancellationToken)).ToList();

        if (expired.Count == 0) return 0;

        foreach (var appt in expired)
        {
            appt.Status = AppointmentStatus.Cancelled;
            appt.CancellationReason = "Hold expired — not confirmed within 30 minutes.";
            appt.CancelledAtUtc = DateTime.UtcNow;
        }

        await _appointmentRepo.UpdateManyAsync(expired, cancellationToken);
        return expired.Count;
    }
}