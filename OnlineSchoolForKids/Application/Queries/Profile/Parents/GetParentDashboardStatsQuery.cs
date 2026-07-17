using Domain.Entities.Content.Leaderboard;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Profile.Parents;

public record GetParentDashboardStatsQuery(string ParentUserId) : IRequest<ParentDashboardStatsDto>;

public class GetParentDashboardStatsHandler : IRequestHandler<GetParentDashboardStatsQuery, ParentDashboardStatsDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseProgressRepository _courseProgressRepo;
    private readonly ILessonProgressRepository _lessonProgressRepo;
    private readonly ILessonRepository _lessonRepo;
    private readonly IUserPointsRepository _userPointsRepo;
    private readonly IBadgeRepository _badgeRepo;
    private readonly ILogger<GetParentDashboardStatsHandler> _logger;

    public GetParentDashboardStatsHandler(
        IUserRepository userRepo,
        IEnrollmentRepository enrollmentRepo,
        ICourseProgressRepository courseProgressRepo,
        ILessonProgressRepository lessonProgressRepo,
        ILessonRepository lessonRepo,
        IUserPointsRepository userPointsRepo,
        IBadgeRepository badgeRepo,
        ILogger<GetParentDashboardStatsHandler> logger)
    {
        _userRepo = userRepo;
        _enrollmentRepo = enrollmentRepo;
        _courseProgressRepo = courseProgressRepo;
        _lessonProgressRepo = lessonProgressRepo;
        _lessonRepo = lessonRepo;
        _userPointsRepo = userPointsRepo;
        _badgeRepo = badgeRepo;
        _logger = logger;
    }

    public async Task<ParentDashboardStatsDto> Handle(GetParentDashboardStatsQuery request, CancellationToken ct)
    {
        var result = new ParentDashboardStatsDto();

        var parent = await _userRepo.GetByIdAsync(request.ParentUserId, ct);
        var childIds = parent?.ChildrenIds ?? new List<string>();
        if (childIds.Count == 0)
            return result;

        var now = DateTime.UtcNow;
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => now.Date.AddDays(-6 + i))
            .ToList();
        var sevenDaysAgo = last7Days.First();

        var allBadges = (await _badgeRepo.GetAllAsync(b => b.IsActive, ct)).ToDictionary(b => b.Id, b => b);
        var weeklyChartRows = last7Days.ToDictionary(d => d, d => new Dictionary<string, object> { ["day"] = d.ToString("ddd") });

        foreach (var childId in childIds)
        {
            var child = await _userRepo.GetByIdAsync(childId, ct);
            if (child is null) continue;

            var enrollments = (await _enrollmentRepo.GetAllAsync(e => e.UserId == childId, ct)).ToList();
            var courseProgress = (await _courseProgressRepo.GetAllAsync(cp => cp.UserId == childId, ct)).ToList();
            var lessonProgress = (await _lessonProgressRepo.GetAllAsync(lp => lp.UserId == childId, ct)).ToList();
            var points = await _userPointsRepo.GetOneAsync(up => up.UserId == childId, ct);

            double overallProgress = courseProgress.Count > 0
                ? Math.Round(courseProgress.Average(cp => cp.ProgressPercentage), 0)
                : 0;

            var thisWeekLessons = lessonProgress.Where(lp => lp.LastAccessedAt >= sevenDaysAgo).ToList();
            double hoursThisWeek = Math.Round(thisWeekLessons.Sum(lp => lp.TimeSpent) / 3600.0, 1);

            // Recent activity: most recently completed lesson
            var lastCompleted = lessonProgress
                .Where(lp => lp.IsCompleted && lp.CompletedAt.HasValue)
                .OrderByDescending(lp => lp.CompletedAt)
                .FirstOrDefault();
            string? recentActivityText = null;
            DateTime? recentActivityAt = null;
            if (lastCompleted is not null)
            {
                var lesson = await _lessonRepo.GetByIdAsync(lastCompleted.LessonId, ct);
                recentActivityText = lesson is not null ? $"Completed: {lesson.Title}" : "Completed a lesson";
                recentActivityAt = lastCompleted.CompletedAt;
            }

            int age = child.DateOfBirth == default
                ? 0
                : now.Year - child.DateOfBirth.Year - (now.Date < child.DateOfBirth.AddYears(now.Year - child.DateOfBirth.Year) ? 1 : 0);

            result.Children.Add(new ChildOverviewDto
            {
                Id = child.Id,
                Name = child.FullName,
                Age = age,
                AvatarUrl = child.ProfilePictureUrl,
                CoursesEnrolled = enrollments.Count,
                HoursThisWeek = hoursThisWeek,
                OverallProgress = overallProgress,
                Streak = points?.CurrentStreak ?? 0,
                RecentActivity = recentActivityText
            });

            if (recentActivityAt.HasValue)
            {
                result.RecentActivity.Add(new ChildActivityDto
                {
                    ChildName = child.FullName,
                    Description = recentActivityText!,
                    Date = recentActivityAt.Value
                });
            }

            // Badges earned by this child
            foreach (var badgeId in points?.BadgesEarned ?? new List<string>())
            {
                if (allBadges.TryGetValue(badgeId, out var badge))
                {
                    result.RecentAchievements.Add(new ChildAchievementDto
                    {
                        ChildName = child.FullName,
                        Title = badge.Name,
                        Description = badge.Description,
                        Icon = badge.Icon
                    });
                }
            }

            // Weekly hours-per-day for the chart, keyed by child id
            foreach (var day in last7Days)
            {
                var seconds = thisWeekLessons
                    .Where(lp => lp.LastAccessedAt.Date == day)
                    .Sum(lp => lp.TimeSpent);
                weeklyChartRows[day][child.Id] = Math.Round(seconds / 3600.0, 2);
            }
        }

        result.RecentActivity = result.RecentActivity.OrderByDescending(a => a.Date).Take(6).ToList();
        result.RecentAchievements = result.RecentAchievements.Take(6).ToList();
        result.WeeklyHoursChartData = last7Days.Select(d => weeklyChartRows[d]).ToList();

        return result;
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class ParentDashboardStatsDto
{
    public List<ChildOverviewDto> Children { get; set; } = new();
    /// <summary>Recharts-ready rows: {"day":"Mon","&lt;childId&gt;":1.5, ...}</summary>
    public List<Dictionary<string, object>> WeeklyHoursChartData { get; set; } = new();
    public List<ChildAchievementDto> RecentAchievements { get; set; } = new();
    public List<ChildActivityDto> RecentActivity { get; set; } = new();
}

public class ChildOverviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? AvatarUrl { get; set; }
    public int CoursesEnrolled { get; set; }
    public double HoursThisWeek { get; set; }
    public double OverallProgress { get; set; }
    public int Streak { get; set; }
    public string? RecentActivity { get; set; }
}

public class ChildAchievementDto
{
    public string ChildName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class ChildActivityDto
{
    public string ChildName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}