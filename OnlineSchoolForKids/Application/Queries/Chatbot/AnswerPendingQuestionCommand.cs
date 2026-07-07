using Application.DTOs;
using Domain.Entities.Chatbot;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Chatbot
{
    public record AnswerPendingQuestionCommand(
    string AdminId,
    string QuestionId,

    // Admin provides the answer in both languages so the chatbot
    // can serve both Arabic and English users going forward.
    string AnswerAr,
    string AnswerEn,

    // Optional: admin can rephrase the question for the knowledge base
    string? QuestionArOverride = null,
    string? QuestionEnOverride = null,

    string Category = "General")
    : IRequest<Result<string>>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public class AnswerPendingQuestionHandler
        : IRequestHandler<AnswerPendingQuestionCommand, Result<string>>
    {
        private readonly IPendingQuestionRepository _pendingRepo;
        private readonly IChatbotService _chatbot;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AnswerPendingQuestionHandler> _logger;

        public AnswerPendingQuestionHandler(
            IPendingQuestionRepository pendingRepo,
            IChatbotService chatbot,
            INotificationService notificationService,
            ILogger<AnswerPendingQuestionHandler> logger)
        {
            _pendingRepo = pendingRepo;
            _chatbot = chatbot;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(
            AnswerPendingQuestionCommand request, CancellationToken ct)
        {
            var pending = await _pendingRepo.GetByIdAsync(request.QuestionId, ct);
            if (pending is null)
                return Result<string>.Failure("Question not found.");

            if (pending.Status == PendingQuestionStatus.Answered)
                return Result<string>.Failure("This question has already been answered.");

            // ── Step 1: save the answer ───────────────────────────────────────────
            pending.Answer = pending.Language == "ar" ? request.AnswerAr : request.AnswerEn;
            pending.AnsweredByAdminId = request.AdminId;
            pending.AnsweredAt = DateTime.UtcNow;
            pending.Status = PendingQuestionStatus.Answered;
            pending.UpdatedAt = DateTime.UtcNow;

            await _pendingRepo.UpdateAsync(pending.Id, pending, ct);

            // ── Step 2: notify the user who asked ─────────────────────────────────
            if (!string.IsNullOrEmpty(pending.UserId))
            {
                _ = _notificationService.SendAsync(
                    userId: pending.UserId,
                    title: "Your question has been answered! 💬",
                    message: $"An admin has answered your question: \"{TruncateQuestion(pending.Question)}\"",
                    type: NotificationType.General,
                    actionUrl: $"/chatbot/answers/{pending.Id}",
                    ct: CancellationToken.None);
            }

            // ── Step 3: push Q&A to chatbot knowledge base ────────────────────────
            // Fire-and-forget — if the chatbot is down, the answer is still saved in our DB
            _ = PushToChatbotAsync(pending, request, CancellationToken.None);

            _logger.LogInformation(
                "Pending chatbot question {Id} answered by admin {AdminId}",
                pending.Id, request.AdminId);

            return Result<string>.Success("Question answered successfully. The chatbot knowledge base will be updated.");
        }

        private async Task PushToChatbotAsync(
            PendingQuestion pending,
            AnswerPendingQuestionCommand request,
            CancellationToken ct)
        {
            try
            {
                var questionAr = request.QuestionArOverride ?? pending.Question;
                var questionEn = request.QuestionEnOverride ?? pending.Question;

                var pushed = await _chatbot.AddToKnowledgeBaseAsync(
                    questionAr: questionAr,
                    answerAr: request.AnswerAr,
                    questionEn: questionEn,
                    answerEn: request.AnswerEn,
                    category: request.Category,
                    ct: ct);

                if (pushed)
                {
                    // Mark as pushed so we know the chatbot has this Q&A
                    pending.PushedToChatbot = true;
                    pending.UpdatedAt = DateTime.UtcNow;
                    await _pendingRepo.UpdateAsync(pending.Id, pending, ct);

                    _logger.LogInformation(
                        "Q&A for pending question {Id} pushed to chatbot knowledge base",
                        pending.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to push Q&A to chatbot for pending question {Id} — " +
                    "answer is saved in our DB but chatbot won't know about it until re-indexed",
                    pending.Id);
            }
        }

        private static string TruncateQuestion(string q) =>
            q.Length > 80 ? q[..80] + "..." : q;
    }

}
