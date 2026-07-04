using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands;
public record ConfirmAndPayCommand(
    string AppointmentId,
    string StudentId,
    string PaymentMethodId,   // the saved PaymentMethod document ID
    string? CouponCode
) : IRequest<ConfirmAndPayResult>;

public record ConfirmAndPayResult(
    string AppointmentId,
    string GoogleMeetLink,
    decimal AmountCharged
);

public class ConfirmAndPayCommandHandler : IRequestHandler<ConfirmAndPayCommand, ConfirmAndPayResult>
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPaymentProcessorFactory _paymentFactory;
    private readonly IGoogleMeetService _googleMeet;
    private readonly IEmailService _email;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ConfirmAndPayCommandHandler(
        IAppointmentRepository appointmentRepo,
        IUserRepository userRepo,
        IPaymentProcessorFactory paymentFactory,
        IGoogleMeetService googleMeet,
        IEmailService email,
        IStringLocalizer<SharedResource> localizer)
    {
        _appointmentRepo = appointmentRepo;
        _userRepo = userRepo;
        _paymentFactory = paymentFactory;
        _googleMeet = googleMeet;
        _email = email;
        _localizer = localizer;
    }

    public async Task<ConfirmAndPayResult> Handle(
        ConfirmAndPayCommand request, CancellationToken cancellationToken)
    {
        // 1. Load appointment
        var appt = await _appointmentRepo.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["AppointmentNotFound"]);

        if (appt.StudentId != request.StudentId)
            throw new UnauthorizedAccessException(_localizer["OnlyOwnAppointmentsCanBeConfirmed"]);

        if (appt.Status != Domain.Enums.AppointmentStatus.Pending)
            throw new InvalidOperationException(
                $"Appointment is already {appt.Status} and cannot be confirmed.");

        if (DateTime.UtcNow > appt.HoldExpiresAtUtc)
            throw new InvalidOperationException(
                _localizer["AppointmentHoldExpired"]);

        // 2. Load student and specialist (we need emails + hourly rate)
        var student = await _userRepo.GetByIdAsync(request.StudentId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["StudentNotFound"]);
        var specialist = await _userRepo.GetByIdAsync(appt.SpecialistId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["SpecialistNotFound"]);

        // 3. Load the chosen payment method from the student's saved methods
        var paymentMethod = student.PaymentMethods?
            .FirstOrDefault(m => m.Id == request.PaymentMethodId)
            ?? throw new KeyNotFoundException(_localizer["PaymentMethodNotFound"]);

        // 4. Charge via the matching processor
        var processor = _paymentFactory.Resolve(paymentMethod.Type);
        var chargeCtx = new ProcessorContext
        {
            Method = paymentMethod,
            Amount = specialist.HourlyRate ?? 0,
            OrderId = appt.Id,
            UserId = student.Id
        };

        var chargeResult = await processor.ProcessAsync(chargeCtx, cancellationToken);
        if (!chargeResult.Success)
            throw new InvalidOperationException($"Payment failed: {chargeResult.FailureReason}");

        // 5. Create Google Meet link
        var startUtc = DateTime.Parse($"{appt.AppointmentDate}T{appt.StartTime}:00").ToUniversalTime();
        var endUtc = DateTime.Parse($"{appt.AppointmentDate}T{appt.EndTime}:00").ToUniversalTime();

        var meetLink = await _googleMeet.CreateMeetingAsync(
            title: appt.Title,
            startUtc: startUtc,
            endUtc: endUtc,
            organizerEmail: specialist.Email,
            attendeeEmail: student.Email,
            ct: cancellationToken);

        // 6. Persist confirmation
        appt.Status = Domain.Enums.AppointmentStatus.Confirmed;
        appt.GoogleMeetLink = meetLink;
        appt.PaymentTransactionId = chargeResult.TransactionId;
        appt.PaymentMethodId = request.PaymentMethodId;   // stored for refund lookup
        appt.AmountPaid = specialist.HourlyRate;
        appt.ConfirmedAtUtc = DateTime.UtcNow;

        await _appointmentRepo.UpdateAsync(appt.Id, appt, cancellationToken);

        // 7. Send confirmation emails (fire-and-forget — don't block the response)
        _ = _email.SendBookingConfirmedAsync(
            appt,
            specialist.Email,
            specialist.FullName,
            student.Email,
            student.FullName,
            meetLink,
            System.Threading.CancellationToken.None);

        return new ConfirmAndPayResult(appt.Id, meetLink, specialist.HourlyRate ?? 0);
    }
}