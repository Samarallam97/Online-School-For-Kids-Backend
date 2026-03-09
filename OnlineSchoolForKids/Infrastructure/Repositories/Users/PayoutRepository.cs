using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Infrastructure.Data;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Infrastructure.Repositories.Users;

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
        Expression<Func<Payout, bool>> filter = p => p.CreatorId == creatorId;

        if (status.HasValue)
        {
            filter = p => p.CreatorId == creatorId && p.Status == status.Value;
        }

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