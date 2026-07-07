using Application.Queries.Chatbot;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/chatbot")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ChatbotController(IMediator mediator) => _mediator = mediator;

        // ── POST api/chatbot/ask ──────────────────────────────────────────────────
        // Anyone can ask — no auth required.
        // If the chatbot knows the answer: returns it.
        // If not: saves as Pending, notifies admins, returns friendly message.
        [HttpPost("ask")]
        [AllowAnonymous]
        public async Task<IActionResult> Ask(
            [FromBody] AskChatbotRequest request, CancellationToken ct)
        {
            // Get userId if the user is logged in (null for anonymous users)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _mediator.Send(
                new AskChatbotCommand(request.Query, request.Lang, userId), ct);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return Ok(result.Data);
        }

        // ── PUT api/chatbot/questions/{id}/answer ─────────────────────────────────
        // Admin only.
        // Saves the answer, notifies the user, pushes Q&A to chatbot knowledge base.
        [HttpPut("questions/{id}/answer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Answer(
            string id,
            [FromBody] AnswerQuestionRequest request,
            CancellationToken ct)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _mediator.Send(new AnswerPendingQuestionCommand(
                AdminId: adminId,
                QuestionId: id,
                AnswerAr: request.AnswerAr,
                AnswerEn: request.AnswerEn,
                QuestionArOverride: request.QuestionArOverride,
                QuestionEnOverride: request.QuestionEnOverride,
                Category: request.Category ?? "General"), ct);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = result.Data });
        }

        // ── GET api/chatbot/questions?status=Pending&page=1&pageSize=20 ───────────
        // Admin only — list pending/answered questions.
        [HttpGet("questions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetQuestions(
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetPendingQuestionsQuery(status, page, pageSize), ct);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            var (items, total) = result.Data;
            return Ok(new { items, totalCount = total });
        }
    }

    // ── Request bodies ────────────────────────────────────────────────────────────

    public record AskChatbotRequest(string Query, string? Lang = null);

    public record AnswerQuestionRequest(
        string AnswerAr,
        string AnswerEn,
        string? QuestionArOverride = null,   // optional rephrasing for the knowledge base
        string? QuestionEnOverride = null,
        string? Category = "General");

}
