// Infrastructure/Repositories/PayoutRepository.cs
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public class PayoutRepository : GenericRepository<Payout>, IPayoutRepository
{
    public PayoutRepository(MongoDbContext context) : base(context.Payouts)
    {
    }

    public async Task<Payout?> GetNextPayoutAsync(string creatorId, CancellationToken cancellationToken = default)
    {
        return await GetOneAsync(
            p => p.CreatorId == creatorId && p.Status == PayoutStatus.Pending,
            cancellationToken
        );
    }

    public async Task<(IEnumerable<Payout> Payouts, long TotalCount)> GetByCreatorIdPagedAsync(
        string creatorId,
        PayoutStatus? status = null,
        int? skip = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // Build filter
        Expression<Func<Payout, bool>> filter = p => p.CreatorId == creatorId;

        if (status.HasValue)
        {
            filter = p => p.CreatorId == creatorId && p.Status == status.Value;
        }

        // Use the base GetPagedAsync method
        return await GetPagedAsync(
            filter: filter,
            orderBy: p => p.ScheduledDate,
            orderByDescending: true,
            skip: skip,
            limit: limit,
            cancellationToken: cancellationToken
        );
    }
}