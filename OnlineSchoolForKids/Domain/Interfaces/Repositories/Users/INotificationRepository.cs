using Domain.Entities.Notification;

namespace Domain.Interfaces.Repositories.Users
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<(IEnumerable<Notification> Items, long TotalCount)> GetByUserIdPagedAsync(
            string userId, int skip, int limit, CancellationToken ct = default);

        Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
        Task MarkAsReadAsync(string notificationId, CancellationToken ct = default);
        Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
    }
}
