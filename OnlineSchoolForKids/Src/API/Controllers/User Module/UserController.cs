using Application;
using Application.Commands.Profile.Creator;
using Application.Commands.Profile.Users;
using Application.DTOs.Profile;
using Application.Queries.Profile.Users;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;

    public UserController(IMediator mediator, IUserRepository userRepository)
    {
        _mediator = mediator;
        _userRepository = userRepository;
    }


    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(Helper.MapToProfileDto(user));
    }

  
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest updateProfileRequest)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var command = new UpdateProfileCommand
        {
            UserId = userId,
            FullName = updateProfileRequest.FullName,
            Phone = updateProfileRequest.Phone,
            Country = updateProfileRequest.Country,
            Bio = updateProfileRequest.Bio,
            LearningGoals = updateProfileRequest.LearningGoals,
            ExpertiseTags = updateProfileRequest.ExpertiseTags,
            ProfessionalTitle = updateProfileRequest.ProfessionalTitle,
            YearsOfExperience = updateProfileRequest.YearsOfExperience,
        };

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("notifications")]
    [ProducesResponseType(typeof(NotificationPreferences), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetNotificationPreferences()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var query = new GetNotificationPreferencesQuery { UserId = userId };
            var preferences = await _mediator.Send(query);

            return Ok(preferences);
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


    [HttpPut("notifications")]
    [ProducesResponseType(typeof(NotificationPreferences), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] NotificationPreferences preferences)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new UpdateNotificationPreferencesCommand
            {
                UserId = userId,
                Preferences = preferences
            };

            var updatedPreferences = await _mediator.Send(command);

            return Ok(updatedPreferences);
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

 
    [HttpGet("payment-methods")]
    [ProducesResponseType(typeof(List<PaymentMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPaymentMethods()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var query = new GetPaymentMethodsQuery { UserId = userId };
            var paymentMethods = await _mediator.Send(query);

            return Ok(paymentMethods);
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


    [HttpPost("payment-methods")]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddPaymentMethod([FromBody] AddPaymentMethodDto addPaymentDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new AddPaymentMethodCommand
            {
                UserId = userId,
                Type = addPaymentDto.Type,

                // Card fields
                CardNumber = addPaymentDto.CardNumber,
                ExpiryMonth = addPaymentDto.ExpiryMonth,
                ExpiryYear = addPaymentDto.ExpiryYear,
                Cvv = addPaymentDto.Cvv,
                CardholderName = addPaymentDto.CardholderName,

                // Vodafone Cash
                PhoneNumber = addPaymentDto.PhoneNumber,

                // Instapay
                InstapayId = addPaymentDto.InstapayId,

                // Fawry
                ReferenceNumber = addPaymentDto.ReferenceNumber,

                // Bank Account
                AccountHolderName = addPaymentDto.AccountHolderName,
                BankName = addPaymentDto.BankName,
                AccountNumber = addPaymentDto.AccountNumber,
                IBAN = addPaymentDto.IBAN
            };

            var paymentMethodDto = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetPaymentMethods), paymentMethodDto);
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

    [HttpPut("payment-methods/{paymentMethodId}/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultPaymentMethod(string paymentMethodId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new SetDefaultPaymentMethodCommand
            {
                UserId = userId,
                PaymentMethodId = paymentMethodId
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


    [HttpDelete("payment-methods/{paymentMethodId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePaymentMethod(string paymentMethodId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new RemovePaymentMethodCommand
            {
                UserId = userId,
                PaymentMethodId = paymentMethodId
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


 
    [HttpPost("profile-picture")]
    [ProducesResponseType(typeof(UploadProfilePictureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB limit
    public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile profilePicture)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new UploadProfilePictureCommand
            {
                UserId = userId,
                File = profilePicture
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while uploading the profile picture" });
        }
    }

    [HttpDelete("profile-picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new DeleteProfilePictureCommand
            {
                UserId = userId
            };

            await _mediator.Send(command);
            return Ok(new { message = "Profile picture deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

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
    public async Task<IActionResult> UpdateWorkExperience(string experienceId, [FromBody] WorkExperienceDto dto)
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
    [ProducesResponseType(typeof(PayoutsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPayouts([FromQuery] string? status = null, [FromQuery] int? limit = null, [FromQuery] int? offset = null)
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

    [HttpGet("certifications")]
    public async Task<IActionResult> GetCertifications(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var result = await _mediator.Send(new GetCertificationsQuery(userId), ct);
        return Ok(result);
    }

    [HttpPost("certifications")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddCertification([FromForm] AddCertificationRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var command = new AddCertificationCommand(
            userId,
            request.Name,
            request.Issuer,
            request.Year,
            request.File
        );
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("certifications/{certificationId}")]
    public async Task<IActionResult> DeleteCertification(string certificationId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        await _mediator.Send(new DeleteCertificationCommand(userId, certificationId), ct);
        return NoContent();
    }

    [HttpGet("certifications/{certificationId}/download")]
    public async Task<IActionResult> DownloadCertification(string certificationId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var result = await _mediator.Send(new DownloadCertificationQuery(userId, certificationId), ct);
        return File(result.FileData, result.ContentType, result.FileName);
    }

    [HttpGet("{userId}/public-profile")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicProfile(string userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicProfileQuery(userId), ct);
        return Ok(result);
    }
}

