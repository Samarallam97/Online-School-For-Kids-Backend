using Application.Commands.Live;
using Application.Queries.Live;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Content_Module
{
    [Route("api/lessons/{lessonId}/live")]
    [ApiController]
    [Authorize]
    public class LiveSessionController : ControllerBase
    {
        private readonly IMediator _mediator;
        public LiveSessionController(IMediator mediator) => _mediator = mediator;

        // ── POST api/lessons/{lessonId}/live/schedule
        // Instructor schedules a live session for this lesson.
        [HttpPost("schedule")]
        public async Task<IActionResult> Schedule(
            string lessonId, [FromBody] ScheduleLiveSessionRequest request, CancellationToken ct)
        {
            var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _mediator.Send(new ScheduleLiveSessionCommand(
                instructorId, lessonId, request.Title, request.Description,
                request.AllowChat, request.AllowQuestions, request.ScheduledAt), ct);

            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(result.Data);
        }

        // ── POST api/lessons/{lessonId}/live/{sessionId}/start
        // Instructor goes live -- notifies all enrolled students instantly.
        [HttpPost("{sessionId}/start")]
        public async Task<IActionResult> Start(string lessonId, string sessionId, CancellationToken ct)
        {
            var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(new StartLiveSessionCommand(instructorId, sessionId), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(result.Data);
        }

        // ── PATCH api/lessons/{lessonId}/live/{sessionId}/end
        // Instructor ends the session and optionally uploads the whiteboard PNG.
        [HttpPatch("{sessionId}/end")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> End(
            string lessonId, string sessionId,
            [FromForm] EndLiveSessionRequest request, CancellationToken ct)
        {
            var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(
                new EndLiveSessionCommand(instructorId, sessionId, request.WhiteboardImage), ct);

            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(new { whiteboardUrl = result.Data });
        }

        // ── GET api/lessons/{lessonId}/live/{sessionId}
        // Get session details -- only the instructor or an enrolled student can see it.
        [HttpGet("{sessionId}")]
        public async Task<IActionResult> Get(string lessonId, string sessionId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _mediator.Send(new GetLiveSessionQuery(sessionId, userId), ct);
            if (!result.IsSuccess) return Forbid();
            return Ok(result.Data);
        }

        // ── GET api/lessons/{lessonId}/live/{sessionId}/chat
        [HttpGet("{sessionId}/chat")]
        public async Task<IActionResult> GetChat(string lessonId, string sessionId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetChatHistoryQuery(sessionId), ct);
            if (!result.IsSuccess) return BadRequest(new { error = result.Error });
            return Ok(result.Data);
        }
    }

    public record ScheduleLiveSessionRequest(
        string Title,
        string? Description,
        bool AllowChat,
        bool AllowQuestions,
        DateTime? ScheduledAt);

    public class EndLiveSessionRequest
    {
        public IFormFile? WhiteboardImage { get; set; }
    }
}