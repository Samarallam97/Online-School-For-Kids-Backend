using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Content.Moderation
{
    public class GetReportedContentQuery : IRequest<IEnumerable<ReportedContentDto>>
    {
    }

    public class GetReportedContentHandler : IRequestHandler<GetReportedContentQuery, IEnumerable<ReportedContentDto>>
    {
        private readonly IReportedContentRepository _reportRepo;
        private readonly ILogger<GetReportedContentHandler> _logger;

        public GetReportedContentHandler(
            IReportedContentRepository reportRepo,
            ILogger<GetReportedContentHandler> logger)
        {
            _reportRepo = reportRepo;
            _logger = logger;
        }

        public async Task<IEnumerable<ReportedContentDto>> Handle(GetReportedContentQuery request, CancellationToken ct)
        {
            try
            {
                var reports = await _reportRepo.GetAllAsync(
                r => r.Status == ReportStatus.Pending || r.Status == ReportStatus.UnderReview,ct);
             
                return reports.Select(r => new ReportedContentDto
                {
                    Id = r.Id,
                    ContentType = r.ContentType.ToString(),
                    Reason = r.Reason.ToString(),
                    ReportCount = r.ReportCount,
                    Description = r.Description,
                    ContentTitle = r.ContentTitle,
                    ReportedByName = r.ReportedByName,
                    CreatedAt = r.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reported content");
                return Enumerable.Empty<ReportedContentDto>();
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
