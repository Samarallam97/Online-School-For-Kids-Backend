using Domain.Entities.Chatbot;

namespace Domain.Interfaces.Repositories
{
    public interface IPendingQuestionRepository : IGenericRepository<PendingQuestion>
    {
        Task<(IEnumerable<PendingQuestion> Items, long TotalCount)> GetPagedAsync(
            PendingQuestionStatus? status,
            int skip,
            int limit,
            CancellationToken ct = default);
    }
}
