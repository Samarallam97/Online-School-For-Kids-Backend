using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Shared;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Content;
using Infrastructure.Repositories.Users;
using Infrastructure.Services;
using Infrastructure.Services.Shared;
using Infrastructure.Services.Shared.Payment;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using StackExchange.Redis;
using System.Text;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        #region MongoDB
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDbSettings"));

        var conventions = new ConventionPack {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreIfNullConvention(true)
        };

        ConventionRegistry.Register("CustomConventions", conventions, _ => true); 

        services.AddSingleton<MongoDbContext>();
        #endregion


        #region Redis
        var redisConnection = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse(redisConnection);
                config.AbortOnConnectFail = false; // ← Don't crash if Redis is down
                config.ConnectRetry = 3;
                config.ConnectTimeout = 5000;
                return ConnectionMultiplexer.Connect(config);
            });
        }
        #endregion



        services.AddMemoryCache();
      
        #region Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        
        // Auth , User Module
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPayoutRepository, PayoutRepository>();

        // Content Module
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICartItemRepository, CartItemRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IWishListRepository, WishListRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<IAttemptRepository, AttemptRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ICourseProgressRepository, CourseProgressRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IUserPointsRepository, UserPointsRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<IPointTransactionRepository, PointTransactionRepository>();
        services.AddScoped<IReportedContentRepository, ReportedContentRepository>();

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.AddScoped<ICouponRepository, CouponRepository>();

        #endregion



        #region Services
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<ITempTokenService, TempTokenService>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IFileStorageService, FileStorageService>();

        services.AddScoped<ICouponValidationService, CouponValidationService>();


        services.AddPaymentProcessors();
        #endregion

        #region JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings?.Issuer,
                ValidAudience = jwtSettings?.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings?.Secret ?? string.Empty)),
                ClockSkew = TimeSpan.Zero
            };
        }).AddCookie()
        .AddGoogle(options =>
        {
            options.ClientId     = configuration["Google:ClientId"]!;
            options.ClientSecret = configuration["Google:ClientSecret"]!;
            options.SaveTokens   = true;
            options.ClaimActions.MapJsonKey("picture", "picture");
            options.ClaimActions.MapJsonKey("email_verified", "email_verified");
        }); ;

        services.AddAuthorization(); 
        #endregion

        return services;
    }
}
