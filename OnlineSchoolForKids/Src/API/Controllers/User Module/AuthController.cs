using Application.Commands.Auth;
using Application.Queries.Auth;
using Domain.Enums.Users;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;


    public AuthController(IMediator mediator, IConfiguration configuration )
    {
        _mediator = mediator;        _configuration=configuration;

    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.Role,
            request.DateOfBirth,
            request.Country,
            request.Expertise,
            request.PortfolioUrl,
            request.CvLink
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }


    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var command = new ResendVerificationEmailCommand(request.Email);
        var result = await _mediator.Send(command);

        return Ok(new { message = result.Data });
    }



    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var command = new VerifyEmailCommand(request.Token);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { message = result.Data });
    }

    
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var command = new LoginCommand(
            request.Email,
            request.Password,
            request.RememberMe,
            ipAddress,
            userAgent
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("login/verify-2fa")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var command = new Verify2FACommand(request.TempToken, request.Code, ipAddress, userAgent);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }


    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var result = await _mediator.Send(command);

        return Ok(new { message = result.Data });
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var command = new ResetPasswordCommand(
            request.Token,
            request.NewPassword
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { message = result.Data });
    }


    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new RefreshTokenCommand(request.RefreshToken, ipAddress);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Invalid user." });

        var result = await _mediator.Send(
            new LogOutCommand(userId));

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = result.Data });
    }


    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? role = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), "Auth", null, Request.Scheme)
        };

        // Piggy-back the role through OAuth state so it survives the redirect
        if (!string.IsNullOrEmpty(role))
            properties.Items["role"] = role;

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var frontendUrl = _configuration["FrontUrl"] ?? "http://localhost:5173";

        var authResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!authResult.Succeeded)
            return Redirect($"{frontendUrl}/login?error=google_failed");

        // Extract claims from the authenticated principal
        var googleId = authResult.Principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = authResult.Principal!.FindFirst(ClaimTypes.Email)?.Value;
        var name = authResult.Principal!.FindFirst(ClaimTypes.Name)?.Value;
        var picture = authResult.Principal!.FindFirst("picture")?.Value;
        var emailVerified = authResult.Principal!.FindFirst("email_verified")?.Value == "true";

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            return Redirect($"{frontendUrl}/login?error=google_failed");

        // Recover the role that was stored before the redirect (may be null)
        authResult.Properties!.Items.TryGetValue("role", out var roleStr);
        UserRole? parsedRole = null;
        if (!string.IsNullOrEmpty(roleStr) && Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var r))
            parsedRole = r;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var command = new GoogleAuthCommand(
            googleId,
            email,
            name ?? email,
            picture,
            emailVerified,
            parsedRole,
            ipAddress,
            userAgent
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return Redirect($"{frontendUrl}/login?error={Uri.EscapeDataString(result.Error ?? "auth_failed")}");

        // New user – role unknown → send to role-selection page
        if (result.Data!.RequiresRoleSelection)
        {
            return Redirect(
                $"{frontendUrl}/complete-profile" +
                $"?temp_token={Uri.EscapeDataString(result.Data.TempToken!)}");
        }

        // Success → pass tokens to the frontend callback page
        return Redirect(
            $"{frontendUrl}/auth/callback" +
            $"?access_token={Uri.EscapeDataString(result.Data.AccessToken!)}" +
            $"&refresh_token={Uri.EscapeDataString(result.Data.RefreshToken!)}" +
            $"&expires_at={Uri.EscapeDataString(result.Data.ExpiresAt.ToString("O"))}");
    }

    [HttpPost("google/complete-registration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteGoogleRegistration(
     [FromBody] CompleteGoogleRegistrationRequest request)
    {
        // Parse role string → enum
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return BadRequest(new { error = $"Invalid role: {request.Role}" });

        // Parse date string → DateTime
        if (!DateTime.TryParse(request.DateOfBirth, out var dateOfBirth))
            return BadRequest(new { error = $"Invalid date of birth: {request.DateOfBirth}" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var command = new CompleteGoogleRegistrationCommand(
            request.TempToken,
            role,
          dateOfBirth,
            request.Country,
            request.Expertise,
            request.CvLink,
            request.PortfolioUrl,
            ipAddress,
            userAgent
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new
        {
            id = userId,
            email,
            fullName = name,
            role
        });
    }


    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        string UserId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var result = await _mediator.Send(new Get2FAStatusQuery(UserId), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup(CancellationToken ct)
    {
        string UserId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var result = await _mediator.Send(new Setup2FACommand(UserId), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("confirm-setup")]
    public async Task<IActionResult> ConfirmSetup([FromBody] ConfirmSetupRequest req, CancellationToken ct)
    {
        string UserId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var result = await _mediator.Send(new ConfirmSetup2FACommand(UserId, req.Secret, req.Code), ct);
        return result.IsSuccess ? Ok(new { message = result.Data }) : BadRequest(new { error = result.Error });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] DisableRequest req, CancellationToken ct)
    {
        string UserId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var result = await _mediator.Send(new Disable2FACommand(UserId, req.Code), ct);
        return result.IsSuccess ? Ok(new { message = result.Data }) : BadRequest(new { error = result.Error });
    }
}

