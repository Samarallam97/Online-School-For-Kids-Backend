using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.Commands.Profile.Specialists;
using Application.Queries.Profile.Specialists;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace API.Controllers.User_Module;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SpecialistController : ControllerBase
{
    private readonly IMediator _mediator;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public SpecialistController(IMediator mediator) => _mediator = mediator;

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAvailabilityQuery(UserId), ct);
        return Ok(result);
    }

    [HttpPost("availability")]
    public async Task<IActionResult> AddAvailabilitySlot([FromBody] AddAvailabilitySlotDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddAvailabilitySlotCommand(UserId, dto.Day, dto.StartTime, dto.EndTime), ct);
        return Ok(result);
    }

    [HttpPut("availability/{slotId}")]
    public async Task<IActionResult> UpdateAvailabilitySlot(string slotId, [FromBody] AddAvailabilitySlotDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAvailabilitySlotCommand(UserId, slotId, dto.Day, dto.StartTime, dto.EndTime), ct);
        return Ok(result);
    }

    [HttpDelete("availability/{slotId}")]
    public async Task<IActionResult> DeleteAvailabilitySlot(string slotId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteAvailabilitySlotCommand(UserId, slotId), ct);
        return NoContent();
    }

    [HttpPut("rates")]
    public async Task<IActionResult> UpdateSessionRates([FromBody] UpdateSessionRatesDto dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateSessionRatesCommand(UserId, dto.HourlyRate), ct);
        return NoContent();
    }
}
