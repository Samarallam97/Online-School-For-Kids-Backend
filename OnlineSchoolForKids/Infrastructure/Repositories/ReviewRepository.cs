using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Entities.Reviews;
using Domain.Interfaces.Repositories;
using global::Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories;



public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    // Uses the MongoDbContext ctor on GenericRepository<T>, which derives the
    // collection name as typeof(T).Name + "s" -> "Reviews"
    public ReviewRepository(MongoDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Review> Items, long TotalCount)> GetApprovedPagedAsync(
        int skip,
        int limit,
        CancellationToken ct = default)
    {
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.IsDeleted, false),
            Builders<Review>.Filter.Eq(r => r.IsApproved, true)
        );

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _collection
            .Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}