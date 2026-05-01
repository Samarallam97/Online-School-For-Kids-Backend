using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries
{
    public class GetUserStatsQuery : IRequest<UserStatsDto?>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, UserStatsDto?>
    {
        private readonly IUserPointsRepository _userPointsRepo;
        private readonly IBadgeRepository _badgeRepo;
        private readonly ILogger<GetUserStatsHandler> _logger;

        public GetUserStatsHandler(
            IUserPointsRepository userPointsRepo,
            IBadgeRepository badgeRepo,
            ILogger<GetUserStatsHandler> logger)
        {
            _userPointsRepo = userPointsRepo;
            _badgeRepo = badgeRepo;
            _logger = logger;
        }

        public async Task<UserStatsDto?> Handle(GetUserStatsQuery request, CancellationToken ct)
        {
            try
            {
                var userPoints = await _userPointsRepo.GetOneAsync(
                    up => up.UserId == request.UserId,
                    ct);

                if (userPoints == null) return null;

                var allBadges = await _badgeRepo.GetAllAsync(b => b.IsActive, ct);
                var earnedBadgeIds = userPoints.BadgesEarned.ToHashSet();

                var badges = allBadges.Select(b => new BadgeDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    Icon = b.Icon,
                    Category = b.Category.ToString(),
                    IsEarned = earnedBadgeIds.Contains(b.Id),
                    RequirementDescription = b.Requirement.Description
                }).ToList();

                return new UserStatsDto
                {
                    TotalPoints = userPoints.TotalPoints,
                    WeeklyPoints = userPoints.WeeklyPoints,
                    MonthlyPoints = userPoints.MonthlyPoints,
                    CurrentStreak = userPoints.CurrentStreak,
                    LongestStreak = userPoints.LongestStreak,
                    CoursesCompleted = userPoints.CoursesCompleted,
                    Rank = userPoints.Rank,
                    RankChange = userPoints.Rank - userPoints.PreviousRank,
                    Badges = badges
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return null;
            }
        }
    }
    public class UserStatsDto
    {
        public int TotalPoints { get; set; }
        public int WeeklyPoints { get; set; }
        public int MonthlyPoints { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int CoursesCompleted { get; set; }
        public int Rank { get; set; }
        public int RankChange { get; set; }
        public List<BadgeDto> Badges { get; set; } = new();
    }
    public class BadgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime? EarnedAt { get; set; }
        public bool IsEarned { get; set; }
        public int Progress { get; set; } // 0-100
        public string RequirementDescription { get; set; } = string.Empty;
    }
    public class GetLeaderboardQuery : IRequest<LeaderboardDto>
    {
        public string UserId { get; set; } = string.Empty;
        public string Period { get; set; } = "AllTime"; // ThisWeek, ThisMonth, AllTime
        public int Limit { get; set; } = 100;
    }

    public class GetLeaderboardHandler : IRequestHandler<GetLeaderboardQuery, LeaderboardDto>
    {
        private readonly IUserPointsRepository _userPointsRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<GetLeaderboardHandler> _logger;

        public GetLeaderboardHandler(
            IUserPointsRepository userPointsRepo,
            IUserRepository userRepo,
            ILogger<GetLeaderboardHandler> logger)
        {
            _userPointsRepo = userPointsRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<LeaderboardDto> Handle(GetLeaderboardQuery request, CancellationToken ct)
        {
            try
            {
                var allUsers = await _userPointsRepo.GetAllAsync(_ => true, ct);
                var users = await _userRepo.GetAllAsync(_ => true, ct);
                var userDict = users.ToDictionary(u => u.Id, u => new { u.FullName, u.ProfilePictureUrl });
                // Sort based on period
                var sortedUsers = request.Period switch
                {
                    "ThisWeek" => allUsers.OrderByDescending(u => u.WeeklyPoints).ToList(),
                    "ThisMonth" => allUsers.OrderByDescending(u => u.MonthlyPoints).ToList(),
                    _ => allUsers.OrderByDescending(u => u.TotalPoints).ToList()
                };
                var entries = sortedUsers.Take(request.Limit).Select((user, index) =>
                {
  
                var userName = userDict.ContainsKey(user.UserId)
                    ? userDict[user.UserId].FullName
                    : user.UserName; // Fallback to stored name

                var userAvatar = userDict.ContainsKey(user.UserId)
                    ? userDict[user.UserId].ProfilePictureUrl
                    : user.UserAvatar; // Fallback to stored avatar

                    return new LeaderboardEntryDto
                    {
                        Rank = index + 1,
                        UserId = user.UserId,
                        UserName = userName,
                        UserAvatar = user.UserAvatar,
                        Points = request.Period switch
                        {
                            "ThisWeek" => user.WeeklyPoints,
                            "ThisMonth" => user.MonthlyPoints,
                            _ => user.TotalPoints
                        },
                        Streak = user.CurrentStreak,
                        CoursesCompleted = user.CoursesCompleted,
                        BadgesCount = user.BadgesEarned.Count,
                        RankChange = user.Rank - user.PreviousRank,
                        IsCurrentUser = user.UserId == request.UserId
                    };
                }).ToList();

                var topThree = entries.Take(3).ToList();
                var currentUser = entries.FirstOrDefault(e => e.UserId == request.UserId);

                return new LeaderboardDto
                {
                    TopThree = topThree,
                    Entries = entries,
                    CurrentUser = currentUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leaderboard");
                return new LeaderboardDto();
            }
        }
    }
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
        public int Points { get; set; }
        public int Streak { get; set; }
        public int CoursesCompleted { get; set; }
        public int BadgesCount { get; set; }
        public int RankChange { get; set; } // +2, -1, 0
        public bool IsCurrentUser { get; set; }
    }

    public class LeaderboardDto
    {
        public List<LeaderboardEntryDto> TopThree { get; set; } = new(); // Podium
        public List<LeaderboardEntryDto> Entries { get; set; } = new(); // Full list
        public LeaderboardEntryDto? CurrentUser { get; set; }
    }

}
