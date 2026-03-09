using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Seeding;

public static class SuperAdminSeeder
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
            var config = services.GetRequiredService<IConfiguration>();

            var email = config["SuperAdmin:Email"]    ?? "superadmin@maman.com";
            var password = config["SuperAdmin:Password"] ?? "SuperAdmin@123!";
            var fullName = config["SuperAdmin:FullName"] ?? "Super Admin";

            var existing = await userRepo.GetByEmailAsync(email.ToLower(), CancellationToken.None);
            if (existing is not null)
            {
                logger.LogInformation("SuperAdmin already exists — skipping seed.");
                return;
            }

            var superAdmin = new User
            {
                FullName      = fullName,
                Email         = email.ToLower(),
                EmailVerified = true,
                PasswordHash  = hasher.HashPassword(password),
                Role          = UserRole.Admin,
                IsSuperAdmin  = true,
                Status        = UserStatus.Active,
                AuthProvider  = AuthProvider.Local,
                IsFirstLogin  = false,
                DateOfBirth   = new DateTime(1990, 1, 1),
                Country       = "Egypt",
                CreatedAt     = DateTime.UtcNow,
                ActivityLog   = new List<ActivityLogEntry>(),
            };

            await userRepo.CreateAsync(superAdmin, CancellationToken.None);
            logger.LogInformation("SuperAdmin seeded → {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed SuperAdmin.");
        }
    }
}

