using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(MongoDbContext context) : base(context.RefreshTokens)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(rt => rt.Token == token && !rt.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken = default)
    {
        var update = Builders<RefreshToken>.Update
            .Set(rt => rt.IsRevoked, true)
            .Set(rt => rt.RevokedAt, DateTime.UtcNow)
            .Set(rt => rt.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateManyAsync(
            rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsDeleted,
            update,
            cancellationToken: cancellationToken);
    }

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var update = Builders<RefreshToken>.Update
            .Set(rt => rt.IsRevoked, true)
            .Set(rt => rt.RevokedAt, DateTime.UtcNow)
            .Set(rt => rt.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(
            rt => rt.Token == token && !rt.IsDeleted,
            update,
            cancellationToken: cancellationToken);
    }
}
