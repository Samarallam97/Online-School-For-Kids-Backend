using Domain.Entities.Content.Moderation;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Moderation
{
    public class ReportContentCommand : IRequest<bool>
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
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
                    ReportedByName = request.UserName,
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
        public ReportContentDtoValidator()
        {
            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("Content type is required")
                .Must(type => new[] { "Comment", "Course", "Review", "Message" }.Contains(type))
                .WithMessage("Invalid content type. Must be Comment, Course, Review, or Message");

            RuleFor(x => x.ContentId)
                .NotEmpty().WithMessage("Content ID is required");

            RuleFor(x => x.ContentTitle)
                .NotEmpty().WithMessage("Content title is required")
                .MaximumLength(200).WithMessage("Content title cannot exceed 200 characters");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Report reason is required")
                .Must(reason => new[] { "Spam", "Harassment", "InappropriateContent", "Copyright", "Misinformation", "Other" }.Contains(reason))
                .WithMessage("Invalid reason. Must be Spam, Harassment, InappropriateContent, Copyright, Misinformation, or Other");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MinimumLength(10).WithMessage("Description must be at least 10 characters")
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
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
