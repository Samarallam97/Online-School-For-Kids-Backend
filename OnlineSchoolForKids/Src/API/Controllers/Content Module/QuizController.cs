using Application.Commands.Course;
using Application.Queries.Content;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Content_Module;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ContentCreator")]
public class QuizController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<QuizController> _logger;

    public QuizController(IMediator mediator, ILogger<QuizController> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>
    /// Generate quiz questions for a single difficulty level.
    /// Called once per difficulty (easy/medium/hard) from the lesson editor.
    /// POST /api/quiz/generate
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateQuizRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new GenerateQuizCommand
            {
                LessonName   = request.LessonName,
                Transcript   = request.Transcript,
                Difficulty   = request.Difficulty,
                NumQuestions = request.NumQuestions
            };

            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new { data = result.Questions, message = result.Message, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quiz");
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    /// <summary>
    /// Save a finalized lesson (transcript + video url + all 3 quiz sets) to the course.
    /// POST /api/quiz/save-lesson
    /// </summary>
    [HttpPost("save-lesson")]
    public async Task<IActionResult> SaveLesson(
        [FromBody] SaveLessonRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new SaveLessonWithQuizCommand
            {
                InstructorId = userId,
                CourseId     = request.CourseId,
                SectionId    = request.SectionId,
                LessonId     = request.LessonId ?? string.Empty,
                Title        = request.Title,
                Transcript   = request.Transcript,
                VideoUrl     = request.VideoUrl,
                Duration     = request.Duration,
                Order        = request.Order,
                IsFree       = request.IsFree,
                Quizzes      = request.Quizzes.Select(q => new DifficultyQuizDto
                {
                    Difficulty = q.Difficulty,
                    Questions  = q.Questions.Select(qq => new QuizQuestionSaveDto
                    {
                        Question      = qq.Question,
                        Options       = qq.Options,
                        CorrectAnswer = qq.CorrectAnswer,
                        Explanation   = qq.Explanation
                    }).ToList()
                }).ToList()
            };

            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new
            {
                data = new { lessonId = result.LessonId },
                message = result.Message,
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving lesson with quiz");
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class GenerateQuizRequest
{
    public string LessonName { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "easy";
    public int NumQuestions { get; set; } = 5;
}

public class SaveLessonRequest
{
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string? LessonId { get; set; }   // null = create new
    public string Title { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int Order { get; set; }
    public bool IsFree { get; set; }
    public List<SaveLessonQuizDto> Quizzes { get; set; } = new();
}

public class SaveLessonQuizDto
{
    public string Difficulty { get; set; } = string.Empty;
    public List<SaveLessonQuestionDto> Questions { get; set; } = new();
}

public class SaveLessonQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswer { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

