using Application.Commands.Leaderboard;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.Content_Module
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaderboardController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<LeaderboardController> _logger;

        public LeaderboardController(IMediator mediator, ILogger<LeaderboardController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetLeaderboard(
            [FromQuery] string period = "AllTime",
            [FromQuery] int limit = 100,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetLeaderboardQuery
                {
                    UserId = userId,
                    Period = period,
                    Limit = limit
                };

                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leaderboard");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpGet("me")]
        public async Task<IActionResult> GetMyStats(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetUserStatsQuery { UserId = userId };
                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                    return NotFound(new { message = "User stats not found", success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserStats(
            string userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var query = new GetUserStatsQuery { UserId = userId };
                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                    return NotFound(new { message = "User stats not found", success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("award-points")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AwardPoints(
            [FromBody] AwardPointsDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = new AwardPointsCommand { Dto = dto };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = "Failed to award points", success = false });

                return Ok(new { message = "Points awarded successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding points");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("update-streak")]
        public async Task<IActionResult> UpdateStreak(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new UpdateStreakCommand { UserId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = "Failed to update streak", success = false });

                return Ok(new { message = "Streak updated", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating streak");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("recalculate-ranks")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecalculateRanks(CancellationToken cancellationToken)
        {
            try
            {
                var command = new RecalculateRanksCommand();
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = "Failed to recalculate ranks", success = false });

                return Ok(new { message = "Ranks recalculated successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating ranks");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
        [HttpPost("create-badge")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBadge(CreateBadgeDto dto)
        {
            try
            {
                var command = new CreateBadgeCommand
                {
                    Dto = dto
                };

                var badgeId = await _mediator.Send(command);

                return Ok(new
                {
                    message = "Badge created successfully",
                    id = badgeId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating badge");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
    }
}
    
