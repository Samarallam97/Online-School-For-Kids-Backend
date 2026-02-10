using Application.Commands;
using Application.Dtos;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CourseController> _logger;

        public CourseController(IMediator mediator, ILogger<CourseController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpGet]
        public async Task<ActionResult<PagedResult<CourseDto>>> GetCourses([FromQuery] GetCoursesQuery query)
        {
            try
            {
                //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //query.UserId = userId;

                var result = await _mediator.Send(query);
                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course list");
                return StatusCode(500, new { message = "An error occurred while retrieving courses.", success = false });
            }
        }
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Student")]

        public async Task<ActionResult<CourseDto>> GetCourseById(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


                var query = new GetCourseByIdQuery
                {
                    CourseId = id,
                    UserId = userId
                };
                var result = await _mediator.Send(query);

                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Course not found or not available",
                        success = false
                    });
                }
                return Ok(new { data = result, success = true });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course {CourseId}", id);
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving the course.",
                    success = false
                });
            }
        }
        [HttpGet("recommended")]
        [ProducesResponseType(typeof(IEnumerable<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetRecommendedCourses()
        {
            try
            {
                // TODO: Implement recommendation logic based on:
                // - User's enrolled courses
                // - User's wishlist
                // - User's browsing history
                // - Similar users' preferences
                // - Course categories user is interested in
                // - Collaborative filtering or content-based filtering

                _logger.LogInformation("GetRecommendedCourses called - returning null (not implemented)");

                return Ok(new
                {
                    data = (IEnumerable<CourseDto>?)null,
                    message = "Recommendation feature coming soon",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended courses");
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving recommended courses",
                    success = false
                });
            }
        }
        [HttpGet("trending")]
        [ProducesResponseType(typeof(IEnumerable<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetTrendingCourses()
        {
            try
            {
                // TODO: Implement trending logic based on:
                // - Recent enrollment count (last 7/30 days)
                // - Rating trends (recently highly rated)
                // - View count increase
                // - Social shares
                // - Completion rate
                // - Time-weighted scoring algorithm

                _logger.LogInformation("GetTrendingCourses called - returning null (not implemented)");

                return Ok(new
                {
                    data = (IEnumerable<CourseDto>?)null,
                    message = "Trending feature coming soon",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending courses");
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving trending courses",
                    success = false
                });
            }
        }
        [HttpPost("favourite")]
        [ProducesResponseType(typeof(AddToFavouriteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AddToFavouriteResponse>> AddToFavourites([FromBody] AddToFavouriteDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated", success = false });
                }

                var command = new AddToFavouriteCommand
                {
                    CourseId = dto.CourseId,
                    UserId = userId
                };

                var result = await _mediator.Send(command);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(new { message = result.Message, success = false });

                    return BadRequest(new { message = result.Message, success = false });
                }

                return Ok(new
                {
                    data = result,
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding course to favourites");
                return StatusCode(500, new
                {
                    message = "An error occurred while adding to favourites",
                    success = false
                });
            }
        }

        [HttpDelete("favourite/{courseId}")]
        [ProducesResponseType(typeof(DeleteFromFavouriteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeleteFromFavouriteResponse>> RemoveFromFavourites(string courseId)
        {
            try {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated", success = false });
                }
                var command = new DeleteFromFavouriteCommand
                {
                    CourseId = courseId,
                    UserId = userId
                };

                var result = await _mediator.Send(command);

                if (!result.Success)
                {
                    return NotFound(new { message = result.Message, success = false });
                }

                return Ok(new
                {
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing course from favourites");
                return StatusCode(500, new
                {
                    message = "An error occurred while removing from favourites",
                    success = false
                });
            }
        }

        
       


    }
}

