using Domain.Entities.Content.Quizes;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Application.Commands;


/// <summary>
/// Replaces the question list for one difficulty on an already-saved lesson's
/// quiz. Used from the lesson editor (post-save maintenance) — distinct from
/// UpdateChunkQuizQuestionsCommand, which edits a draft quiz on an in-progress
/// VideoProcessingJob chunk before the lesson exists at all.
/// </summary>
public class UpdateLessonQuizCommand : IRequest<bool>
{
    public string InstructorId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<UpdateLessonQuizQuestionDto> Questions { get; set; } = new();
}

public class UpdateLessonQuizQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswer { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class UpdateLessonQuizHandler : IRequestHandler<UpdateLessonQuizCommand, bool>
{
    private readonly ICourseRepository _courseRepo;
    private readonly ILogger<UpdateLessonQuizHandler> _logger;

    public UpdateLessonQuizHandler(
        ICourseRepository courseRepo,
        ILogger<UpdateLessonQuizHandler> logger)
    {
        _courseRepo = courseRepo;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateLessonQuizCommand request, CancellationToken ct)
    {
        try
        {
            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null || course.InstructorId != request.InstructorId) return false;

            var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
            var lesson = section?.Lessons?.FirstOrDefault(l => l.Id == request.LessonId);
            if (lesson == null) return false;

            var questions = request.Questions.Select(q => new QuizQuestion
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Text = q.Question,
                Options = q.Options.Select((o, index) => new QuizOption
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Text = o,
                    IsCorrect = index == q.CorrectAnswer,
                    Order = index
                }).ToList(),
                CorrectAnswer = q.CorrectAnswer,
                Explanation = q.Explanation
            }).ToList();

            var existingQuiz = lesson.Quizzes.FirstOrDefault(q => q.Difficulty == request.Difficulty);
            if (existingQuiz != null)
            {
                existingQuiz.Questions = questions;
            }
            else if (questions.Count > 0)
            {
                lesson.Quizzes.Add(new LessonQuiz
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Difficulty = request.Difficulty,
                    Questions = questions
                });
            }

            // If the creator removed every question for this difficulty, drop
            // the now-empty quiz set entirely rather than keeping a hollow entry.
            if (questions.Count == 0)
                lesson.Quizzes.RemoveAll(q => q.Difficulty == request.Difficulty);

            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepo.UpdateAsync(course.Id, course, ct);

            _logger.LogInformation(
                "Updated {Difficulty} quiz for lesson {LessonId} ({Count} questions)",
                request.Difficulty, lesson.Id, questions.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quiz for lesson {LessonId}", request.LessonId);
            return false;
        }
    }
}
