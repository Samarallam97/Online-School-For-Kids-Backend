using Application.Commands;
using Application.Commands.Course;
using Application.Queries;
using Application.Queries.Content;
using Application.Queries.Content.Calendar;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static MarkLessonCompleteHandler;
using static ToggleBookmarkHandler;
using static UpdateLessonProgressHandler;

namespace API.Controllers.Content_Module
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProgressController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProgressController> _logger;

        public ProgressController(IMediator mediator, ILogger<ProgressController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        private string GetUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var query = new GetStudentDashboardQuery { UserId = userId };
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpGet("Curriculum/{courseId}")]
        public async Task<IActionResult> GetCourseCurriculum(
            string courseId, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var query = new GetCourseCurriculumQuery { UserId = userId, CourseId = courseId };
                var result = await _mediator.Send(query, cancellationToken);
                if (result == null)
                    return NotFound(new { message = "Course not found", success = false });
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course curriculum");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPost("notes")]
        public async Task<IActionResult> CreateNote(
            [FromBody] CreateNoteDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new CreateNoteCommand { UserId = userId, Dto = dto };
                var result = await _mediator.Send(command, cancellationToken);
                if (result == null)
                    return BadRequest(new { message = "Failed to create note", success = false });
                return Ok(new { data = result, message = "Note created", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPost("bookmark/toggle")]
        public async Task<IActionResult> ToggleBookmark(
            [FromBody] ToggleBookmarkDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new ToggleBookmarkCommand { UserId = userId, Dto = dto };
                var result = await _mediator.Send(command, cancellationToken);
                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });
                return Ok(new
                {
                    data = new { isBookmarked = result.IsBookmarked },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling bookmark");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpGet("continue/{courseId}")]
        public async Task<IActionResult> GetContinueLearning(
            string courseId, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var query = new GetContinueLearningQuery { UserId = userId, CourseId = courseId };
                var result = await _mediator.Send(query, cancellationToken);
                if (result == null)
                    return NotFound(new { message = "No enrollment found", success = false });
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting continue learning data");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProgress(
            [FromBody] UpdateLessonProgressDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new UpdateLessonProgressCommand { UserId = userId, Dto = dto };
                var result = await _mediator.Send(command, cancellationToken);
                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });
                return Ok(new
                {
                    data = new { courseProgress = result.CourseProgress },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPost("complete")]
        public async Task<IActionResult> MarkComplete(
            [FromBody] MarkLessonCompleteDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var command = new MarkLessonCompleteCommand { UserId = userId, Dto = dto };
                var result = await _mediator.Send(command, cancellationToken);
                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });
                return Ok(new
                {
                    data = new
                    {
                        courseCompleted = result.CourseCompleted,
                        courseProgress = result.CourseProgress,
                        pointsEarned = result.PointsEarned,
                        totalPoints = result.TotalPoints
                    },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking lesson complete");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpGet("{courseId}/{lessonId}")]
        public async Task<IActionResult> GetLessonProgress(
            string courseId, string lessonId, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var query = new GetLessonProgressQuery
                {
                    UserId = userId,
                    CourseId = courseId,
                    LessonId = lessonId
                };
                var result = await _mediator.Send(query, cancellationToken);
                if (result == null)
                {
                    return Ok(new
                    {
                        data = new { lessonId, isCompleted = false, videoPosition = 0, timeSpent = 0 },
                        success = true
                    });
                }
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson progress");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var query = new GetCalendarStatsQuery { UserId = userId };
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting calendar stats");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        // ── Quiz Attempts ─────────────────────────────────────────────────────

        /// <summary>POST /api/progress/quiz-attempt — save a completed quiz attempt</summary>
        [HttpPost("quiz-attempt")]
        public async Task<IActionResult> SaveQuizAttempt(
            [FromBody] SaveQuizAttemptDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var command = new SaveQuizAttemptCommand
                {
                    UserId = userId,
                    CourseId = dto.CourseId,
                    LessonId = dto.LessonId,
                    Difficulty = dto.Difficulty,
                    Answers = dto.Answers.Select(a => new SaveQuizAttemptAnswerDto
                    {
                        QuestionIndex = a.QuestionIndex,
                        SelectedAnswer = a.SelectedAnswer,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                };

                var result = await _mediator.Send(command, cancellationToken);
                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });

                return Ok(new
                {
                    data = new
                    {
                        attemptId = result.AttemptId,
                        score = result.Score,
                        correctAnswers = result.CorrectAnswers,
                        totalQuestions = result.TotalQuestions,
                        passed = result.Passed,
                        pointsEarned = result.PointsEarned,
                        totalPoints = result.TotalPoints
                    },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving quiz attempt");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        /// <summary>GET /api/progress/quiz-attempts/lesson/{lessonId}</summary>
        [HttpGet("quiz-attempts/lesson/{lessonId}")]
        public async Task<IActionResult> GetLessonQuizAttempts(
            string lessonId, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var query = new GetLessonQuizAttemptsQuery
                {
                    UserId = userId,
                    LessonId = lessonId
                };
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson quiz attempts");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        /// <summary>GET /api/progress/quiz-attempts/course/{courseId}</summary>
        [HttpGet("quiz-attempts/course/{courseId}")]
        public async Task<IActionResult> GetCourseQuizAttempts(
            string courseId, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var query = new GetCourseQuizAttemptsQuery
                {
                    UserId = userId,
                    CourseId = courseId
                };
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course quiz attempts");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }


        /// <summary>GET /api/progress/quiz/{courseId}/{lessonId} — full quiz content for taking a quiz</summary>
        [HttpGet("quiz/{courseId}/{lessonId}")]
        public async Task<IActionResult> GetLessonQuiz(
            string courseId, string lessonId, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var query = new GetLessonQuizQuery
                {
                    UserId = userId,
                    CourseId = courseId,
                    LessonId = lessonId
                };
                var result = await _mediator.Send(query, cancellationToken);
                if (result == null)
                    return NotFound(new { message = "Quiz not found or not enrolled", success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson quiz");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    public class SaveQuizAttemptDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public List<AttemptAnswerDto> Answers { get; set; } = new();
    }

    public class AttemptAnswerDto
    {
        public int QuestionIndex { get; set; }
        public int SelectedAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }
}