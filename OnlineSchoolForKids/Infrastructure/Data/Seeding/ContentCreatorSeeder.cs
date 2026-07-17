using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Services.Shared;

namespace Infrastructure.Data.Seeding;

public static class ContentCreatorSeeder
{
    public static async Task SeedAsync(IHost app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<User>>();

        try
        {
            var userRepo = services.GetRequiredService<IUserRepository>();
            var hasher = services.GetRequiredService<IPasswordHasher>();

            var creators = GetSeedContentCreators();

            foreach (var (email, data) in creators)
            {
                var existing = await userRepo.GetByEmailAsync(email, CancellationToken.None);
                if (existing is not null)
                {
                    logger.LogInformation("Content creator {Email} already exists — skipping.", email);
                    continue;
                }

                var user = new User
                {
                    FullName          = data.FullName,
                    Email             = email,
                    EmailVerified     = true,
                    PasswordHash      = hasher.HashPassword("Creator@123!"),
                    Role              = UserRole.ContentCreator,
                    Status            = UserStatus.Active,
                    AuthProvider      = AuthProvider.Local,
                    IsFirstLogin      = false,
                    DateOfBirth       = new DateTime(1988, 1, 1),
                    Country           = data.Country,
                    Bio               = data.Bio,
                    ProfilePictureUrl = data.ProfilePictureUrl,
                    ExpertiseTags     = data.ExpertiseTags,
                    AverageRating     = data.AverageRating,
                    ReviewsCount      = data.ReviewsCount,
                    StudentsCount     = data.StudentsCount,
                    CoursesCount      = data.CoursesCount,
                    IsVerifiedCreator = data.IsVerifiedCreator,
                    PortfolioUrl      = data.PortfolioUrl,
                    CreatedCourseIds  = [],
                    CreatedAt         = DateTime.UtcNow,
                    ActivityLog       = [],
                };

                await userRepo.CreateAsync(user, CancellationToken.None);
                logger.LogInformation("Content creator seeded → {Email}", email);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed content creators.");
        }
    }

    // ── Seed data ─────────────────────────────────────────────────────────────

    private record ContentCreatorSeedData(
        string FullName,
        string Country,
        string Bio,
        List<string> ExpertiseTags,
        double AverageRating,
        int ReviewsCount,
        int StudentsCount,
        int CoursesCount,
        bool IsVerifiedCreator,
        string? ProfilePictureUrl,
        string? PortfolioUrl
    );

    private static Dictionary<string, ContentCreatorSeedData> GetSeedContentCreators() => new()
    {
        ["amira.fouad@maman.com"] = new(
            FullName: "Amira Fouad",
            Country: "Egypt",
            Bio: "Math educator with 10+ years creating engaging courses on algebra, calculus, and standardized test prep. Focused on breaking down complex concepts into digestible lessons.",
            ExpertiseTags: ["Math", "Test Prep"],
            AverageRating: 4.9,
            ReviewsCount: 512,
            StudentsCount: 3400,
            CoursesCount: 14,
            IsVerifiedCreator: true,
            ProfilePictureUrl: null,
            PortfolioUrl: "https://portfolio.example.com/amira-fouad"
        ),

        ["daniel.reyes@maman.com"] = new(
            FullName: "Daniel Reyes",
            Country: "United States",
            Bio: "Full-stack developer and coding instructor. My courses cover web development fundamentals through advanced React and backend architecture patterns.",
            ExpertiseTags: ["Coding"],
            AverageRating: 4.8,
            ReviewsCount: 890,
            StudentsCount: 5200,
            CoursesCount: 21,
            IsVerifiedCreator: true,
            ProfilePictureUrl: null,
            PortfolioUrl: "https://portfolio.example.com/daniel-reyes"
        ),

        ["noor.abdallah@maman.com"] = new(
            FullName: "Noor Abdallah",
            Country: "Egypt",
            Bio: "Science communicator passionate about making physics and chemistry accessible to high schoolers. Courses blend animation, experiments, and real-world examples.",
            ExpertiseTags: ["Science"],
            AverageRating: 4.7,
            ReviewsCount: 344,
            StudentsCount: 2100,
            CoursesCount: 9,
            IsVerifiedCreator: false,
            ProfilePictureUrl: null,
            PortfolioUrl: null
        ),

        ["olivia.bennett@maman.com"] = new(
            FullName: "Olivia Bennett",
            Country: "United Kingdom",
            Bio: "Award-winning writing coach helping students master essay structure, creative writing, and literary analysis for exams and beyond.",
            ExpertiseTags: ["Language Arts"],
            AverageRating: 4.9,
            ReviewsCount: 276,
            StudentsCount: 1800,
            CoursesCount: 7,
            IsVerifiedCreator: true,
            ProfilePictureUrl: null,
            PortfolioUrl: "https://portfolio.example.com/olivia-bennett"
        ),

        ["youssef.karam@maman.com"] = new(
            FullName: "Youssef Karam",
            Country: "Egypt",
            Bio: "Illustrator and design educator teaching digital art, UI/UX fundamentals, and creative portfolio building for aspiring designers.",
            ExpertiseTags: ["Arts & Design"],
            AverageRating: 4.6,
            ReviewsCount: 132,
            StudentsCount: 950,
            CoursesCount: 5,
            IsVerifiedCreator: false,
            ProfilePictureUrl: null,
            PortfolioUrl: "https://portfolio.example.com/youssef-karam"
        ),
    };
}