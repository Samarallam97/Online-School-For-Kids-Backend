using Application.Commands.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.User_Module
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        public NotificationController(IMediator mediator) => _mediator = mediator;

        // GET api/notification?page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] GetNotificationsRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(new GetNotificationsQuery(userId, request.Page, request.PageSize), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            var (items, total) = result.Data;
            return Ok(new { items, totalCount = total });
        }

        // GET api/notification/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(new GetUnreadCountQuery(userId), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(new { unreadCount = result.Data });
        }

        // PATCH api/notification/{id}/read
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(string id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(new MarkNotificationReadCommand(id, userId), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(new { message = result.Data });
        }

        // PATCH api/notification/read-all
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(new MarkAllNotificationsReadCommand(userId), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(new { message = result.Data });
        }
    }
}