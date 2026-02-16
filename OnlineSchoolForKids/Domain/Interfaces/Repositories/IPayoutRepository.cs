using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces.Repositories;


public interface IPayoutRepository : IGenericRepository<Payout>
{
    Task<Payout?> GetNextPayoutAsync(string creatorId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Payout> Payouts, long TotalCount)> GetByCreatorIdPagedAsync(
        string creatorId,
        PayoutStatus? status = null,
        int? skip = null,
        int? limit = null,
        CancellationToken cancellationToken = default);
}

