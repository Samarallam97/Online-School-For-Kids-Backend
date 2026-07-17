using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Moderation
{
    public class TakeModerationActionCommand : IRequest<bool>
    {
        public string ReportId { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "Dismiss", "Warn", "Delete"
    }

    public class TakeModerationActionHandler : IRequestHandler<TakeModerationActionCommand, bool>
    {
        private readonly IReportedContentRepository _reportRepo;
        private readonly ICommentRepository _commentRepo;
        private readonly Domain.Interfaces.Repositories.IPostRepository _postRepo;
        private readonly Domain.Interfaces.Repositories.IPostCommentRepository _postCommentRepo;
        private readonly ILogger<TakeModerationActionHandler> _logger;

        public TakeModerationActionHandler(
            IReportedContentRepository reportRepo,
            ICommentRepository commentRepo,
            Domain.Interfaces.Repositories.IPostRepository postRepo,
            Domain.Interfaces.Repositories.IPostCommentRepository postCommentRepo,
            ILogger<TakeModerationActionHandler> logger)
        {
            _reportRepo = reportRepo;
            _commentRepo = commentRepo;
            _postRepo = postRepo;
            _postCommentRepo = postCommentRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(TakeModerationActionCommand request, CancellationToken ct)
        {
            try
            {
                var report = await _reportRepo.GetByIdAsync(request.ReportId, ct);
                if (report == null) return false;

                var action = Enum.Parse<ModerationAction>(request.Action);

                report.Status = ReportStatus.Resolved;
                report.Action = action;
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewedBy = request.AdminId;

                // If action is Delete, remove the underlying content. The repos below
                // filter on author-id ownership, so we delete "as" the original author
                // rather than the admin — the admin's authority to do this at all is
                // already gated by [Authorize(Roles = "Admin")] on the controller.
                if (action == ModerationAction.ContentRemoved)
                {
                    switch (report.ContentType)
                    {
                        case ContentType.Comment:
                            await _commentRepo.DeleteAsync(report.ContentId, ct);
                            break;

                        case ContentType.Post:
                            var post = await _postRepo.GetByIdAsync(report.ContentId, ct);
                            if (post != null)
                                await _postRepo.DeleteAsync(report.ContentId, post.AuthorId, ct);
                            break;

                        case ContentType.PostComment:
                            var comment = await _postCommentRepo.GetByIdAsync(report.ContentId, ct);
                            if (comment != null)
                                await _postCommentRepo.DeleteAsync(report.ContentId, comment.AuthorId, ct);
                            break;
                    }
                }

                await _reportRepo.UpdateAsync(report.Id, report, ct);

                _logger.LogInformation("Moderation action taken: {Action} on Report {ReportId}", action, report.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking moderation action");
                return false;
            }
        }
    }
    public class ModerationActionDtoValidator : AbstractValidator<ModerationActionDto>
    {
        public ModerationActionDtoValidator()
        {
            RuleFor(x => x.ReportId)
                .NotEmpty().WithMessage("Report ID is required");

            RuleFor(x => x.Action)
                .NotEmpty().WithMessage("Action is required")
                .Must(action => new[] { "Dismissed", "Warned", "ContentRemoved", "UserBanned" }.Contains(action))
                .WithMessage("Invalid action. Must be Dismissed, Warned, ContentRemoved, or UserBanned");
        }
    }
    public class ModerationActionDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "Dismiss", "Warn", "Delete"
    }
}