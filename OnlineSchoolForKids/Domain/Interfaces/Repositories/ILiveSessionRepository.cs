using Domain.Entities;
using Domain.Entities.Chat;

namespace Domain.Interfaces.Repositories
{
    public interface ILiveSessionRepository : IGenericRepository<LiveSession>
    {
        /// <summary>Gets the live session tied to a specific lesson, if one exists.</summary>
        Task<LiveSession?> GetByLessonIdAsync(string lessonId, CancellationToken ct = default);

        Task UpdateViewerCountAsync(string sessionId, int count, CancellationToken ct = default);
        Task EndSessionAsync(string sessionId, string? whiteboardUrl, CancellationToken ct = default);
    }

    public interface IChatMessageRepository : IGenericRepository<ChatMessage>
    {
        /// <summary>
        /// Returns all messages for a live session
        /// (ContextType = LiveSession and ContextId = sessionId).
        /// </summary>
        Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
    }

}
