using Application.DTOs;
using Domain.Entities.Chatbot;
using Domain.Enums.Content;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Chatbot
{
    public record AskChatbotRequest(string Query, string? Lang = null);

    public record ChatbotAnswerDto(
        bool Status,
        string Answer,
        double Similarity,
        string Language,
        double ResponseTime,
        string? PendingQuestionId); // non-null when the question was saved as pending

    // ── Command ───────────────────────────────────────────────────────────────────

    public record AskChatbotCommand(
        string Query,
        string? Lang,
        string? UserId)   // null if the user is not logged in
        : IRequest<Result<ChatbotAnswerDto>>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public class AskChatbotCommandHandler : IRequestHandler<AskChatbotCommand, Result<ChatbotAnswerDto>>
    {
        private readonly IChatbotService _chatbot;
        private readonly IPendingQuestionRepository _pendingRepo;
        private readonly IUserRepository _userRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AskChatbotCommandHandler> _logger;

        // Friendly messages returned to the user when the question is unknown
        private const string UnknownMessageEn =
            "Sorry, I couldn't find an answer to your question. " +
            "It has been forwarded to the admin and you will be notified when it's answered.";

        private const string UnknownMessageAr =
            "عذراً، لم أتمكن من الإجابة على سؤالك. " +
            "تم إرسال سؤالك إلى الإدارة وسيتم إشعارك عند الرد عليه.";

        public AskChatbotCommandHandler(
            IChatbotService chatbot,
            IPendingQuestionRepository pendingRepo,
            IUserRepository userRepo,
            INotificationService notificationService,
            ILogger<AskChatbotCommandHandler> logger)
        {
            _chatbot = chatbot;
            _pendingRepo = pendingRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Result<ChatbotAnswerDto>> Handle(
            AskChatbotCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return Result<ChatbotAnswerDto>.Failure("Question cannot be empty.");

            // ── Step 1: forward to chatbot ────────────────────────────────────────
            var response = await _chatbot.AskAsync(request.Query, request.Lang, ct);

            // ── Step 2: known answer → return immediately ─────────────────────────
            if (response.Status)
            {
                return Result<ChatbotAnswerDto>.Success(new ChatbotAnswerDto(
                    response.Status, response.Answer, response.Similarity,
                    response.Language, response.ResponseTime, null));
            }

            // ── Step 3: unknown → save as pending + notify admins ─────────────────
            var pending = new PendingQuestion
            {
                UserId = request.UserId,
                Question = request.Query,
                Language = response.Language,
                Similarity = response.Similarity,
                Status = PendingQuestionStatus.Pending
            };

            await _pendingRepo.CreateAsync(pending, ct);

            _logger.LogInformation(
                "Unknown chatbot question saved as pending: {Id} — \"{Q}\"",
                pending.Id, request.Query);

            // Notify all admins — fire-and-forget so it never blocks the response
            _ = NotifyAdminsAsync(pending, CancellationToken.None);

            // Return a friendly localized message
            var friendlyMessage = response.Language == "ar"
                ? UnknownMessageAr
                : UnknownMessageEn;

            return Result<ChatbotAnswerDto>.Success(new ChatbotAnswerDto(
                Status: false,
                Answer: friendlyMessage,
                Similarity: 0,
                Language: response.Language,
                ResponseTime: response.ResponseTime,
                PendingQuestionId: pending.Id));
        }

        private async Task NotifyAdminsAsync(PendingQuestion pending, CancellationToken ct)
        {
            try
            {
                // Get all admins using the existing GetUsersPagedAsync
                var (admins, _) = await _userRepo.GetUsersPagedAsync(
                    search: null,
                    role: UserRole.Admin.ToString(),
                    status: null,
                    excludeAdmins: false,
                    skip: 0,
                    limit: 100,
                    cancellationToken: ct);

                var tasks = admins.Select(admin => _notificationService.SendAsync(
                    userId: admin.Id,
                    title: "New unanswered chatbot question",
                    message: $"A user asked: \"{TruncateQuestion(pending.Question)}\" — tap to answer.",
                    type: NotificationType.General,
                    actionUrl: $"/admin/chatbot/questions/{pending.Id}",
                    ct: ct));

                await Task.WhenAll(tasks);

                _logger.LogInformation(
                    "Notified {Count} admin(s) about pending chatbot question {Id}",
                    admins.Count(), pending.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to notify admins about pending chatbot question {Id}", pending.Id);
            }
        }

        private static string TruncateQuestion(string q) =>
            q.Length > 80 ? q[..80] + "..." : q;
    }

}
