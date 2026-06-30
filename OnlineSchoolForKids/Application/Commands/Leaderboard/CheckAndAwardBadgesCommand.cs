using Domain.Entities.Content.Leaderboard;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Services.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Leaderboard;

public class CheckAndAwardBadgesCommand : IRequest<List<string>>
{
    public string UserId { get; set; } = string.Empty;
}

public class CheckAndAwardBadgesHandler : IRequestHandler<CheckAndAwardBadgesCommand, List<string>>
{
    private readonly IUserPointsRepository _userPointsRepo;
    private readonly IBadgeRepository _badgeRepo;
    private readonly IPointTransactionRepository _transactionRepo;
    private readonly INotificationService _notification;
    private readonly ILogger<CheckAndAwardBadgesHandler> _logger;

    public CheckAndAwardBadgesHandler(
        IUserPointsRepository userPointsRepo,
        IBadgeRepository badgeRepo,
        IPointTransactionRepository transactionRepo,
        INotificationService notification,
        ILogger<CheckAndAwardBadgesHandler> logger)
    {
        _userPointsRepo = userPointsRepo;
        _badgeRepo = badgeRepo;
        _transactionRepo = transactionRepo;
        _notification = notification;
        _logger = logger;
    }

    public async Task<List<string>> Handle(CheckAndAwardBadgesCommand request, CancellationToken ct)
    {
        var newBadges = new List<string>();

        try
        {
            // 1. Load user points
            var userPoints = await _userPointsRepo.GetOneAsync(
                up => up.UserId == request.UserId, ct);

            if (userPoints is null)
            {
                _logger.LogWarning("UserPoints not found for User {UserId}", request.UserId);
                return newBadges;
            }

            // 2. Get all active badges
            var allBadges = await _badgeRepo.GetAllAsync(b => b.IsActive, ct);

            var notifyTasks = new List<Task>();

            // 3. Check each badge
            foreach (var badge in allBadges)
            {
                if (userPoints.BadgesEarned.Contains(badge.Id.ToString()))
                    continue; // already earned

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

                if (!qualifies) continue;

                // Award badge
                const int badgeBonus = 150;

                userPoints.BadgesEarned.Add(badge.Id.ToString());
                newBadges.Add(badge.Id);

                userPoints.TotalPoints += badgeBonus;
                userPoints.WeeklyPoints += badgeBonus;
                userPoints.MonthlyPoints += badgeBonus;

                // Record transaction
                var transaction = new PointTransaction
                {
                    UserId = request.UserId,
                    Points = badgeBonus,
                    Reason = PointReason.BadgeEarned,
                    Description = $"Earned badge: {badge.Name}",
                    RelatedEntityId = badge.Id
                };
                await _transactionRepo.CreateAsync(transaction, ct);

                _logger.LogInformation(
                    "Badge awarded: {BadgeName} to User {UserId}", badge.Name, request.UserId);

                // Queue in-app notification for this badge
                notifyTasks.Add(
                    _notification.SendBadgeEarnedNotificationAsync(
                        request.UserId,
                        badge.Name,
                        badge.Icon,
                        badgeBonus,
                        ct));
            }

            // 4. Persist updated user points
            if (newBadges.Count > 0)
                await _userPointsRepo.UpdateAsync(userPoints.Id, userPoints, ct);

            // 5. Fire all badge notifications concurrently (non-blocking)
            if (notifyTasks.Count > 0)
                _ = Task.WhenAll(notifyTasks);

            return newBadges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking badges for User {UserId}", request.UserId);
            return newBadges;
        }
    }
}
