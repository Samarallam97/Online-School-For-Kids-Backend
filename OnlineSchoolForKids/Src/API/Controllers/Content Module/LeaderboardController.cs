using Application.Commands.Leaderboard;
using Application.Queries;
using Application.Queries.Leaderboard;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace API.Controllers.Content_Module
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class LeaderboardController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<LeaderboardController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public LeaderboardController(IMediator mediator, ILogger<LeaderboardController> logger, IStringLocalizer<SharedResource> localizer)
        {
            _mediator = mediator;
            _logger = logger;
            _localizer = localizer;
        }

        // ── Existing endpoints (unchanged) ────────────────────────────────────

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
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });

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
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyStats(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });

                var query = new GetUserStatsQuery { UserId = userId };
                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                    return NotFound(new { message = _localizer["UserStatsNotFound"], success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
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
                    return NotFound(new { message = _localizer["UserStatsNotFound"], success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
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
                    return BadRequest(new { message = _localizer["FailedToAwardPoints"], success = false });

                return Ok(new { message = _localizer["PointsAwardedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding points");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

        [HttpPost("update-streak")]
        public async Task<IActionResult> UpdateStreak(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });

                var command = new UpdateStreakCommand { UserId = userId };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = _localizer["FailedToUpdateStreak"], success = false });

                return Ok(new { message = _localizer["StreakUpdated"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating streak");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
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
                    return BadRequest(new { message = _localizer["FailedToRecalculateRanks"], success = false });

                return Ok(new { message = _localizer["RanksRecalculatedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating ranks");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

        [HttpPost("create-badge")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBadge(CreateBadgeDto dto)
        {
            try
            {
                var command = new CreateBadgeCommand { Dto = dto };
                var badgeId = await _mediator.Send(command);
                return Ok(new { message = _localizer["BadgeCreatedSuccessfully"], id = badgeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating badge");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

        // ── NEW: GET /api/leaderboard/badges/me ───────────────────────────────
        // Returns all badges with IsEarned flag for the current user.

        [HttpGet("badges/me")]
        public async Task<IActionResult> GetMyBadges(CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });

                var query = new GetMyBadgesQuery { UserId = userId };
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting badges");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

        // ── NEW: GET /api/leaderboard/transactions?limit=20 ───────────────────
        // Returns the current user's recent point transactions, newest first.

        [HttpGet("transactions")]
        public async Task<IActionResult> GetMyTransactions(
            [FromQuery] int limit = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });

                var query = new GetMyTransactionsQuery { UserId = userId, Limit = limit };
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
    }
}