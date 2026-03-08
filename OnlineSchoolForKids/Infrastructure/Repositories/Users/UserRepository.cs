using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Infrastructure.Data;
using MongoDB.Driver;


namespace Infrastructure.Repositories.Users;

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

    public async Task<User?> GetByChildInviteTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.Role, UserRole.Parent),
            Builders<User>.Filter.AnyEq(u => u.ChildInvitaions, token)
        );

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IEnumerable<User> Items, long TotalCount)> GetUsersPagedAsync(
        string? search,
        string? role,
        string? status,
        bool excludeAdmins,
        int skip,
        int limit,
        CancellationToken ct = default)
    {
        var filters = new List<FilterDefinition<User>>
        {
            Builders<User>.Filter.Eq(u => u.IsDeleted, false)
        };

        // Exclude Admin-role users for non-super-admin callers
        if (excludeAdmins)
            filters.Add(Builders<User>.Filter.Ne(u => u.Role, UserRole.Admin));

        // Full-text search on FullName and Email
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filters.Add(Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.FullName, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(term, "i"))
            ));
        }

        // Role filter
        if (!string.IsNullOrWhiteSpace(role) &&
            Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole))
            filters.Add(Builders<User>.Filter.Eq(u => u.Role, parsedRole));

        // Status filter
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<UserStatus>(status, ignoreCase: true, out var parsedStatus))
            filters.Add(Builders<User>.Filter.Eq(u => u.Status, parsedStatus));

        var combined = Builders<User>.Filter.And(filters);

        // Count uses the same filter — total is always accurate
        var totalCount = await _collection.CountDocumentsAsync(combined, cancellationToken: ct);

        var items = await _collection
            .Find(combined)
            .SortByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    // ── Admin: fetch multiple users by id list ─────────────────────────────

    public async Task<List<User>> GetManyByIdsAsync(List<string> ids, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.In(u => u.Id, ids),
            Builders<User>.Filter.Eq(u => u.IsDeleted, false)
        );
        return await _collection.Find(filter).ToListAsync(ct);
    }

    // ── Hard delete — permanently removes the document ────────────────────

    public async Task<bool> HardDeleteAsync(string id, CancellationToken ct = default)
    {
        var result = await _collection.DeleteOneAsync(
            u => u.Id == id,
            cancellationToken: ct);
        return result.DeletedCount > 0;
    }
}
