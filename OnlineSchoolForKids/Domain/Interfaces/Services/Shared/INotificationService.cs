using Domain.Enums.Content;

namespace Domain.Interfaces.Services.Shared
{

    public interface INotificationService
    {
        Task SendAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            string? actionUrl = null,
            CancellationToken ct = default);

        // ── Leaderboard ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired after RecalculateRanks when a user's rank has improved.
        /// Only sent on rank-up (new rank &lt; previous rank).
        /// </summary>
        Task SendRankAchievedNotificationAsync(
            string userId,
            int newRank,
            int previousRank,
            CancellationToken ct = default);

        /// <summary>
        /// Fired inside CheckAndAwardBadges for every newly earned badge.
        /// </summary>
        Task SendBadgeEarnedNotificationAsync(
            string userId,
            string badgeName,
            string badgeIcon,
            int bonusPoints,
            CancellationToken ct = default);

    }
}

