using Domain.Entities;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Infrastructure.Settings;

namespace Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);

        // Create indexes on startup
        CreateIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshTokens");

    private void CreateIndexes()
    {
        // User indexes
        var userIndexes = Users.Indexes;

        // Email index (unique, case-insensitive)
        var emailIndexModel = new CreateIndexModel<User>(
         Builders<User>.IndexKeys.Ascending(u => u.Email),
         new CreateIndexOptions
         {
             Unique = true,
             Collation = new Collation("en", strength: CollationStrength.Primary) 
         }
        ); 

        // Google ID index
        var googleIdIndexModel = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.GoogleId),
            new CreateIndexOptions { Sparse = true }
        );

        // Email verification token index
        var emailTokenIndexModel = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.EmailVerificationToken),
            new CreateIndexOptions { Sparse = true }
        );

        // Password reset token index
        var resetTokenIndexModel = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.PasswordResetToken),
            new CreateIndexOptions { Sparse = true }
        );

        // Role index for queries
        var roleIndexModel = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Role)
        );

        userIndexes.CreateMany(new[]
        {
            emailIndexModel,
            googleIdIndexModel,
            emailTokenIndexModel,
            resetTokenIndexModel,
            roleIndexModel
        });

        // RefreshToken indexes
        var tokenIndexes = RefreshTokens.Indexes;

        // Token index (unique)
        var tokenIndexModel = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.Token),
            new CreateIndexOptions { Unique = true }
        );

        // UserId index for user lookups
        var userIdIndexModel = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.UserId)
        );

        // Expiry index with TTL (auto-delete expired tokens after 7 days)
        var expiryIndexModel = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.ExpiresAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(7) }
        );

        tokenIndexes.CreateMany(new[]
        {
            tokenIndexModel,
            userIdIndexModel,
            expiryIndexModel
        });
    }
}