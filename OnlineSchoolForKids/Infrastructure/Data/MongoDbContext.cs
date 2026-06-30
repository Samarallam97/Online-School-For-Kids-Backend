using Domain.Entities;
using Domain.Entities.Chat;
using Domain.Entities.Content;
using Domain.Entities.Content.Calendar;
using Domain.Entities.Content.Leaderboard;
using Domain.Entities.Content.Moderation;
using Domain.Entities.Content.Orders;
using Domain.Entities.Content.Progress;
using Domain.Entities.Content.Quiz;
using Domain.Entities.Feed;
using Domain.Entities.Live;
using Domain.Entities.Notification;
using Domain.Entities.Users;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);

        CreateIndexes();
    }

    // User Module
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshTokens");
    public IMongoCollection<Payout> Payouts => _database.GetCollection<Payout>("payouts");

    // Content Module
    public IMongoCollection<Course> Courses => _database.GetCollection<Course>("courses");
    public IMongoCollection<Category> Categories => GetCollection<Category>("categories");
    public IMongoCollection<Wishlist> Wishlists => _database.GetCollection<Wishlist>("wishlist");
    public IMongoCollection<CartItem> CartItems => _database.GetCollection<CartItem>("cartItems");
    public IMongoCollection<Order> Orders => _database.GetCollection<Order>("orders");
    public IMongoCollection<Enrollment> Enrollments => _database.GetCollection<Enrollment>("enrollments");
    public IMongoCollection<Quiz> Quizzes => _database.GetCollection<Quiz>("quizzes");
    public IMongoCollection<QuizAttempt> QuizAttempts => _database.GetCollection<QuizAttempt>("quizAttempts");
    public IMongoCollection<Section> Sections => _database.GetCollection<Section>("sections");
    public IMongoCollection<Lesson> Lessons => _database.GetCollection<Lesson>("lessons");
    public IMongoCollection<CourseProgress> CoursesProgress => _database.GetCollection<CourseProgress>("coursesprogress");
    public IMongoCollection<LessonProgress> LessonsProgress => _database.GetCollection<LessonProgress>("lessonsprogress");
    public IMongoCollection<Note> Notes => _database.GetCollection<Note>("notes");
    public IMongoCollection<Bookmark> Bookmarks => _database.GetCollection<Bookmark>("bookmarks");
    public IMongoCollection<Payment> Payments => _database.GetCollection<Payment>("payments");
    public IMongoCollection<Event> Events => _database.GetCollection<Event>("events");
    public IMongoCollection<Comment> Comments => _database.GetCollection<Comment>("comments");
    public IMongoCollection<Coupon> Coupons => _database.GetCollection<Coupon>("coupons");
    public IMongoCollection<ReportedContent> ReportedContents => _database.GetCollection<ReportedContent>("reportedContents");
    public IMongoCollection<UserPoints> UserPoints => _database.GetCollection<UserPoints>("userPoints");
    public IMongoCollection<Badge> Badges => _database.GetCollection<Badge>("badges");
    public IMongoCollection<PointTransaction> PointTransactions => _database.GetCollection<PointTransaction>("pointTransactions");


    public IMongoCollection<Post> Posts => _database.GetCollection<Post>("posts");
    public IMongoCollection<PostReaction> PostReactions => _database.GetCollection<PostReaction>("postReactions");
    public IMongoCollection<PostComment> PostComments => _database.GetCollection<PostComment>("postComments");

    public IMongoCollection<VideoProcessingJob> VideoProcessingJobs =>
     _database.GetCollection<VideoProcessingJob>("videoProcessingJobs");

    public IMongoCollection<Conversation> Conversations => _database.GetCollection<Conversation>("conversations");
    public IMongoCollection<Group> Groups => _database.GetCollection<Group>("groups");
    public IMongoCollection<ChatMessage> ChatMessages => _database.GetCollection<ChatMessage>("chatMessages");
    public IMongoCollection<LiveSession> LiveSessions => _database.GetCollection<LiveSession>("live_sessions");
    public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>("notifications");


    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }

    private void CreateIndexes()
    {
        var appts = _database.GetCollection<Appointment>("Appointments");

        appts.Indexes.CreateMany(new[]
        {
            // Slot conflict check
            new CreateIndexModel<Appointment>(
                Builders<Appointment>.IndexKeys
                    .Ascending(a => a.SpecialistId)
                    .Ascending(a => a.AppointmentDate)
                    .Ascending(a => a.StartTime)),

            // Expiry job scan
            new CreateIndexModel<Appointment>(
                Builders<Appointment>.IndexKeys
                    .Ascending(a => a.Status)
                    .Ascending(a => a.HoldExpiresAtUtc))
        });
    }
}