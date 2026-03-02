using Application.Commands.Course;
using Application.Queries.Content;
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
        /// <summary>
        /// Get student dashboard with all enrolled courses
        /// GET /api/progress/dashboard
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

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
           string courseId,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetCourseCurriculumQuery
                {
                    UserId = userId,
                    CourseId = courseId
                };

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


        [HttpGet("{courseId}/quiz-scores")]
        public async Task<IActionResult> GetQuizScores(
            string courseId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetUserQuizResultsQuery
                {
                    UserId = userId,
                    CourseId = courseId
                };

                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                    return NotFound(new { message = "No quiz results found", success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz scores");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }


        [HttpPost("notes")]
        public async Task<IActionResult> CreateNote(
            [FromBody] CreateNoteDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new CreateNoteCommand
                {
                    UserId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result == null)
                    return BadRequest(new { message = "Failed to create note", success = false });

                return Ok(new
                {
                    data = result,
                    message = "Note created",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        [HttpPost("bookmark/toggle")]
        public async Task<IActionResult> ToggleBookmark(
           [FromBody] ToggleBookmarkDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new ToggleBookmarkCommand
                {
                    UserId = userId,
                    Dto = dto
                };

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

        /// <summary>
        /// 1️⃣ Get Continue Learning data (where user left off)
        /// GET /api/learning/continue/{courseId}
        /// </summary>
        [HttpGet("continue/{courseId}")]
        public async Task<IActionResult> GetContinueLearning(
            string courseId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetContinueLearningQuery
                {
                    UserId = userId,
                    CourseId = courseId
                };

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
                  [FromBody] UpdateLessonProgressDto dto,
                  CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new UpdateLessonProgressCommand
                {
                    UserId = userId,
                    Dto = dto
                };

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
                    [FromBody] MarkLessonCompleteDto dto,
                    CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new MarkLessonCompleteCommand
                {
                    UserId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });

                return Ok(new
                {
                    data = new
                    {
                        courseCompleted = result.CourseCompleted,
                        courseProgress = result.CourseProgress
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
            string courseId,
            string lessonId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetLessonProgressQuery
                {
                    UserId = userId,
                    CourseId = courseId,
                    LessonId = lessonId
                };

                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                {
                    // No progress yet - start from beginning
                    return Ok(new
                    {
                        data = new
                        {
                            lessonId = lessonId,
                            isCompleted = false,
                            videoPosition = 0,
                            timeSpent = 0
                        },
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
    }
}


 

