using Application.Commands.Calendar;
using Application.Queries.Content.Calendar;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Application.Commands.Calendar.CreateEventHandler;

namespace API.Controllers.Content_Module
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(IMediator mediator, ILogger<CalendarController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpPost("event")]
        [Authorize(Roles = "ContentCreator,Admin")]
        public async Task<IActionResult> CreateEvent(
                    [FromBody] CreateEventDto dto,
                    CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new CreateEventCommand
                {
                    InstructorId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result == null)
                    return BadRequest(new { message = "Failed to create event", success = false });

                return Ok(new
                {
                    data = result,
                    message = "Event created successfully",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPut("event/{eventId}")]
        [Authorize(Roles = "ContentCreator,Admin")]
        public async Task<IActionResult> UpdateEvent(
            string eventId,
            [FromBody] UpdateEventDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = new UpdateEventCommand
                {
                    EventId = eventId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Event not found", success = false });

                return Ok(new { message = "Event updated successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpDelete("event/{eventId}")]
        [Authorize(Roles = "ContentCreator,Admin")]
        public async Task<IActionResult> DeleteEvent(
                    string eventId,
                    CancellationToken cancellationToken)
        {
            try
            {
                var command = new DeleteEventCommand { EventId = eventId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return NotFound(new { message = "Event not found", success = false });

                return Ok(new { message = "Event deleted successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("join")]     
        public async Task<IActionResult> JoinEvent(
                    [FromBody] JoinEventDto dto,
                    CancellationToken cancellationToken)
        {
            try
            {

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null )
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new JoinEventCommand
                {
                    UserId = userId,
                    EventId = dto.EventId
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });

                return Ok(new
                {
                    data = new { meetingUrl = result.MeetingUrl },
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining event");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetEvent(
            string eventId,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetEventByIdQuery
                {
                    UserId = userId,
                    EventId = eventId
                };

                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                    return NotFound(new { message = "Event not found", success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingEvents(
            [FromQuery] int limit = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetUpcomingEventsQuery
                {
                    UserId = userId,
                    Limit = limit
                };

                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming events");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        /// </summary>
        [HttpGet("month")]
        public async Task<IActionResult> GetCalendarMonth(
            [FromQuery] int year,
            [FromQuery] int month,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetCalendarMonthQuery
                {
                    UserId = userId,
                    Year = year,
                    Month = month
                };

                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting calendar month");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpGet("stats")]
        [Authorize(Roles = "ContentCreator,Admin")]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

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
    }
}
