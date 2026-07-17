using Domain.Entities.Content.Quizes;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories.Content;

public class QuizAttemptRepository : GenericRepository<QuizAttempt>, IQuizAttemptRepository
{
    private readonly IMongoCollection<QuizAttempt> _collection;

    public QuizAttemptRepository(MongoDbContext context) : base(context.QuizAttempts)
    {
        _collection = context.QuizAttempts;
    }

    public async Task<List<QuizAttempt>> GetByUserAndLessonAsync(
        string userId, string lessonId, CancellationToken ct = default)
    {
        return await _collection
            .Find(a => a.UserId == userId && a.LessonId == lessonId)
            .SortByDescending(a => a.CompletedAt)
            .ToListAsync(ct);
    }

    public async Task<List<QuizAttempt>> GetByUserAndCourseAsync(
        string userId, string courseId, CancellationToken ct = default)
    {
        return await _collection
            .Find(a => a.UserId == userId && a.CourseId == courseId)
            .SortByDescending(a => a.CompletedAt)
            .ToListAsync(ct);
    }
}
