using Domain.Entities;
using Domain.Interfaces.Repositories;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;

    public GenericRepository(IMongoCollection<T> collection)
    {
        _collection = collection;
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(e => e.Id == id && !e.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<T?> GetOneAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        var deletedFilter = Builders<T>.Filter.Eq(e => e.IsDeleted, false);
        var combinedFilter = Builders<T>.Filter.And(deletedFilter, filter);

        return await _collection
            .Find(combinedFilter)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var deletedFilter = Builders<T>.Filter.Eq(e => e.IsDeleted, false);

        FilterDefinition<T> combinedFilter = filter == null
            ? deletedFilter
            : Builders<T>.Filter.And(deletedFilter, filter);

        return await _collection
            .Find(combinedFilter)
            .ToListAsync(cancellationToken);
    }

    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task<bool> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;

        var result = await _collection.ReplaceOneAsync(
            e => e.Id == id && !e.IsDeleted,
            entity,
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        // Soft delete
        var update = Builders<T>.Update
            .Set(e => e.IsDeleted, true)
            .Set(e => e.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            e => e.Id == id && !e.IsDeleted,
            update,
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        var deletedFilter = Builders<T>.Filter.Eq(e => e.IsDeleted, false);
        var combinedFilter = Builders<T>.Filter.And(deletedFilter, filter);

        var count = await _collection.CountDocumentsAsync(combinedFilter, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task<long> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        var deletedFilter = Builders<T>.Filter.Eq(e => e.IsDeleted, false);

        FilterDefinition<T> combinedFilter = filter == null
            ? deletedFilter
            : Builders<T>.Filter.And(deletedFilter, filter);

        return await _collection.CountDocumentsAsync(combinedFilter, cancellationToken: cancellationToken);
    }

    public async Task<(IEnumerable<T> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool orderByDescending = false,
        int? skip = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var deletedFilter = Builders<T>.Filter.Eq(e => e.IsDeleted, false);
        FilterDefinition<T> combinedFilter = filter == null
            ? deletedFilter
            : Builders<T>.Filter.And(deletedFilter, filter);

        // Get total count
        var totalCount = await _collection.CountDocumentsAsync(combinedFilter, cancellationToken: cancellationToken);

        // Build query
        var query = _collection.Find(combinedFilter);

        // Apply sorting
        if (orderBy != null)
        {
            query = orderByDescending
                ? query.SortByDescending(orderBy)
                : query.SortBy(orderBy);
        }

        // Apply pagination
        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (limit.HasValue)
            query = query.Limit(limit.Value);

        var items = await query.ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
