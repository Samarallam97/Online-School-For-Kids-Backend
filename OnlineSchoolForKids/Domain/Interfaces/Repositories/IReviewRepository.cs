using Domain.Entities.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces.Repositories;

public interface IReviewRepository : IGenericRepository<Review>
{
    Task<(IEnumerable<Review> Items, long TotalCount)> GetApprovedPagedAsync(
        int skip,
        int limit,
        CancellationToken ct = default);
}