using Application.Commands.Profile;
using Application.Commands.Profile.Creator;
using Application.DTOs.Profile;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentCreatorController : ControllerBase
    {

        private readonly IMediator _mediator;
        private readonly IUserRepository _userRepository;

        public ContentCreatorController(IMediator mediator, IUserRepository userRepository)
        {
            _mediator = mediator;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Get social links for current user
        /// </summary>
        [HttpGet("social-links")]
        [ProducesResponseType(typeof(List<SocialLinkDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSocialLinks()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var query = new GetSocialLinksCommand { UserId = userId };
                var links = await _mediator.Send(query);

                return Ok(links);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


        /// <summary>
        /// Add a social link
        /// </summary>
        [HttpPost("social-links")]
        [ProducesResponseType(typeof(SocialLinkDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddSocialLink([FromBody] SocialLinkDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new AddSocialLinkCommand
                {
                    UserId = userId,
                    Name = dto.Name,
                    Value = dto.Value
                };

                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetSocialLinks), result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update a social link
        /// </summary>
        [HttpPut("social-links/{linkId}")]
        [ProducesResponseType(typeof(SocialLinkDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSocialLink(string linkId, [FromBody] UpdateSocialLinkDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new UpdateSocialLinkCommand
                {
                    UserId = userId,
                    LinkId = linkId,
                    Name = dto.Name,
                    Value = dto.Value
                };

                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a social link
        /// </summary>
        [HttpDelete("social-links/{linkId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSocialLink(string linkId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new DeleteSocialLinkCommand
                {
                    UserId = userId,
                    LinkId = linkId
                };

                await _mediator.Send(command);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }



        [HttpGet("experiences")]
        [ProducesResponseType(typeof(List<WorkExperienceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetWorkExperiences()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new GetWorkExperiencesCommand { UserId = userId };
                var experiences = await _mediator.Send(command);

                return Ok(experiences);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("experiences")]
        [ProducesResponseType(typeof(WorkExperienceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddWorkExperience([FromBody] WorkExperienceDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new AddWorkExperienceCommand
                {
                    UserId = userId,
                    Title = dto.Title,
                    Place = dto.Place,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    IsCurrentRole = dto.IsCurrentRole
                };

                var experience = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetWorkExperiences), new { id = experience.Id }, experience);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("experiences/{experienceId}")]
        [ProducesResponseType(typeof(WorkExperienceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWorkExperience(
            string experienceId,
            [FromBody] WorkExperienceDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new UpdateWorkExperienceCommand
                {
                    UserId = userId,
                    ExperienceId = experienceId,
                    Title = dto.Title,
                    Place = dto.Place,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    IsCurrentRole = dto.IsCurrentRole
                };

                var experience = await _mediator.Send(command);
                return Ok(experience);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("experiences/{experienceId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWorkExperience(string experienceId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var command = new DeleteWorkExperienceCommand
                {
                    UserId = userId,
                    ExperienceId = experienceId
                };

                await _mediator.Send(command);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


        [HttpGet("payouts")]
        [Authorize(Roles = "ContentCreator")]
        [ProducesResponseType(typeof(PayoutsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPayouts(
            [FromQuery] string? status = null,
            [FromQuery] int? limit = null,
            [FromQuery] int? offset = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                PayoutStatus? payoutStatus = null;
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<PayoutStatus>(status, true, out var parsedStatus))
                    {
                        payoutStatus = parsedStatus;
                    }
                }

                var query = new GetPayoutsQuery
                {
                    CreatorId = userId,
                    Status = payoutStatus,
                    Limit = limit,
                    Offset = offset
                };

                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching payouts" });
            }
        }

    }
}
