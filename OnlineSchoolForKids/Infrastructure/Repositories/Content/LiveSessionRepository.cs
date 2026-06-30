using Domain.Entities.Chat;
using Domain.Entities.Live;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories.Content
{
    public class LiveSessionRepository : GenericRepository<LiveSession>, ILiveSessionRepository
    {
        public LiveSessionRepository(MongoDbContext context)
            : base(context.GetCollection<LiveSession>("live_sessions")) { }

        public async Task<IEnumerable<LiveSession>> GetLiveSessionsAsync(CancellationToken ct = default)
        {
            return await _collection
                .Find(s => s.Status == "live" && !s.IsDeleted)
                .SortByDescending(s => s.StartedAt)
                .ToListAsync(ct);
        }

        public async Task UpdateViewerCountAsync(string sessionId, int count, CancellationToken ct = default)
        {
            var update = Builders<LiveSession>.Update
                .Set(s => s.ViewerCount, count)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(s => s.Id == sessionId, update, cancellationToken: ct);
        }

        public async Task EndSessionAsync(string sessionId, string? whiteboardUrl, CancellationToken ct = default)
        {
            var update = Builders<LiveSession>.Update
                .Set(s => s.Status, "ended")
                .Set(s => s.EndedAt, DateTime.UtcNow)
                .Set(s => s.UpdatedAt, DateTime.UtcNow)
                .Set(s => s.WhiteboardUrl, whiteboardUrl);

            await _collection.UpdateOneAsync(s => s.Id == sessionId, update, cancellationToken: ct);
        }
    }

    public class ChatMessageRepository : GenericRepository<ChatMessage>, IChatMessageRepository
    {
        public ChatMessageRepository(MongoDbContext context)
            : base(context.GetCollection<ChatMessage>("chat_messages")) { }

        public async Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(
            string sessionId, CancellationToken ct = default)
        {
            return await _collection
                .Find(m => m.SessionId == sessionId && !m.IsDeleted)
                .SortBy(m => m.CreatedAt)
                .ToListAsync(ct);
        }
    }

}
