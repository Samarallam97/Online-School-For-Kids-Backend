using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries;

public record GetBookedSlotsQuery(string SpecialistId, string Date) : IRequest<List<BookedSlotDto>>;

public class GetBookedSlotsQueryHandler : IRequestHandler<GetBookedSlotsQuery, List<BookedSlotDto>>
{
    private readonly IAppointmentRepository _appointmentRepo;

    public GetBookedSlotsQueryHandler(IAppointmentRepository appointmentRepo)
        => _appointmentRepo = appointmentRepo;

    public async Task<List<BookedSlotDto>> Handle(
        GetBookedSlotsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepo.GetBookedSlotsAsync(
            request.SpecialistId, request.Date, cancellationToken);

        return appointments.Select(a => new BookedSlotDto
        {
            Time = a.StartTime,
            // "Reserved" = someone else has it on hold but hasn't paid yet; the
            // slot may free back up on its own if HoldExpiresAtUtc passes.
            // "Booked" = confirmed and paid — genuinely unavailable.
            Status = a.Status == AppointmentStatus.Pending ? "Reserved" : "Booked",
            HoldExpiresAtUtc = a.Status == AppointmentStatus.Pending ? a.HoldExpiresAtUtc : null
        }).ToList();
    }
}

public class BookedSlotDto
{
    public string Time { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Reserved" | "Booked"
    public DateTime? HoldExpiresAtUtc { get; set; }
}