using Application.Services;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Shared;
using Infrastructure.BackgroundJobs;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
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
        services.AddSingleton(new UpstashRedisClient(
            configuration["Upstash:Url"]!,
            configuration["Upstash:Token"]!
        ));
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
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ICourseProgressRepository, CourseProgressRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUserPointsRepository, UserPointsRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<IPointTransactionRepository, PointTransactionRepository>();
        services.AddScoped<IReportedContentRepository, ReportedContentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.Configure<GoogleMeetOptions>(configuration.GetSection("GoogleMeet"));
        services.AddScoped<IGoogleMeetService, GoogleMeetService>();
        services.AddHostedService<BackgroundJobs.AppointmentExpiryJob>();
        services.AddHostedService<BackgroundJobs.VideoJobCleanupJob>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IPostReactionRepository, PostReactionRepository>();
        services.AddScoped<IPostCommentRepository, PostCommentRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<IVideoProcessingJobRepository, VideoProcessingJobRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();

        // Chat
        services.AddScoped<ChatRepository>();


        services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
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
        services.AddScoped<IFeedService, FeedService>();
        services.AddPaymentProcessors();

        services.AddHostedService<RankRecalculationBackgroundService>();
        // ── AI services ──────────────────────────────────────────────────
        services.AddHttpClient<ITextCorrectionClient, TextCorrectionClient>();
        #endregion

        #region JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtSettings?.Issuer,
                ValidAudience            = jwtSettings?.Audience,
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings?.Secret ?? string.Empty)),
                ClockSkew = TimeSpan.Zero
            };

            // ── Required for SignalR ──────────────────────────────────────────
            // The SignalR JS client sends the JWT as ?access_token=... in the
            // WebSocket handshake URL because browsers can't set Authorization
            // headers on WebSocket connections.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) &&
                        context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        })
        .AddCookie()
        .AddGoogle(options =>
        {
            options.ClientId     = configuration["Google:ClientId"]!;
            options.ClientSecret = configuration["Google:ClientSecret"]!;
            options.SaveTokens   = true;
            options.ClaimActions.MapJsonKey("picture", "picture");
            options.ClaimActions.MapJsonKey("email_verified", "email_verified");
        });

        services.AddAuthorization();
        #endregion

        return services;
    }
}