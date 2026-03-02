using Application.Commands.Course;
using Application.Queries.Content;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Content_Module
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<QuizController> _logger;

        public QuizController(IMediator mediator, ILogger<QuizController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpPost("create")]
        [AllowAnonymous] 
        [ProducesResponseType(typeof(CreateQuizResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateQuizFromAi(
          [FromBody] CreateQuizDto dto,
          CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Received quiz creation request for course {CourseId} with {QuestionCount} questions",
                    dto.CourseId, dto.Questions.Count);

                var command = new CreateQuizCommand { Dto = dto };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning("Quiz creation failed: {Message}", result.Message);
                    return BadRequest(new
                    {
                        message = result.Message,
                        success = false
                    });
                }

                _logger.LogInformation(
                    "Quiz created successfully: {QuizId} - {QuizTitle}",
                    result.QuizId, result.QuizTitle);

                return Ok(new
                {
                    data = new
                    {
                        quizId = result.QuizId,
                        quizTitle = result.QuizTitle,
                        totalQuestions = result.TotalQuestions
                    },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quiz");
                return StatusCode(500, new
                {
                    message = "An error occurred while creating quiz",
                    success = false
                });
            }
        }

        /// <summary>
        /// Get all quizzes for a course
        /// GET /api/quiz/course/{courseId}
        /// </summary>
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseQuizzes(
            string courseId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated", success = false });
                }

                var query = new GetCourseQuizzesQuery
                {
                    CourseId = courseId,
                    UserId = userId
                };

                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course quizzes");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        /// <summary>
        /// Get quiz by ID with questions
        /// GET /api/quiz/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuiz(string id, CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated", success = false });
                }

                var query = new GetQuizByIdQuery
                {
                    QuizId = id,
                    UserId = userId
                };

                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                    return NotFound(new { message = "Quiz not found", success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz {Id}", id);
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        /// <summary>
        /// Start a new quiz attempt
        /// POST /api/quiz/{id}/start
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartQuizAttempt(
            string id,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated", success = false });
                }

                var command = new StartQuizAttemptCommand
                {
                    QuizId = id,
                    UserId = userId
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });

                return Ok(new
                {
                    data = new { attemptId = result.AttemptId },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting quiz attempt");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        /// <summary>
        /// Submit quiz answers
        /// POST /api/quiz/submit
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuiz(
            [FromBody] SubmitQuizDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated", success = false });
                }

                var command = new SubmitQuizCommand
                {
                    UserId = userId,
                    SubmitQuizDto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result == null)
                    return BadRequest(new { message = "Failed to submit quiz", success = false });

                return Ok(new
                {
                    data = result,
                    message = result.Passed ? "Quiz passed! 🎉" : "Quiz completed",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quiz");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }

        
    }
}

