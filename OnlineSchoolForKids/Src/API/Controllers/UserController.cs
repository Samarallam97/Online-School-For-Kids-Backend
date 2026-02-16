using Application;
using Application.Commands.Profile.Users;
using Application.DTOs.Profile;
using Domain.Entities;
using Domain.Interfaces.Repositories;
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

    /// <summary>
    /// Get current user's full profile
    /// </summary>
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

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var command = new UpdateProfileCommand
        {
            UserId = userId,
            FullName = updateDto.FullName,
            Phone = updateDto.Phone,
            Country = updateDto.Country,
            Bio = updateDto.Bio,
            LearningGoals = updateDto.LearningGoals,
            ExpertiseTags = updateDto.ExpertiseTags,
            ProfessionalTitle = updateDto.ProfessionalTitle,
            YearsOfExperience = updateDto.YearsOfExperience,
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

    /// <summary>
    /// Get notification preferences for current user
    /// </summary>
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

            var query = new GetNotificationPreferencesCommand { UserId = userId };
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

    /// <summary>
    /// Update notification preferences for current user
    /// </summary>
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

    /// <summary>
    /// Get payment methods for current user
    /// </summary>
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

            var query = new GetPaymentMethodsCommand { UserId = userId };
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

    /// <summary>
    /// Add a payment method for current user
    /// Supports: Credit/Debit Cards, Vodafone Cash, Instapay, Fawry, Bank Account
    /// </summary>
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
                Cvc = addPaymentDto.Cvc,
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

    /// <summary>
    /// Set a payment method as default
    /// </summary>
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

    /// <summary>
    /// Remove a payment method
    /// </summary>
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


    /// <summary>
    /// Upload profile picture
    /// </summary>
    /// <param name="profilePicture">Image file (max 5MB, supported formats: jpg, jpeg, png, gif, webp)</param>
    /// <returns>Profile picture URL</returns>
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

    /// <summary>
    /// Delete profile picture
    /// </summary>
    /// <returns>Success message</returns>
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

    ///// <summary>
    ///// Add a social link
    ///// </summary>
    //[HttpPost("social-links")]
    //[ProducesResponseType(typeof(SocialLinkDto), StatusCodes.Status201Created)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> AddSocialLink([FromBody] AddSocialLinkDto dto)
    //{
    //    try
    //    {
    //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //        if (userId == null) return Unauthorized();

    //        var command = new AddSocialLinkCommand
    //        {
    //            UserId = userId,
    //            Name = dto.Name,
    //            Value = dto.Value
    //        };

    //        var result = await _mediator.Send(command);
    //        return CreatedAtAction(nameof(GetSocialLinks), result);
    //    }
    //    catch (KeyNotFoundException ex)
    //    {
    //        return NotFound(new { message = ex.Message });
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}

    ///// <summary>
    ///// Update a social link
    ///// </summary>
    //[HttpPut("social-links/{linkId}")]
    //[ProducesResponseType(typeof(SocialLinkDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> UpdateSocialLink(string linkId, [FromBody] UpdateSocialLinkDto dto)
    //{
    //    try
    //    {
    //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //        if (userId == null) return Unauthorized();

    //        var command = new UpdateSocialLinkCommand
    //        {
    //            UserId = userId,
    //            LinkId = linkId,
    //            Name = dto.Name,
    //            Value = dto.Value
    //        };

    //        var result = await _mediator.Send(command);
    //        return Ok(result);
    //    }
    //    catch (KeyNotFoundException ex)
    //    {
    //        return NotFound(new { message = ex.Message });
    //    }
    //    catch (UnauthorizedAccessException)
    //    {
    //        return Forbid();
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}

    ///// <summary>
    ///// Delete a social link
    ///// </summary>
    //[HttpDelete("social-links/{linkId}")]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> DeleteSocialLink(string linkId)
    //{
    //    try
    //    {
    //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //        if (userId == null) return Unauthorized();

    //        var command = new DeleteSocialLinkCommand
    //        {
    //            UserId = userId,
    //            LinkId = linkId
    //        };

    //        await _mediator.Send(command);
    //        return NoContent();
    //    }
    //    catch (KeyNotFoundException ex)
    //    {
    //        return NotFound(new { message = ex.Message });
    //    }
    //    catch (UnauthorizedAccessException)
    //    {
    //        return Forbid();
    //    }
    //}

    ///// <summary>
    ///// Get public profile information for a user (no authentication required)
    ///// </summary>
    //[HttpGet("{userId}/public-profile")]
    //[AllowAnonymous]
    //[ProducesResponseType(typeof(PublicProfileDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> GetPublicProfile(string userId)
    //{
    //    try
    //    {
    //        var query = new GetPublicProfileCommand
    //        {
    //            UserId = userId
    //        };

    //        var result = await _mediator.Send(query);
    //        return Ok(result);
    //    }
    //    catch (KeyNotFoundException ex)
    //    {
    //        return NotFound(new { message = ex.Message });
    //    }
    //}
}