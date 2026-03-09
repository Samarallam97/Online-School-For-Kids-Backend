using Application.Commands.Admin;
using Application.Commands.Auth;
using Application.Commands.Profile.Admin;
using Application.Queries.Admin;
using Application.Queries.Profile.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("security-settings")]
    public async Task<IActionResult> GetSecuritySettings(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSecuritySettingsQuery(UserId), ct);
        return Ok(result);
    }

    [HttpPut("security-settings")]
    public async Task<IActionResult> UpdateSecuritySettings(
        [FromBody] UpdateSecuritySettingsRequest dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateSecuritySettingsCommand(
                UserId,
                dto.LoginNotifications,
                dto.SuspiciousActivityAlerts),
            ct);
        return Ok(result);
    }

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

    [HttpGet("activity-log")]
    public async Task<IActionResult> GetActivityLog(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetActivityLogQuery(UserId, page, limit), ct);
        return Ok(result);
    }

    [HttpPost("create-admin")]
    [Authorize]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(callerId))
            return Unauthorized();

        var command = new CreateAdminCommand(
            request.FullName,
            request.Email,
            request.Password,
            callerId);

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(GetUsersResponse), 200)]
    public async Task<IActionResult> GetUsers(
         [FromQuery] string? search,
         [FromQuery] string? role,
         [FromQuery] string? status,
         [FromQuery] int page = 1,
         [FromQuery] int limit = 5,
         [FromQuery] bool isSuperAdmin = false,
         CancellationToken ct = default)
    {
        var callerIsSuperAdmin = isSuperAdmin && CallerIsSuperAdmin;

        var result = await _mediator.Send(
            new GetUsersQuery(search, role, status, page, limit, ExcludeAdmins: !callerIsSuperAdmin), ct);
        return Ok(result);
    }


    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(AdminUserDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserById(string userId, CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new GetUserByIdQuery(userId, CallerIsSuperAdmin), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }


    [HttpPut("users/{userId}/approve")]
    [ProducesResponseType(typeof(AdminUserDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ApproveUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(new ApproveUserCommand(userId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }


    [HttpPut("users/{userId}/suspend")]
    [ProducesResponseType(typeof(AdminUserDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SuspendUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(new SuspendUserCommand(userId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }


    [HttpDelete("users/{userId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteUser(string userId, CancellationToken ct = default)
    {
        try
        {
            await _mediator.Send(new DeleteUserCommand(userId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }


    [HttpPut("users/{userId}/change-password")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ChangeUserPassword(
        string userId,
        [FromBody] ChangePasswordRequest body,
        CancellationToken ct = default)
    {
        if (!CallerIsSuperAdmin) return SuperAdminOnly();

        try
        {
            await _mediator.Send(new ChangeUserPasswordCommand(userId, body.NewPassword), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }


    [HttpPost("users/bulk/approve")]
    [ProducesResponseType(typeof(BulkActionResponse), 200)]
    public async Task<IActionResult> BulkApprove([FromBody] BulkUserIdsRequest body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new BulkApproveUsersCommand(body.UserIds), ct);
        return Ok(result);
    }


    [HttpPost("users/bulk/suspend")]
    [ProducesResponseType(typeof(BulkActionResponse), 200)]
    public async Task<IActionResult> BulkSuspend([FromBody] BulkUserIdsRequest body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new BulkSuspendUsersCommand(body.UserIds), ct);
        return Ok(result);
    }


    [HttpPost("users/bulk/delete")]
    [ProducesResponseType(typeof(BulkActionResponse), 200)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkUserIdsRequest body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new BulkDeleteUsersCommand(body.UserIds), ct);
        return Ok(result);
    }

    private bool CallerIsSuperAdmin =>
         User.FindFirstValue("isSuperAdmin") == "true" ||
         User.HasClaim("isSuperAdmin", "true");

    private IActionResult SuperAdminOnly() =>
        StatusCode(403, new { message = "This action requires Super Admin privileges." });


}
