using Domain.Entities.Content.Moderation;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Moderation
{
    public class ReportContentCommand : IRequest<bool>
    {
        public string UserId { get; set; } = string.Empty;
        public ReportContentDto Dto { get; set; } = new();
    }

    public class ReportContentHandler : IRequestHandler<ReportContentCommand, bool>
    {
        private readonly IReportedContentRepository _reportRepo;
        private readonly ILogger<ReportContentHandler> _logger;

        public ReportContentHandler(
            IReportedContentRepository reportRepo,
            ILogger<ReportContentHandler> logger)
        {
            _reportRepo = reportRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(ReportContentCommand request, CancellationToken ct)
        {
            try
            {
                // Check if already reported by this user
                var existing = await _reportRepo.GetOneAsync(
                    r => r.ContentId == request.Dto.ContentId &&
                         r.ReportedBy == request.UserId,
                    ct);

                if (existing != null)
                    return false; // Already reported

                var report = new ReportedContent
                {
                    ReportedBy = request.UserId,
                    ContentType = Enum.Parse<ContentType>(request.Dto.ContentType),
                    ContentId = request.Dto.ContentId,
                    ContentTitle = request.Dto.ContentTitle,
                    Reason = Enum.Parse<ReportReason>(request.Dto.Reason),
                    Description = request.Dto.Description,
                    Status = ReportStatus.Pending
                };

                await _reportRepo.CreateAsync(report, ct);

                _logger.LogInformation("Content reported: {ContentId} by User {UserId}", request.Dto.ContentId, request.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting content");
                return false;
            }
        }
    }
    public class ReportContentDtoValidator : AbstractValidator<ReportContentDto>
    {
        public ReportContentDtoValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage(localizer["ContentTypeIsRequired"])
                .Must(type => new[] { "Comment", "Course", "Review", "Message" }.Contains(type))
                .WithMessage(localizer["InvalidContentType"]);

            RuleFor(x => x.ContentId)
                .NotEmpty().WithMessage(localizer["ContentIdIsRequired"]);

            RuleFor(x => x.ContentTitle)
                .NotEmpty().WithMessage(localizer["ContentTitleIsRequired"])
                .MaximumLength(200).WithMessage(localizer["ContentTitleMaxLength"]);

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage(localizer["ReportReasonIsRequired"])

                .Must(reason => new[] { "Spam", "Harassment", "InappropriateContent", "Copyright", "Misinformation", "Other" }.Contains(reason))
                .WithMessage(localizer["InvalidReason"]);

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage(localizer["DescriptionIsRequired"])
                .MinimumLength(10).WithMessage(localizer["DescriptionMinLength"])
                .MaximumLength(1000).WithMessage(localizer["DescriptionMaxLength1000"]);
        }
    }
    public class ReportedContentDto
    {
        public string Id { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty; // "Comment", "Review"
        public string Reason { get; set; } = string.Empty; // "Spam", "Harassment"
        public int ReportCount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ContentTitle { get; set; } = string.Empty;
        public string ReportedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ReportContentDto
    {
        public string ContentType { get; set; } = string.Empty; // "Comment", "Course"
        public string ContentId { get; set; } = string.Empty;
        public string ContentTitle { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }


}
