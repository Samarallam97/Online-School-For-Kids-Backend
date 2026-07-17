
using Domain.Entities.Content.Quizes;

namespace Domain.Interfaces.Repositories.Content;

public interface IQuizAttemptRepository : IGenericRepository<QuizAttempt>
{
    Task<List<QuizAttempt>> GetByUserAndLessonAsync(string userId, string lessonId, CancellationToken ct = default);
    Task<List<QuizAttempt>> GetByUserAndCourseAsync(string userId, string courseId, CancellationToken ct = default);
}
