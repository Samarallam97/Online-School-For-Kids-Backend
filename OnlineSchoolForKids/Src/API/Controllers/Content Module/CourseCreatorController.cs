using Application.Commands;
using Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseCreatorController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CourseCreatorController> _logger;

        public CourseCreatorController(
            IMediator mediator,
            ILogger<CourseCreatorController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }


        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseCommand command)
        {
            try
            {
                var creatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(creatorId))
                    return Unauthorized(new { message = "Invalid user.", success = false });

                command.CreatorId = creatorId;

                var result = await _mediator.Send(command);
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, new { message = "An error occurred while creating the course.", success = false });
            }

        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(UpdateCourseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UpdateCourseResponse>> UpdateCourse(string id, [FromBody] UpdateCourseCommand command)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(currentUserId))
                    return Unauthorized(new { message = "Authentication required.", success = false });

                // Set course ID and instructor ID from route and auth
                command.CourseId = id;
                command.CreatorId = currentUserId;

                var result = await _mediator.Send(command);
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course: CourseId={CourseId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the course.", success = false });
            }
        }
    }
}




