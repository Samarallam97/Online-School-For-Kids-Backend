using Domain.Entities.Content;
using Domain.Entities.Content.Orders;
using Domain.Entities.Users;
using Domain.Enums.Content;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Admin;

public record GetAdminDashboardStatsQuery : IRequest<AdminDashboardStatsDto>;

public class GetAdminDashboardStatsHandler : IRequestHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IMediator _mediator;
    private readonly ILogger<GetAdminDashboardStatsHandler> _logger;

    public GetAdminDashboardStatsHandler(
        IUserRepository userRepo,
        IOrderRepository orderRepo,
        ICourseRepository courseRepo,
        ICategoryRepository categoryRepo,
        IEnrollmentRepository enrollmentRepo,
        IMediator mediator,
        ILogger<GetAdminDashboardStatsHandler> logger)
    {
        _userRepo = userRepo;
        _orderRepo = orderRepo;
        _courseRepo = courseRepo;
        _categoryRepo = categoryRepo;
        _enrollmentRepo = enrollmentRepo;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<AdminDashboardStatsDto> Handle(GetAdminDashboardStatsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfThisMonth.AddMonths(-1);

        var users = (await _userRepo.GetAllAsync(u => !u.IsDeleted, ct)).ToList();
        var orders = (await _orderRepo.GetAllAsync(o => !o.IsDeleted, ct)).ToList();
        var courses = (await _courseRepo.GetAllAsync(c => !c.IsDeleted, ct)).ToList();
        var categories = (await _categoryRepo.GetAllAsync(c => !c.IsDeleted, ct)).ToList();
        var enrollments = (await _enrollmentRepo.GetAllAsync(e => !e.IsDeleted, ct)).ToList();

        var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

        // ── Stat cards + month-over-month change ────────────────────────────
        int newUsersThisMonth = users.Count(u => u.CreatedAt >= startOfThisMonth);
        int newUsersLastMonth = users.Count(u => u.CreatedAt >= startOfLastMonth && u.CreatedAt < startOfThisMonth);

        decimal revenueThisMonth = completedOrders.Where(o => o.CreatedAt >= startOfThisMonth).Sum(o => o.Total);
        decimal revenueLastMonth = completedOrders
            .Where(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfThisMonth)
            .Sum(o => o.Total);

        int coursesThisMonth = courses.Count(c => c.CreatedAt >= startOfThisMonth);
        int coursesLastMonth = courses.Count(c => c.CreatedAt >= startOfLastMonth && c.CreatedAt < startOfThisMonth);

        var last30 = now.AddDays(-30);
        var prev30 = now.AddDays(-60);
        int activeNow = users.Count(u => u.LastLoginAt >= last30);
        int activePrev = users.Count(u => u.LastLoginAt >= prev30 && u.LastLoginAt < last30);

        // ── Monthly trends (last 7 months, oldest first) ────────────────────
        var months = Enumerable.Range(0, 7)
            .Select(i => startOfThisMonth.AddMonths(-6 + i))
            .ToList();

        var userRegistrations = months.Select(m => new MonthlyCountDto
        {
            Month = m.ToString("MMM"),
            Value = users.Count(u => u.CreatedAt.Year == m.Year && u.CreatedAt.Month == m.Month)
        }).ToList();

        var revenueByMonth = months.Select(m => new MonthlyRevenueDto
        {
            Month = m.ToString("MMM"),
            Revenue = completedOrders
                .Where(o => o.CreatedAt.Year == m.Year && o.CreatedAt.Month == m.Month)
                .Sum(o => o.Total)
        }).ToList();

        // ── Enrollments by category ──────────────────────────────────────────
        var courseCategoryLookup = courses.ToDictionary(c => c.Id, c => c.CategoryId);
        var categoryNameLookup = categories.ToDictionary(c => c.Id, c => c.Name);

        var enrollmentsByCategory = enrollments
            .Where(e => courseCategoryLookup.ContainsKey(e.CourseId))
            .GroupBy(e => courseCategoryLookup[e.CourseId])
            .Select(g => new CategoryEnrollmentDto
            {
                Name = categoryNameLookup.GetValueOrDefault(g.Key, "Uncategorized"),
                Enrollments = g.Count()
            })
            .OrderByDescending(x => x.Enrollments)
            .Take(6)
            .ToList();

        // ── Role distribution ────────────────────────────────────────────────
        var roleDistribution = users
            .GroupBy(u => u.Role)
            .Select(g => new RoleDistributionDto { Name = g.Key.ToString(), Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToList();

        // ── Recent transactions ──────────────────────────────────────────────
        var userNameLookup = users.ToDictionary(u => u.Id, u => u.FullName);
        var recentTransactions = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(8)
            .Select(o => new RecentTransactionDto
            {
                Id = o.Id,
                UserName = userNameLookup.GetValueOrDefault(o.UserId, "Unknown"),
                CourseName = o.Items.FirstOrDefault()?.CourseTitle
                    ?? (o.Items.Count > 1 ? $"{o.Items.Count} courses" : "—"),
                Amount = o.Total,
                Date = o.CreatedAt,
                Status = o.Status.ToString().ToLowerInvariant()
            })
            .ToList();

        // ── Recent registrations ─────────────────────────────────────────────
        var recentRegistrations = users
            .OrderByDescending(u => u.CreatedAt)
            .Take(8)
            .Select(u => new RecentRegistrationDto
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                Date = u.CreatedAt
            })
            .ToList();

        // ── Pending reviews badge (reuse moderation stats) ───────────────────
        int pendingReviews = 0;
        try
        {
            var modStats = await _mediator.Send(
                new Application.Queries.Content.Moderation.GetModerationStatsQuery(), ct);
            pendingReviews = modStats.PendingCourses + modStats.ReportedContent + modStats.FlaggedComments;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load moderation stats for admin dashboard");
        }

        return new AdminDashboardStatsDto
        {
            TotalUsers = users.Count,
            TotalUsersChangePercent = PercentChange(newUsersLastMonth, newUsersThisMonth),
            ActiveUsers = activeNow,
            ActiveUsersChangePercent = PercentChange(activePrev, activeNow),
            TotalRevenue = completedOrders.Sum(o => o.Total),
            TotalRevenueChangePercent = PercentChange(revenueLastMonth, revenueThisMonth),
            TotalCourses = courses.Count,
            TotalCoursesChangePercent = PercentChange(coursesLastMonth, coursesThisMonth),
            PendingReviews = pendingReviews,
            UserRegistrations = userRegistrations,
            Revenue = revenueByMonth,
            EnrollmentsByCategory = enrollmentsByCategory,
            RoleDistribution = roleDistribution,
            RecentTransactions = recentTransactions,
            RecentRegistrations = recentRegistrations
        };
    }

    private static double PercentChange(decimal previous, decimal current)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return (double)Math.Round(((current - previous) / previous) * 100, 1);
    }

    private static double PercentChange(int previous, int current) => PercentChange((decimal)previous, current);
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class AdminDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public double TotalUsersChangePercent { get; set; }
    public int ActiveUsers { get; set; }
    public double ActiveUsersChangePercent { get; set; }
    public decimal TotalRevenue { get; set; }
    public double TotalRevenueChangePercent { get; set; }
    public int TotalCourses { get; set; }
    public double TotalCoursesChangePercent { get; set; }
    public int PendingReviews { get; set; }

    public List<MonthlyCountDto> UserRegistrations { get; set; } = new();
    public List<MonthlyRevenueDto> Revenue { get; set; } = new();
    public List<CategoryEnrollmentDto> EnrollmentsByCategory { get; set; } = new();
    public List<RoleDistributionDto> RoleDistribution { get; set; } = new();
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    public List<RecentRegistrationDto> RecentRegistrations { get; set; } = new();
}

public class MonthlyCountDto
{
    public string Month { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class CategoryEnrollmentDto
{
    public string Name { get; set; } = string.Empty;
    public int Enrollments { get; set; }
}

public class RoleDistributionDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class RecentTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RecentRegistrationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}