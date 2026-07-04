using Domain.Enums;
using Domain.Interfaces.Repositories;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands;

public record ReleaseExpiredHoldsCommand : IRequest<int>; // returns count released

public class ReleaseExpiredHoldsCommandHandler : IRequestHandler<ReleaseExpiredHoldsCommand, int>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReleaseExpiredHoldsCommandHandler(IAppointmentRepository appointmentRepo, IStringLocalizer<SharedResource> localizer)
    {
        _appointmentRepo = appointmentRepo;
        _localizer = localizer;
    }

    public async Task<int> Handle(ReleaseExpiredHoldsCommand _, CancellationToken cancellationToken)
    {
        var expired = (await _appointmentRepo.GetExpiredPendingAsync(
            DateTime.UtcNow, cancellationToken)).ToList();

        if (expired.Count == 0) return 0;

        foreach (var appt in expired)
        {
            appt.Status = AppointmentStatus.Cancelled;
            appt.CancellationReason = _localizer["AppointmentHoldExpiredReason"];
            appt.CancelledAtUtc = DateTime.UtcNow;
        }

        await _appointmentRepo.UpdateManyAsync(expired, cancellationToken);
        return expired.Count;
    }
}