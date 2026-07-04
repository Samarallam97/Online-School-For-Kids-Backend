using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands;
public record UpdateAppointmentStatusCommand(
    string RequesterId,
    string AppointmentId,
    AppointmentStatus NewStatus,
    string? CancellationReason = null
) : IRequest;

public class UpdateAppointmentStatusCommandHandler : IRequestHandler<UpdateAppointmentStatusCommand>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPaymentProcessorFactory _paymentFactory;
    private readonly IEmailService _email;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateAppointmentStatusCommandHandler(
        IAppointmentRepository appointmentRepo,
        IUserRepository userRepo,
        IPaymentProcessorFactory paymentFactory,
        IEmailService email,
        IStringLocalizer<SharedResource> localizer)
    {
        _appointmentRepo = appointmentRepo;
        _userRepo = userRepo;
        _paymentFactory = paymentFactory;
        _email = email;
        _localizer = localizer;
    }

    public async Task Handle(
        UpdateAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        var appt = await _appointmentRepo.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["AppointmentNotFound"]);

        var isSpecialist = appt.SpecialistId == request.RequesterId;
        var isStudent = appt.StudentId == request.RequesterId;

        if (!isSpecialist && !isStudent)
            throw new UnauthorizedAccessException(_localizer["AppointmentAccessDenied"]);

        switch (request.NewStatus)
        {
            // ── Confirm (specialist only) ──────────────────────────────────
            // NOTE: Confirmation with payment now goes through ConfirmAndPayCommand.
            // This path is kept for specialist-side manual confirmation if needed.
            case AppointmentStatus.Confirmed:
                if (!isSpecialist)
                    throw new UnauthorizedAccessException(_localizer["OnlySpecialistCanConfirmAppointments"]);
                appt.Status = AppointmentStatus.Confirmed;
                appt.ConfirmedAtUtc = DateTime.UtcNow;
                break;

            // ── Cancel (student or specialist, >30 min before session) ─────
            case AppointmentStatus.Cancelled:
                if (!appt.CanCancel())
                    throw new InvalidOperationException(
                        _localizer["CancellationDeadlineExceeded"]);

                appt.Status = AppointmentStatus.Cancelled;
                appt.CancellationReason = request.CancellationReason;
                appt.CancelledAtUtc = DateTime.UtcNow;

                // Refund if payment was taken
                await HandleRefundAsync(appt, cancellationToken);

                // Email both parties
                await SendCancellationEmailsAsync(appt, request.CancellationReason,
                    appt.PaymentTransactionId is not null, cancellationToken);
                break;

            // ── Complete (specialist only) ─────────────────────────────────
            case AppointmentStatus.Completed:
                if (!isSpecialist)
                    throw new UnauthorizedAccessException("Only the specialist can mark a session as completed.");
                if (appt.Status != AppointmentStatus.Confirmed)
                    throw new InvalidOperationException("Only confirmed sessions can be marked as completed.");
                appt.Status = AppointmentStatus.Completed;
                break;

            default:
                throw new InvalidOperationException($"Unsupported status transition to {request.NewStatus}.");
        }

        await _appointmentRepo.UpdateAsync(appt.Id, appt, cancellationToken);
    }

    private async Task HandleRefundAsync(Domain.Entities.Appointment appt, CancellationToken ct)
    {
        // Nothing to refund if payment was never taken (still Pending when cancelled)
        if (appt.PaymentTransactionId is null || appt.AmountPaid is null || appt.PaymentMethodId is null)
            return;

        var student = await _userRepo.GetByIdAsync(appt.StudentId, ct);
        var method = student?.PaymentMethods?.FirstOrDefault(m => m.Id == appt.PaymentMethodId);

        if (method is null) return; // Log in production — can't refund without method type

        var processor = _paymentFactory.Resolve(method.Type);
        await processor.RefundAsync(appt.PaymentTransactionId, appt.AmountPaid.Value, ct);
    }

    private async Task SendCancellationEmailsAsync(
        Domain.Entities.Appointment appt,
        string? reason,
        bool refundIssued,
        CancellationToken ct)
    {
        var specialist = await _userRepo.GetByIdAsync(appt.SpecialistId, ct);
        var student = await _userRepo.GetByIdAsync(appt.StudentId, ct);

        if (specialist is null || student is null) return;

        _ = _email.SendBookingCancelledAsync(
            appt,
            specialist.Email,
            student.Email,
            reason,
            refundIssued,
            CancellationToken.None);
    }
}