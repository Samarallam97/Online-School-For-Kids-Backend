using Domain.Entities.Chat;
using Domain.Entities.Live;

namespace Domain.Interfaces.Repositories.Content
{
    public interface ILiveSessionRepository : IGenericRepository<LiveSession>
    {
        Task<IEnumerable<LiveSession>> GetLiveSessionsAsync(CancellationToken ct = default);
        Task UpdateViewerCountAsync(string sessionId, int count, CancellationToken ct = default);
        Task EndSessionAsync(string sessionId, string? whiteboardUrl, CancellationToken ct = default);
    }

    public interface IChatMessageRepository : IGenericRepository<ChatMessage>
    {
        Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
    }

}
