using Application.Commands.Moderation;
using Application.Queries.Content.Moderation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Content_Module
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ModerationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ModerationController> _logger;

        public ModerationController(IMediator mediator, ILogger<ModerationController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpGet("pending-courses")]
        public async Task<IActionResult> GetPendingCourses(CancellationToken cancellationToken)
        {
            try
            {
                var query = new GetPendingCoursesQuery();
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending courses");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        {
            try
            {
                var query = new GetModerationStatsQuery();
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting moderation stats");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpGet("reported-content")]
        public async Task<IActionResult> GetReportedContent(CancellationToken cancellationToken)
        {
            try
            {
                var query = new GetReportedContentQuery();
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reported content");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpGet("flagged-comments")]
        public async Task<IActionResult> GetFlaggedComments(CancellationToken cancellationToken)
        {
            try
            {
                var query = new GetFlaggedCommentsQuery();
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flagged comments");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("approve-course")]
        public async Task<IActionResult> ApproveCourse(
            [FromBody] ApproveCourseDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (adminId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new ApproveCourseCommand
                {
                    CourseId = dto.CourseId,
                    AdminId = adminId
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Course not found", success = false });

                return Ok(new { message = "Course approved successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving course");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("reject-course")]
        public async Task<IActionResult> RejectCourse(
           [FromBody] RejectCourseDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (adminId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new RejectCourseCommand
                {
                    CourseId = dto.CourseId,
                    AdminId = adminId,
                    Reason = dto.Reason
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Course not found", success = false });

                return Ok(new { message = "Course rejected", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting course");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("approve-comment")]
        public async Task<IActionResult> ApproveComment(
            [FromBody] ApproveCommentDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = new ApproveCommentCommand
                {
                    CommentId = dto.CommentId
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Comment not found", success = false });

                return Ok(new { message = "Comment approved", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving comment");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("remove-comment")]
        public async Task<IActionResult> RemoveComment(
                    [FromBody] RemoveCommentDto dto,
                    CancellationToken cancellationToken)
        {
            try
            {
                var command = new RemoveCommentCommand
                {
                    CommentId = dto.CommentId
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Comment not found", success = false });

                return Ok(new { message = "Comment removed", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing comment");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("take-action")]
        public async Task<IActionResult> TakeAction(
                   [FromBody] ModerationActionDto dto,
                   CancellationToken cancellationToken)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (adminId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new TakeModerationActionCommand
                {
                    ReportId = dto.ReportId,
                    AdminId = adminId,
                    Action = dto.Action
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Report not found", success = false });

                return Ok(new { message = "Action taken successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking action");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        } 
        [HttpPost("Comment")]
        [Authorize]
        public async Task<IActionResult> CreateComment(
           [FromBody] CreateCommentDto dto,
           CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;

                if (userId == null || userName == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new CreateCommentCommand
                {
                    UserId = userId,
                    UserName = userName,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result == null)
                    return BadRequest(new { message = "Failed to create comment", success = false });

                return Ok(new
                {
                    data = result,
                    message = "Comment created successfully",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
    }
}
