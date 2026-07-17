using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Content.Quizes;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using Application.Commands.Leaderboard;
using Application.Queries;

namespace Application.Commands.Course;

public class SaveQuizAttemptCommand : IRequest<SaveQuizAttemptResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<SaveQuizAttemptAnswerDto> Answers { get; set; } = new();
}

public class SaveQuizAttemptAnswerDto
{
    public int QuestionIndex { get; set; }
    public int SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }
}

public class SaveQuizAttemptResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AttemptId { get; set; }
    public decimal Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public bool Passed { get; set; }
    public int PointsEarned { get; set; }
    public int TotalPoints { get; set; }
}

public class SaveQuizAttemptHandler : IRequestHandler<SaveQuizAttemptCommand, SaveQuizAttemptResponse>
{
    private readonly IQuizAttemptRepository _attemptRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly IMediator _mediator;
    private readonly ILogger<SaveQuizAttemptHandler> _logger;

    private const decimal PassThreshold = 60;

    public SaveQuizAttemptHandler(
        IQuizAttemptRepository attemptRepo,
        ICourseRepository courseRepo,
        IMediator mediator,
        ILogger<SaveQuizAttemptHandler> logger)
    {
        _attemptRepo = attemptRepo;
        _courseRepo = courseRepo;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<SaveQuizAttemptResponse> Handle(
        SaveQuizAttemptCommand request, CancellationToken ct)
    {
        try
        {
            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null)
                return Fail("Course not found");

            var lesson = course.Sections?
                .SelectMany(s => s.Lessons ?? new List<Domain.Entities.Content.Progress.Lesson>())
                .FirstOrDefault(l => l.Id == request.LessonId);
            if (lesson == null)
                return Fail("Lesson not found");

            var correct = request.Answers.Count(a => a.IsCorrect);
            var total = request.Answers.Count;
            var score = total > 0 ? Math.Round((decimal)correct / total * 100, 1) : 0;

            var attempt = new QuizAttempt
            {
                UserId = request.UserId,
                CourseId = request.CourseId,
                LessonId = request.LessonId,
                Difficulty = request.Difficulty,
                Score = score,
                CorrectAnswers = correct,
                TotalQuestions = total,
                Passed = score >= PassThreshold,
                CompletedAt = DateTime.UtcNow,
                Answers = request.Answers.Select(a => new QuizAttemptAnswer
                {
                    QuestionIndex = a.QuestionIndex,
                    SelectedAnswer = a.SelectedAnswer,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            await _attemptRepo.CreateAsync(attempt, ct);

            // ── Award points on a pass ───────────────────────────────
            int pointsEarned = 0;
            if (attempt.Passed)
            {
                pointsEarned = request.Difficulty?.ToLowerInvariant() switch
                {
                    "hard" => 30,
                    "medium" => 20,
                    _ => 10
                };

                await _mediator.Send(new AwardPointsCommand
                {
                    Dto = new AwardPointsDto
                    {
                        UserId = request.UserId,
                        Points = pointsEarned,
                        Reason = "QuizPassed",
                        Description = $"Passed {request.Difficulty} quiz on lesson {request.LessonId}",
                        RelatedEntityId = request.LessonId
                    }
                }, ct);
            }

            var stats = await _mediator.Send(new GetUserStatsQuery { UserId = request.UserId }, ct);

            _logger.LogInformation(
                "Quiz attempt saved: user {UserId} lesson {LessonId} difficulty {Difficulty} score {Score}",
                request.UserId, request.LessonId, request.Difficulty, score);

            return new SaveQuizAttemptResponse
            {
                Success = true,
                Message = "Attempt saved",
                AttemptId = attempt.Id,
                Score = score,
                CorrectAnswers = correct,
                TotalQuestions = total,
                Passed = attempt.Passed,
                PointsEarned = pointsEarned,
                TotalPoints = stats?.TotalPoints ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving quiz attempt");
            return Fail("An error occurred while saving the attempt");
        }
    }

    private static SaveQuizAttemptResponse Fail(string msg) =>
        new() { Success = false, Message = msg };
}