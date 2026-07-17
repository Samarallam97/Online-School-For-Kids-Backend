using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using Application.Commands.Moderation; // ReportedContentDto lives here — not redefined below

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
                r => r.Status == ReportStatus.Pending || r.Status == ReportStatus.UnderReview, ct);

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
}