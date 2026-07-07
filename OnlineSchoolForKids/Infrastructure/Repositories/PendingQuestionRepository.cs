using Domain.Entities.Chatbot;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class PendingQuestionRepository
     : GenericRepository<PendingQuestion>, IPendingQuestionRepository
    {
        public PendingQuestionRepository(MongoDbContext context)
            : base(context.GetCollection<PendingQuestion>("pending_questions")) { }

        public async Task<(IEnumerable<PendingQuestion> Items, long TotalCount)> GetPagedAsync(
            PendingQuestionStatus? status,
            int skip,
            int limit,
            CancellationToken ct = default)
        {
            return await GetPagedAsync(
                filter: q => status == null || q.Status == status,
                orderBy: q => q.CreatedAt,
                orderByDescending: true,
                skip: skip,
                limit: limit,
                cancellationToken: ct);
        }
    }

}
