using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(MongoDbContext context) : base(context.Users)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(u => u.Email == email.ToLower() && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(u => u.GoogleId == googleId && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(u => u.EmailVerificationToken == token && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(u => u.PasswordResetToken == token && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(
            u => u.Email == email.ToLower() && !u.IsDeleted,
            cancellationToken: cancellationToken);

        return count > 0;
    }
}
