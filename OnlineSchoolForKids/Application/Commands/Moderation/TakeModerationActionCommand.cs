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
        private readonly ILogger<TakeModerationActionHandler> _logger;

        public TakeModerationActionHandler(
            IReportedContentRepository reportRepo,
            ICommentRepository commentRepo,
            ILogger<TakeModerationActionHandler> logger)
        {
            _reportRepo = reportRepo;
            _commentRepo = commentRepo;
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

                // If action is Delete, remove the content
                if (action == ModerationAction.ContentRemoved && report.ContentType == ContentType.Comment)
                {
                    await _commentRepo.DeleteAsync(report.ContentId, ct);
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
