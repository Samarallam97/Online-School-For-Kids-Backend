using Domain.Entities.Content.Quizes;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public class GetLessonQuizAttemptsQuery : IRequest<List<QuizAttemptDto>>
{
    public string UserId { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
}

public class GetCourseQuizAttemptsQuery : IRequest<List<QuizAttemptDto>>
{
    public string UserId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
}

public class QuizAttemptDto
{
    public string Id { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<QuizAttemptAnswerDto> Answers { get; set; } = new();
}

public class QuizAttemptAnswerDto
{
    public int QuestionIndex { get; set; }
    public int SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }
}

// ── Handlers ──────────────────────────────────────────────────────────────────

public class GetLessonQuizAttemptsHandler
    : IRequestHandler<GetLessonQuizAttemptsQuery, List<QuizAttemptDto>>
{
    private readonly IQuizAttemptRepository _repo;
    private readonly ILogger<GetLessonQuizAttemptsHandler> _logger;

    public GetLessonQuizAttemptsHandler(
        IQuizAttemptRepository repo,
        ILogger<GetLessonQuizAttemptsHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<List<QuizAttemptDto>> Handle(
        GetLessonQuizAttemptsQuery request, CancellationToken ct)
    {
        try
        {
            var attempts = await _repo.GetByUserAndLessonAsync(request.UserId, request.LessonId, ct);
            return attempts.Select(QuizAttemptMapper.MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quiz attempts for lesson {LessonId}", request.LessonId);
            return new List<QuizAttemptDto>();
        }
    }
}

public class GetCourseQuizAttemptsHandler
    : IRequestHandler<GetCourseQuizAttemptsQuery, List<QuizAttemptDto>>
{
    private readonly IQuizAttemptRepository _repo;
    private readonly ILogger<GetCourseQuizAttemptsHandler> _logger;

    public GetCourseQuizAttemptsHandler(
        IQuizAttemptRepository repo,
        ILogger<GetCourseQuizAttemptsHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<List<QuizAttemptDto>> Handle(
        GetCourseQuizAttemptsQuery request, CancellationToken ct)
    {
        try
        {
            var attempts = await _repo.GetByUserAndCourseAsync(request.UserId, request.CourseId, ct);
            return attempts.Select(QuizAttemptMapper.MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quiz attempts for course {CourseId}", request.CourseId);
            return new List<QuizAttemptDto>();
        }
    }
}

// Shared mapper
file static class AttemptMapper
{
    internal static QuizAttemptDto MapToDto(QuizAttempt a) =>
        new()
        {
            Id = a.Id,
            LessonId = a.LessonId,
            Difficulty = a.Difficulty,
            Score = a.Score.Value,
            CorrectAnswers = a.CorrectAnswers,
            TotalQuestions = a.TotalQuestions,
            Passed = a.Passed,
            CompletedAt = a.CompletedAt,
            Answers = a.Answers.Select(ans => new QuizAttemptAnswerDto
            {
                QuestionIndex = ans.QuestionIndex,
                SelectedAnswer = ans.SelectedAnswer,
                IsCorrect = ans.IsCorrect
            }).ToList()
        };
}

// Make MapToDto visible without the file-scoped class needing to repeat itself
public static class QuizAttemptMapper
{
    public static QuizAttemptDto MapToDto(QuizAttempt a) =>
        AttemptMapper.MapToDto(a);
}
