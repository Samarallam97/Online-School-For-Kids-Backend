using Application.Commands.Profile;
using Application.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ParentController : ControllerBase
{
    private readonly IMediator _mediator;

    public ParentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search for a child account by email
    /// </summary>
    [HttpGet("search-child")]
    [ProducesResponseType(typeof(SearchChildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchChild([FromQuery] string email)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var query = new SearchChildQuery
            {
                ParentUserId = userId,
                Email = email
            };

            var result = await _mediator.Send(query);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Send link invitation to an existing child account
    /// </summary>
    [HttpPost("send-invite/{childId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendChildInvite(string childId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new SendChildInviteCommand
            {
                ParentUserId = userId,
                ChildId = childId
            };

            await _mediator.Send(command);

            return Ok(new { message = "Invitation sent successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new child account and link it to parent
    /// </summary>
    [HttpPost("create-child")]
    [ProducesResponseType(typeof(ChildDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAndLinkChild([FromBody] CreateChildDto createChildDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new CreateAndLinkChildCommand
            {
                ParentUserId = userId,
                FullName = createChildDto.FullName,
                Email = createChildDto.Email,
                Password = createChildDto.Password,
                ConfirmPassword = createChildDto.ConfirmPassword,
                DateOfBirth = createChildDto.DateOfBirth,
                Country = createChildDto.Country
            };

            var childDto = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetLinkedChildren), childDto);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    ///// <summary>
    ///// Get child's course progress and learning stats
    ///// </summary>
    //[HttpGet("children/{childId}/progress")]
    //[ProducesResponseType(typeof(ChildProgressDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> GetChildProgress(string childId)
    //{
    //    try
    //    {
    //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //        if (userId == null) return Unauthorized();

    //        var query = new GetChildProgressQuery
    //        {
    //            ParentUserId = userId,
    //            ChildId = childId
    //        };

    //        var progress = await _mediator.Send(query);

    //        return Ok(progress);
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

    /// <summary>
    /// Get linked children for current parent
    /// </summary>
    [HttpGet("children")]
    [ProducesResponseType(typeof(List<ChildDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLinkedChildren()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var query = new GetLinkedChildrenQuery { UserId = userId };
            var children = await _mediator.Send(query);

            return Ok(children);
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
    /// Remove/unlink a child from parent's account
    /// </summary>
    [HttpDelete("children/{childId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveChild(string childId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new RemoveChildCommand
            {
                UserId = userId,
                ChildId = childId
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
    /// Get notification preferences for current parent
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

    /// <summary>
    /// Update notification preferences for current parent
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
    /// Get payment methods for current parent
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

    /// <summary>
    /// Add a payment method for current parent
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
}


