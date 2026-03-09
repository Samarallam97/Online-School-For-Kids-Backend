using Application.DTOs;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using MediatR;

namespace Application.Commands.Auth;

public record CompleteGoogleRegistrationRequest(
    string TempToken,
    string Role,
    string DateOfBirth,
    string Country,

    // Content Creator & Specialist only
    string? Expertise,
    string? CvLink,
    string? PortfolioUrl
);

public record CompleteGoogleRegistrationCommand(
    string TempToken,
    UserRole Role,
    DateTime DateOfBirth,
    string Country,
    string? Expertise,
    string? CvLink,
    string? PortfolioUrl,
    string? IpAddress = null,
    string? DeviceInfo = null
) : IRequest<Result<AuthResponse>>;


public class CompleteGoogleRegistrationCommandHandler
    : IRequestHandler<CompleteGoogleRegistrationCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITempTokenService _tempTokenService;

    public CompleteGoogleRegistrationCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITempTokenService tempTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _tempTokenService = tempTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(
        CompleteGoogleRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Retrieve pending Google user from temp store ───────────────────
        var pendingUser = await _tempTokenService.GetPendingGoogleUserAsync(request.TempToken);
        if (pendingUser == null)
            return Result<AuthResponse>.Failure(
                "Invalid or expired session. Please try signing in again.");

        // ── 2. Validate role-specific required fields ─────────────────────────
        if (request.Role == UserRole.ContentCreator || request.Role == UserRole.Specialist)
        {
            if (string.IsNullOrWhiteSpace(request.Expertise))
                return Result<AuthResponse>.Failure("Area of expertise is required.");

            if (string.IsNullOrWhiteSpace(request.CvLink))
                return Result<AuthResponse>.Failure("CV link is required.");
        }

        // ── 3. Guard against double-submit ────────────────────────────────────
        var existing = await _userRepository.GetByGoogleIdAsync(pendingUser.GoogleId, cancellationToken)
                       ?? await _userRepository.GetByEmailAsync(pendingUser.Email.ToLower(), cancellationToken);

        User user;

        if (existing != null)
        {
            user = existing;
        }
        else
        {
            user = new User
            {
                FullName = pendingUser.FullName,
                Email = pendingUser.Email.ToLower(),
                GoogleId = pendingUser.GoogleId,
                Role = request.Role,
                AuthProvider = AuthProvider.Google,
                EmailVerified = pendingUser.EmailVerified,
                ProfilePictureUrl = pendingUser.ProfilePictureUrl,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,

                // Fields collected from the form
                DateOfBirth = request.DateOfBirth,
                Country = request.Country,
            };

            // ContentCreator / Specialist — mirrors RegisterCommandHandler exactly
            if (request.Role == UserRole.ContentCreator || request.Role == UserRole.Specialist)
            {
                user.Status = UserStatus.Pending;
                user.ExpertiseTags = new List<string> { request.Expertise! };
                user.CvLink = request.CvLink;
                if (!string.IsNullOrWhiteSpace(request.PortfolioUrl))
                    user.PortfolioUrl = request.PortfolioUrl;
            }

            await _userRepository.CreateAsync(user, cancellationToken);
        }

        // ── 4. Clean up temp token ────────────────────────────────────────────
        await _tempTokenService.DeletePendingGoogleUserAsync(request.TempToken);

        // ── 5. ContentCreator / Specialist must wait for approval ─────────────
        if (user.Status == UserStatus.Pending)
            return Result<AuthResponse>.Failure(
                "Your account is pending admin approval. You will be notified by email.");

        // ── 6. Check account status — mirrors LoginCommandHandler ─────────────
        if (user.Status != UserStatus.Active)
            return Result<AuthResponse>.Failure(
                "Account is deactivated or not approved. Please contact support.");

        // ── 7. Handle IsFirstLogin — mirrors LoginCommandHandler ──────────────
        var userDto = Helper.MapToUserDto(user);
        if (user.IsFirstLogin)
        {
            user.IsFirstLogin = false;
            await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
        }

        // ── 8. Generate tokens ────────────────────────────────────────────────
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _jwtTokenService.CreateRefreshTokenAsync(
            user.Id,
            refreshToken,
            request.IpAddress,
            request.DeviceInfo
        );

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = userDto,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }
}

