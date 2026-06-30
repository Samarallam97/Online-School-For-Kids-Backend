using Domain.Entities.Notification;
using Domain.Interfaces.Repositories.Users;
using Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories.Users
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(MongoDbContext context)
            : base(context.GetCollection<Notification>("notifications")) { }

        public async Task<(IEnumerable<Notification> Items, long TotalCount)> GetByUserIdPagedAsync(
            string userId, int skip, int limit, CancellationToken ct = default)
        {
            return await GetPagedAsync(
                filter: n => n.UserId == userId,
                orderBy: n => n.CreatedAt,
                orderByDescending: true,
                skip: skip,
                limit: limit,
                cancellationToken: ct);
        }

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        {
            var count = await CountAsync(n => n.UserId == userId && !n.IsRead, ct);
            return (int)count;
        }

        public async Task MarkAsReadAsync(string notificationId, CancellationToken ct = default)
        {
            var update = Builders<Notification>.Update
                .Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow)
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                n => n.Id == notificationId && !n.IsDeleted, update, cancellationToken: ct);
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
        {
            var update = Builders<Notification>.Update
                .Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow)
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateManyAsync(
                n => n.UserId == userId && !n.IsRead && !n.IsDeleted, update, cancellationToken: ct);
        }
    }
}