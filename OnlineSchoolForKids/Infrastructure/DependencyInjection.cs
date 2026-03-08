using Domain.Entities.Content.Moderation;
using Domain.Entities.Content.Progress;
using Domain.Entities.Content.Quiz;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Content;
using Infrastructure.Repositories.Users;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using StackExchange.Redis;
using System.Text;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDbSettings"));

        var conventions = new ConventionPack {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreIfNullConvention(true)
        };

        ConventionRegistry.Register("CustomConventions", conventions, _ => true);

        services.AddSingleton<MongoDbContext>();

        var redisConnection = configuration.GetConnectionString("Redis");
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<MongoDbContext>();

        ///////////////////////////////////////////
        //services.AddSingleton<MongoDbContext>();
        //services.AddScoped(sp =>
        //    sp.GetRequiredService<MongoDbContext>().GetCollection<Domain.Entities.Content.Course>("Courses"));
        //services.AddScoped(sp =>
        //    sp.GetRequiredService<MongoDbContext>().GetCollection<Domain.Entities.Payment>("Payments"));
        //services.AddScoped(sp =>
        //    sp.GetRequiredService<MongoDbContext>().GetCollection<Domain.Entities.Content.Order.Order>("Orders"));
        //services.AddScoped(sp =>
        //    sp.GetRequiredService<MongoDbContext>().GetCollection<Domain.Entities.Content.Enrollment>("Enrollments"));
        //services.AddScoped(sp =>
        //    sp.GetRequiredService<MongoDbContext>().GetCollection<Domain.Entities.Content.Quiz.Quiz>("Quizzes"));
        //services.AddScoped<IMongoCollection<QuizAttempt>>(sp =>
        //{
        //    var database = sp.GetRequiredService<IMongoDatabase>();
        //    return database.GetCollection<QuizAttempt>("QuizAttempts");
        //});
        //services.AddScoped(sp =>
        //    sp.GetRequiredService<MongoDbContext>().GetCollection<Domain.Entities.Users.User>("Users"));
        //var mongoSection = configuration.GetSection("MongoDbSettings");
        //var connectionString = mongoSection["ConnectionString"];
        //var databaseName = mongoSection["DatabaseName"];
        //if (string.IsNullOrEmpty(connectionString))
        //{
        //    throw new ArgumentNullException(nameof(connectionString),
        //        "MongoDB ConnectionString is missing in appsettings.json under 'MongoDbSettings'");
        //}
        //services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        //services.AddScoped<IMongoDatabase>(sp =>
        //{
        //    var client = sp.GetRequiredService<IMongoClient>();
        //    return client.GetDatabase(databaseName);
        //});
        //services.AddScoped<IMongoCollection<CourseProgress>>(sp =>
        //{
        //    var database = sp.GetRequiredService<IMongoDatabase>();
        //    return database.GetCollection<CourseProgress>("CourseProgress");
        //});
        //services.AddScoped<IMongoCollection<LessonProgress>>(sp =>
        //{
        //    var database = sp.GetRequiredService<IMongoDatabase>();
        //    return database.GetCollection<LessonProgress>("LessonProgress");
        //});
        //services.AddScoped<IMongoCollection<Comment>>(sp =>
        //{
        //    var db = sp.GetRequiredService<IMongoDatabase>();
        //    return db.GetCollection<Comment>("Comments");
        //});

        //services.AddScoped<IMongoCollection<ReportedContent>>(sp =>
        //{
        //    var db = sp.GetRequiredService<IMongoDatabase>();
        //    return db.GetCollection<ReportedContent>("ReportedContents");
        //});
        ///////////////////////////////////////////////

        // Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPayoutRepository, PayoutRepository>();
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
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IReportedContentRepository, ReportedContentRepository>();

        // Authentication Services
        services.Configure<JwtSettings>(
            configuration.GetSection("JwtSettings"));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();
        services.Configure<EmailSettings>(
           configuration.GetSection("EmailSettings"));
        services.AddScoped<IPaymentService, PaymentService>();

        // HTTP Client for Google Auth
        services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();

        services.AddScoped<IFileStorageService, FileStorageService>();


        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
        });

        services.AddAuthorization();

        return services;
    }
}
