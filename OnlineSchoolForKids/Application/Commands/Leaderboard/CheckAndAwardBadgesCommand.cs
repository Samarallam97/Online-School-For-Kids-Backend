using Domain.Entities.Content.Leaderboard;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Leaderboard
{
    public class CheckAndAwardBadgesCommand : IRequest<List<string>>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class CheckAndAwardBadgesHandler : IRequestHandler<CheckAndAwardBadgesCommand, List<string>>
    {
        private readonly IUserPointsRepository _userPointsRepo;
        private readonly IBadgeRepository _badgeRepo;
        private readonly IPointTransactionRepository _transactionRepo;
        private readonly ILogger<CheckAndAwardBadgesHandler> _logger;

        public CheckAndAwardBadgesHandler(
            IUserPointsRepository userPointsRepo,
            IBadgeRepository badgeRepo,
            IPointTransactionRepository transactionRepo,
            ILogger<CheckAndAwardBadgesHandler> logger)
        {
            _userPointsRepo = userPointsRepo;
            _badgeRepo = badgeRepo;
            _transactionRepo = transactionRepo;
            _logger = logger;
        }

        public async Task<List<string>> Handle(CheckAndAwardBadgesCommand request, CancellationToken ct)
        {
            try
            {
                var newBadges = new List<string>();

                // 1. Get user points
                var userPoints = await _userPointsRepo.GetOneAsync(
                    up => up.UserId == request.UserId,
                    ct);

                if (userPoints == null)
                {
                    _logger.LogWarning("UserPoints not found for User {UserId}", request.UserId);
                    return newBadges;
                }

                // 2. Get all active badges
                var allBadges = await _badgeRepo.GetAllAsync(b => b.IsActive, ct);

                // 3. Check each badge
                foreach (var badge in allBadges)
                {
                    // Skip if already earned
                    if (userPoints.BadgesEarned.Contains(badge.Id.ToString()))
                        continue;

                    // Check if user qualifies
                    bool qualifies = badge.Requirement.Type switch
                    {
                        BadgeRequirementType.CoursesCompleted =>
                            userPoints.CoursesCompleted >= badge.Requirement.Value,

                        BadgeRequirementType.StreakDays =>
                            userPoints.CurrentStreak >= badge.Requirement.Value,

                        BadgeRequirementType.TotalPoints =>
                            userPoints.TotalPoints >= badge.Requirement.Value,

                        _ => false
                    };

                    if (qualifies)
                    {
                        // Award badge!
                        userPoints.BadgesEarned.Add(badge.Id.ToString());
                        newBadges.Add(badge.Id);

                        // Award bonus points for earning badge
                        userPoints.TotalPoints += 150; // Badge bonus points
                        userPoints.WeeklyPoints += 150;
                        userPoints.MonthlyPoints += 150;

                        // Record transaction
                        var transaction = new PointTransaction
                        {
                            UserId = request.UserId,
                            Points = 150,
                            Reason = PointReason.BadgeEarned,
                            Description = $"Earned badge: {badge.Name}",
                            RelatedEntityId = badge.Id
                        };
                        await _transactionRepo.CreateAsync(transaction, ct);

                        _logger.LogInformation(
                            "Badge awarded: {BadgeName} to User {UserId}",
                            badge.Name, request.UserId);
                    }
                }

                // 4. Update user points if any badges earned
                if (newBadges.Any())
                {
                    await _userPointsRepo.UpdateAsync(userPoints.Id, userPoints, ct);
                }

                return newBadges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking badges for User {UserId}", request.UserId);
                return new List<string>();
            }
        }
    }
}


