using Domain.Entities.Notification;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Shared
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repo,
            IHubContext<NotificationHub> hub,
            ILogger<NotificationService> logger)
        {
            _repo = repo;
            _hub = hub;
            _logger = logger;
        }

        // ── Core send ─────────────────────────────────────────────────────────

        public async Task SendAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            string? actionUrl = null,
            CancellationToken ct = default)
        {
            // Step 1: Save to MongoDB — always persisted first.
            // If the user is offline they will get it via the REST API
            // when they next open the app.
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl
            };

            try
            {
                await _repo.CreateAsync(notification, ct);
                _logger.LogInformation(
                    "Notification [{Type}] saved for user {UserId} — \"{Title}\"",
                    type, userId, title);
            }
            catch (Exception ex)
            {
                // If DB save fails, do NOT send a ghost real-time notification.
                _logger.LogError(ex,
                    "Failed to save notification [{Type}] for user {UserId}", type, userId);
                return;
            }

            // Step 2: Push via SignalR (fire-and-forget).
            // If the user is offline, SignalR finds no connections in their group
            // and silently does nothing. The notification is already in MongoDB.
            _ = PushToUserAsync(userId, notification);
        }

        private async Task PushToUserAsync(string userId, Notification n)
        {
            try
            {
                await _hub.Clients
                    .Group(userId)   // all active tabs / devices for this user
                    .SendAsync("ReceiveNotification", new
                    {
                        id = n.Id,
                        title = n.Title,
                        message = n.Message,
                        type = n.Type.ToString(),
                        actionUrl = n.ActionUrl,
                        isRead = false,
                        createdAt = n.CreatedAt
                    });

                _logger.LogInformation(
                    "SignalR push sent to user {UserId} — [{Type}]", userId, n.Type);
            }
            catch (Exception ex)
            {
                // Real-time push is best-effort; notification is already in MongoDB.
                _logger.LogWarning(ex,
                    "SignalR push failed for user {UserId} — notification {Id} is still in DB",
                    userId, n.Id);
            }
        }

        // ── Account ───────────────────────────────────────────────────────────

        public Task SendWelcomeNotificationAsync(
            string userId,
            string userName,
            CancellationToken ct = default) =>
            SendAsync(
                userId,
                "Welcome aboard! 🎉",
                $"Hi {userName}, your account is all set. Explore courses or book a specialist session to get started.",
                NotificationType.General,
                "/dashboard",
                ct);

        // ── Session ───────────────────────────────────────────────────────────

        public async Task SendSessionConfirmedNotificationAsync(
            string studentId,
            string specialistId,
            string sessionTitle,
            string sessionDate,
            string startTime,
            string sessionId,
            CancellationToken ct = default)
        {
            var dateTime = $"{sessionDate} at {startTime} UTC";
            var sessionUrl = $"/sessions/{sessionId}";

            await SendAsync(
                studentId,
                $"Session confirmed: {sessionTitle} ✅",
                $"Your session is confirmed for {dateTime}. Join via Google Meet when the time comes.",
                NotificationType.BookingConfirmed,
                sessionUrl,
                ct);

            await SendAsync(
                specialistId,
                $"New session booked: {sessionTitle}",
                $"A student has confirmed a session with you on {dateTime}.",
                NotificationType.BookingConfirmed,
                sessionUrl,
                ct);
        }

        public async Task SendSessionCancelledNotificationAsync(
            string studentId,
            string specialistId,
            string sessionTitle,
            string sessionDate,
            string startTime,
            string sessionId,
            string? reason,
            bool refundIssued,
            CancellationToken ct = default)
        {
            var reasonLine = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Reason: {reason}.";
            var refundLine = refundIssued ? " A full refund has been issued." : string.Empty;
            var sessionUrl = $"/sessions/{sessionId}";

            await SendAsync(
                studentId,
                $"Session cancelled: {sessionTitle}",
                $"Your session on {sessionDate} at {startTime} UTC was cancelled.{reasonLine}{refundLine}",
                NotificationType.BookingCancelled,
                sessionUrl,
                ct);

            await SendAsync(
                specialistId,
                $"Session cancelled: {sessionTitle}",
                $"The session on {sessionDate} at {startTime} UTC has been cancelled.{reasonLine}",
                NotificationType.BookingCancelled,
                sessionUrl,
                ct);
        }

        // ── Leaderboard ───────────────────────────────────────────────────────

        public Task SendRankAchievedNotificationAsync(
            string userId,
            int newRank,
            int previousRank,
            CancellationToken ct = default)
        {
            var (title, message) = newRank switch
            {
                1 => (
                    "🥇 You're #1 on the leaderboard!",
                    $"You climbed from #{previousRank} to the top spot. Incredible work — keep it up!"),
                2 => (
                    "🥈 You reached #2 on the leaderboard!",
                    $"You moved up from #{previousRank} to #2. You're just one step away from the top!"),
                3 => (
                    "🥉 You're in the Top 3!",
                    $"You climbed from #{previousRank} to #3. You're on the podium!"),
                <= 10 => (
                    $"🔥 You're in the Top 10! (#{newRank})",
                    $"You jumped from #{previousRank} to #{newRank} on the leaderboard."),
                _ => (
                    $"⬆️ Rank up! You're now #{newRank}",
                    $"You climbed from #{previousRank} to #{newRank} on the leaderboard. Keep earning points!")
            };

            return SendAsync(
                userId,
                title,
                message,
                NotificationType.RankAchieved,
                "/leaderboard",
                ct);
        }

        public Task SendBadgeEarnedNotificationAsync(
            string userId,
            string badgeName,
            string badgeIcon,
            int bonusPoints,
            CancellationToken ct = default) =>
            SendAsync(
                userId,
                $"{badgeIcon} New badge: {badgeName}",
                $"You earned the \"{badgeName}\" badge and received {bonusPoints} bonus points!",
                NotificationType.BadgeEarned,
                "/profile/badges",
                ct);
    }
}
