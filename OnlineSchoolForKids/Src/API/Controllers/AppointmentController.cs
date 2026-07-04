using Application.Commands;
using Application.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AppointmentController : ControllerBase
{
    private readonly IMediator _mediator;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? "Student";

    public AppointmentController(IMediator mediator) => _mediator = mediator;

    // ── POST /api/Appointment ─────────────────────────────────────────────────
    // Step 1: reserve a slot (status = Pending, hold for 30 min).
    // Returns { appointmentId, status } so the client can redirect to payment.
    [HttpPost]
    public async Task<IActionResult> BookSession(
        [FromBody] BookSessionDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new BookSessionCommand(
            UserId,
            dto.SpecialistId,
            dto.Title,
            dto.Description,
            dto.AppointmentDate,
            dto.StartTime,
            dto.EndTime), ct);

        return Ok(result); // { id, status }
    }

    // ── POST /api/Appointment/{id}/confirm-pay ────────────────────────────────
    // Step 2: charge the student, generate the Meet link, move to Confirmed.
    // Returns { appointmentId, googleMeetLink, amountCharged }.
    [HttpPost("{id}/confirm-pay")]
    public async Task<IActionResult> ConfirmAndPay(
        string id,
        [FromBody] ConfirmPayDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmAndPayCommand(
            AppointmentId: id,
            StudentId: UserId,
            PaymentMethodId: dto.PaymentMethodId,
            CouponCode: dto.CouponCode), ct);

        return Ok(result);
    }

    // ── GET /api/Appointment/my ───────────────────────────────────────────────
    // Returns all appointments for the calling user (student or specialist).
    [HttpGet("my")]
    public async Task<IActionResult> GetMyAppointments(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyAppointmentsQuery(UserId), ct);
        return Ok(result);
    }


    // ── GET /api/Appointment/booked-slots?specialistId=&date= ────────────────
    // Returns "HH:mm" strings for all taken slots (Pending + Confirmed).
    // AllowAnonymous so the calendar is visible before login.
    [HttpGet("booked-slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBookedSlots(
        [FromQuery] string specialistId,
        [FromQuery] string date,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetBookedSlotsQuery(specialistId, date), ct);
        return Ok(result);
    }

    // ── PUT /api/Appointment/{id}/cancel ─────────────────────────────────────
    // Student or specialist. Validates 30-min window; issues refund automatically.
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(
        string id,
        [FromBody] CancelDto dto,
        CancellationToken ct)
    {
        await _mediator.Send(new UpdateAppointmentStatusCommand(
            UserId, id, AppointmentStatus.Cancelled, dto.Reason), ct);
        return NoContent();
    }

    // ── PUT /api/Appointment/{id}/complete ────────────────────────────────────
    // Specialist only — marks the session as done.
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(string id, CancellationToken ct)
    {
        await _mediator.Send(new UpdateAppointmentStatusCommand(
            UserId, id, AppointmentStatus.Completed), ct);
        return NoContent();
    }

    [HttpGet("my-sessions")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> GetMySessions(CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetMySessionsQuery(UserId), ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAppointmentByIdQuery(id, UserId), ct);
        return Ok(result);
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public class BookSessionDto
{
    public string SpecialistId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AppointmentDate { get; set; } = string.Empty; // "yyyy-MM-dd"
    public string StartTime { get; set; } = string.Empty; // "HH:mm"
    public string EndTime { get; set; } = string.Empty;
}

public class ConfirmPayDto
{
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
}

public class CancelDto
{
    public string? Reason { get; set; }
}