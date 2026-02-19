using Application.Commands.Profile.Admin;
using Application.Queries.Profile.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
//[Authorize(Roles = "Admin")]
[Authorize]

public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public AdminController(IMediator mediator) => _mediator = mediator;

    // GET /api/Admin/security-settings
    [HttpGet("security-settings")]
    public async Task<IActionResult> GetSecuritySettings(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSecuritySettingsQuery(UserId), ct);
        return Ok(result);
    }

    // PUT /api/Admin/security-settings
    [HttpPut("security-settings")]
    public async Task<IActionResult> UpdateSecuritySettings(
        [FromBody] AdminSecuritySettingsDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateSecuritySettingsCommand(
                UserId,
                dto.TwoFactorEnabled,
                dto.LoginNotifications,
                dto.SuspiciousActivityAlerts),
            ct);
        return Ok(result);
    }

    // PUT /api/Admin/change-password
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto,
        CancellationToken ct)
    {
        await _mediator.Send(
            new ChangePasswordCommand(
                UserId,
                dto.CurrentPassword,
                dto.NewPassword,
                dto.ConfirmPassword),
            ct);
        return NoContent();
    }

    // GET /api/Admin/activity-log?page=1&limit=10
    [HttpGet("activity-log")]
    public async Task<IActionResult> GetActivityLog(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetActivityLogQuery(UserId, page, limit), ct);
        return Ok(result);
    }
}
