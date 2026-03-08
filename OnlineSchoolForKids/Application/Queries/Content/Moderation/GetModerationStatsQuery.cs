using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Content.Moderation
{
    public class GetModerationStatsQuery : IRequest<ModerationStatsDto>
    {
    }
    public class GetModerationStatsHandler : IRequestHandler<GetModerationStatsQuery, ModerationStatsDto>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IReportedContentRepository _reportRepo;
        private readonly ICommentRepository _commentRepo;
        private readonly ILogger<GetModerationStatsHandler> _logger;

        public GetModerationStatsHandler(
            ICourseRepository courseRepo,
            IReportedContentRepository reportRepo,
            ICommentRepository commentRepo,
            ILogger<GetModerationStatsHandler> logger)
        {
            _courseRepo = courseRepo;
            _reportRepo = reportRepo;
            _commentRepo = commentRepo;
            _logger = logger;
        }
        public async Task<ModerationStatsDto> Handle(GetModerationStatsQuery request, CancellationToken ct)
        {
            try
            {
                var pendingCourses = await _courseRepo.CountAsync(
                    c => !c.IsPublished,
                    ct);

                var reports = await _reportRepo.CountAsync(
                    r => r.Status == ReportStatus.Pending ||
                         r.Status == ReportStatus.UnderReview,
                    ct);

                var flaggedComments = await _commentRepo.CountAsync(
                    c => c.IsFlagged,
                    ct);

                var today = DateTime.UtcNow.Date;

                var reportsToday = await _reportRepo.CountAsync(
                    r => r.CreatedAt >= today,
                    ct);

                return new ModerationStatsDto
                {
                    PendingCourses = (int)pendingCourses,
                    ReportedContent = (int)reports,
                    FlaggedComments = (int)flaggedComments,
                    TotalReportsToday = (int)reportsToday
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting moderation stats");
                throw;
            }
        }
    }
        public class ModerationStatsDto
    {
        public int PendingCourses { get; set; }
        public int ReportedContent { get; set; }
        public int FlaggedComments { get; set; }
        public int TotalReportsToday { get; set; }
    }
}

   

