using Application;
using Application.Commands.Profile;
using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
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
    [Authorize]
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
    [Authorize]
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
            ParentalControlsActive = updateDto.ParentalControlsActive,
            NotificationPreferences = updateDto.NotificationPreferences,
            Expertise = updateDto.Expertise,
            SocialLinks = updateDto.SocialLinks,
            ProfessionalTitle = updateDto.ProfessionalTitle,
            Specializations = updateDto.Specializations,
            YearsOfExperience = updateDto.YearsOfExperience,
            HourlyRate = updateDto.HourlyRate,
            SessionRates = updateDto.SessionRates,
            Availability = updateDto.Availability
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
}


    




 