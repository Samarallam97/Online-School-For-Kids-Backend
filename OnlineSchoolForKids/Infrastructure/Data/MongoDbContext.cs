using Domain.Entities;
using Domain.Enums;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);

        // Create indexes on startup
        //CreateIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshTokens");
    public IMongoCollection<Course> Courses => _database.GetCollection<Course>("courses");
    public IMongoCollection<Wishlist> Wishlists => _database.GetCollection<Wishlist>("wishlist");
    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }
    public async Task SeedDataAsync()
    {
        // Check if data already exists
        var categoriesCollection = _database.GetCollection<Category>("categories");
        var coursesCollection = _database.GetCollection<Course>("courses");

        if (await categoriesCollection.CountDocumentsAsync(FilterDefinition<Category>.Empty) > 0)
        {
            return; // Data already seeded
        }

        // Seed Categories
        var webDevCategory = new Category
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "Web Development",
            IconUrl = "https://cdn-icons-png.flaticon.com/512/1005/1005141.png",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var mobileDevCategory = new Category
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "Mobile Development",
            IconUrl = "https://cdn-icons-png.flaticon.com/512/2941/2941807.png",
            DisplayOrder = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await categoriesCollection.InsertManyAsync(new[] { webDevCategory, mobileDevCategory });

        // Seed Courses
        var aspNetCourse = new Course
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Title = "Complete ASP.NET Core Bootcamp 2024",
            Description = "Master ASP.NET Core development from scratch. Learn MVC, Web API, Entity Framework Core, Identity, and deploy production-ready applications.",
            InstructorId = ObjectId.GenerateNewId().ToString(),
            CategoryId = webDevCategory.Id,
            Level = CourseLevel.Beginner,
            Price = 99.99m,
            DiscountPrice = 79.99m,
            Rating = 4.5m,
            TotalStudents = 1500,
            DurationHours = 40,
            ThumbnailUrl = "https://images.unsplash.com/photo-1516116216624-53e697fedbea?w=800",
            Language = "English",
            IsPublished = true,
            IsFeatured = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var reactNativeCourse = new Course
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Title = "React Native - Build iOS & Android Apps",
            Description = "Build professional mobile applications for iOS and Android using React Native. Learn navigation, state management, and native modules.",
            InstructorId = ObjectId.GenerateNewId().ToString(),
            CategoryId = mobileDevCategory.Id,
            Level = CourseLevel.Intermediate,
            Price = 129.99m,
            DiscountPrice = 99.99m,
            Rating = 4.7m,
            TotalStudents = 2800,
            DurationHours = 52,
            ThumbnailUrl = "https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?w=800",
            Language = "English",
            IsPublished = true,
            IsFeatured = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await coursesCollection.InsertManyAsync(new[] { aspNetCourse, reactNativeCourse });
    }

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