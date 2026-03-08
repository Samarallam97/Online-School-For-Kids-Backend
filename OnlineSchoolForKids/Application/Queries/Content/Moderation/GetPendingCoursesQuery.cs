using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Content.Moderation
{
    public class GetPendingCoursesQuery : IRequest<IEnumerable<PendingCourseDto>>
    {
    }

    public class GetPendingCoursesHandler : IRequestHandler<GetPendingCoursesQuery, IEnumerable<PendingCourseDto>>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetPendingCoursesHandler> _logger;

        public GetPendingCoursesHandler(
            ICourseRepository courseRepo,
            IUserRepository userRepository,
            ILogger<GetPendingCoursesHandler> logger)
        {
            _courseRepo = courseRepo;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<PendingCourseDto>> Handle(GetPendingCoursesQuery request, CancellationToken ct)
        {
            try
            {
                var courses = await _courseRepo.GetAllAsync(
                    c => !c.IsPublished &&
                         (c.ModerationStatus == null || c.ModerationStatus.Status == ModerationStatus.Pending),
                    ct);
                var instructorIds = courses.Select(c => c.InstructorId).Distinct().ToList();
                var instructors = await _userRepository.GetAllAsync(
                    u => instructorIds.Contains(u.Id),
                    ct);
                return courses.Select(c =>
                {
                    var instructor = instructors.FirstOrDefault(i => i.Id == c.InstructorId);
                    return new PendingCourseDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Description = c.Description,
                        InstructorName = instructor?.FullName ?? "Unknown",
                        Category = c.Category?.Name ?? "Unknown",
                        ThumbnailUrl = c.ThumbnailUrl,
                        TotalLessons = c.Sections?.Sum(s => s.Lessons?.Count ?? 0) ?? 0,
                        Duration = FormatDuration(c.Sections?.Sum(s => s.Lessons?.Sum(l => l.Duration) ?? 0) ?? 0),
                        SubmittedAt = c.ModerationStatus?.SubmittedAt ?? c.CreatedAt
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending courses");
                return Enumerable.Empty<PendingCourseDto>();
            }
        }

        private static string FormatDuration(int totalSeconds)
        {
            var ts = TimeSpan.FromSeconds(totalSeconds);
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }
    }
    public class PendingCourseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public int TotalLessons { get; set; }
        public string Duration { get; set; } = string.Empty; // "8h 30m"
        public DateTime SubmittedAt { get; set; }
    }

    public class ApproveCourseDto
    {
        public string CourseId { get; set; } = string.Empty;
    }

    public class RejectCourseDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

}
