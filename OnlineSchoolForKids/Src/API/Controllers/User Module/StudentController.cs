using Application.Commands.Profile.Students;
using Application.Queries.Content;
using Application.Queries.Profile.Students;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {

        private readonly IMediator _mediator;
        private readonly IUserRepository _userRepository;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public StudentController(IMediator mediator, IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
        {
            _mediator = mediator;
            _userRepository = userRepository;
            _localizer = localizer;
        }

        [HttpGet("courses")]
        [Authorize]
        public async Task<IActionResult> GetEnrolledCourses()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            // Fetch courses based on user.EnrolledCourseIds
            //var courses = await _courseRepository.GetCoursesByIdsAsync(user.EnrolledCourseIds);


            return Ok();
        }

        [HttpGet("achievements")]
        [Authorize]
        public async Task<IActionResult> GetAchievements()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            // Fetch achievements based on user.AchievementIds
            //var achievements = await _achievementRepository.GetAchievementsByIdsAsync(user.AchievementIds);

            return Ok();
        }

        [HttpPost("accept-invite")]
        [ProducesResponseType(typeof(AcceptParentInviteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptChildInvite([FromBody] AcceptInviteRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new AcceptParentInviteCommand
                {
                    Token = request.Token,
                    ChildUserId = userId
                };

                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("linked-parent")]
        [ProducesResponseType(typeof(ParentInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetLinkedParent()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var query = new GetLinkedParentQuery
                {
                    StudentUserId = userId
                };

                var result = await _mediator.Send(query);

                if (result == null)
                    return NotFound(new { message = _localizer["NoParentLinkedToAccount"] });

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("unlink-parent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnlinkParent()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new UnlinkParentCommand
                {
                    StudentUserId = userId
                };

                await _mediator.Send(command);
                return Ok(new { message = _localizer["ParentAccountUnlinkedSuccessfully"] });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("enrollments")]
        [Authorize]
        public async Task<IActionResult> GetMyEnrollments(CancellationToken ct)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });

                var result = await _mediator.Send(
                    new GetUserEnrollmentsQuery { UserId = userId }, ct);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }
    }
}
