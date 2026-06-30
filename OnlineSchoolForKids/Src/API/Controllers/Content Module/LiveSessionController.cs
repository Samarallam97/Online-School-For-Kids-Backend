using Application.Commands.Live;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Content_Module
{
    [Route("api/live-sessions")]
    [ApiController]
    [Authorize]
    public class LiveSessionController : ControllerBase
    {
        private readonly IMediator _mediator;
        public LiveSessionController(IMediator mediator) => _mediator = mediator;

        // ── GET api/live-sessions
        // Discover page — all currently live sessions
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetLiveSessionsQuery(), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(result.Data);
        }

        // ── GET api/live-sessions/{id}
        // LiveSessionPage.tsx calls this on mount to get session details + channel name
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(new GetLiveSessionQuery(id, userId), ct);
            if (!result.IsSuccess) return NotFound(new { error = result.Error });
            return Ok(result.Data);
        }

        // ── POST api/live-sessions
        // GoLivePage.tsx calls this when the host clicks "Go Live"
        // Returns the session id and channelName the frontend needs
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLiveSessionRequest request, CancellationToken ct)
        {
            var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            DateTime? scheduledAt = null;
            if (request.IsScheduled && !string.IsNullOrEmpty(request.ScheduledAt))
                scheduledAt = DateTime.Parse(request.ScheduledAt);

            var result = await _mediator.Send(new CreateLiveSessionCommand(
                hostId,
                request.Title,
                request.Description,
                request.Category,
                request.AllowChat,
                request.AllowQuestions,
                scheduledAt), ct);

            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(result.Data);
        }

        // ── PATCH api/live-sessions/{id}/end
        // GoLivePage.tsx calls this when host clicks "Stop Stream"
        // Also accepts an optional whiteboard PNG as multipart/form-data
        [HttpPatch("{id}/end")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> End(string id, [FromForm] EndLiveSessionRequest request, CancellationToken ct)
        {
            var hostId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(new EndLiveSessionCommand(hostId, id, request.WhiteboardImage), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(new { message = result.Data });
        }

        // ── GET api/live-sessions/{id}/chat
        // Load chat history when a user joins (so they see messages sent before they arrived)
        [HttpGet("{id}/chat")]
        public async Task<IActionResult> GetChat(string id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetChatHistoryQuery(id), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(result.Data);
        }
    }

    // ── Request bodies ────────────────────────────────────────────────────────────

    public record CreateLiveSessionRequest(
        string Title,
        string? Description,
        string? Category,
        bool AllowChat,
        bool AllowQuestions,
        bool IsScheduled,
        string? ScheduledAt);

    public class EndLiveSessionRequest
    {
        public Microsoft.AspNetCore.Http.IFormFile? WhiteboardImage { get; set; }
    }

}
