using Application.DTOs;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using MediatR;


namespace Application.Commands.Auth;


public record GoogleAuthRequest(
    string GoogleId,
    string Email,
    string FullName,
    string? ProfilePictureUrl,
    bool EmailVerified,
    UserRole? Role           // null when the backend could not determine role yet
);


public record GoogleAuthCommand(
    string GoogleId,
    string Email,
    string FullName,
    string? ProfilePictureUrl,
    bool EmailVerified,
    UserRole? Role,
    string? IpAddress = null,
    string? DeviceInfo = null
) : IRequest<Result<GoogleAuthResponse>>;


public class GoogleAuthCommandHandler : IRequestHandler<GoogleAuthCommand, Result<GoogleAuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITempTokenService _tempTokenService;

    public GoogleAuthCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITempTokenService tempTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _tempTokenService = tempTokenService;
    }

    public async Task<Result<GoogleAuthResponse>> Handle(
        GoogleAuthCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Look up existing user ─────────────────────────────────────────
        var user = await _userRepository.GetByGoogleIdAsync(request.GoogleId, cancellationToken)
                   ?? await _userRepository.GetByEmailAsync(request.Email.ToLower(), cancellationToken);

        bool isNewUser = user == null;

        // ── 2. New user – role not yet known → defer to role-selection page ──
        if (isNewUser && request.Role == null)
        {
            var tempToken = await _tempTokenService.StorePendingGoogleUserAsync(new PendingGoogleUser
            {
                GoogleId = request.GoogleId,
                Email = request.Email,
                FullName = request.FullName,
                ProfilePictureUrl = request.ProfilePictureUrl,
                EmailVerified = request.EmailVerified
            });

            return Result<GoogleAuthResponse>.Success(new GoogleAuthResponse
            {
                RequiresRoleSelection = true,
                TempToken = tempToken
            });
        }

        // ── 3. New user + role provided → create account ─────────────────────
        if (isNewUser)
        {
            user = new User
            {
                FullName = request.FullName,
                Email = request.Email.ToLower(),
                GoogleId = request.GoogleId,
                Role = request.Role!.Value,
                AuthProvider = AuthProvider.Google,
                EmailVerified = request.EmailVerified,
                ProfilePictureUrl = request.ProfilePictureUrl,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            // ContentCreator and Specialist need admin approval — mirrors RegisterCommandHandler
            if (request.Role == UserRole.ContentCreator || request.Role == UserRole.Specialist)
                user.Status = UserStatus.Pending;

            await _userRepository.CreateAsync(user, cancellationToken);
        }
        // ── 4. Existing user → update login metadata (role is never changed) ─
        else
        {
            if (user!.GoogleId == null)
                user.GoogleId = request.GoogleId;

            user.LastLoginAt = DateTime.UtcNow;
            user.EmailVerified = true; // Google guarantees email ownership

            if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(request.ProfilePictureUrl))
                user.ProfilePictureUrl = request.ProfilePictureUrl;

            await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
        }

        // ── 5. Check account status — mirrors LoginCommandHandler ─────────────
        if (user!.Status != UserStatus.Active)
        {
            return Result<GoogleAuthResponse>.Failure(
                "Account is deactivated or not approved. Please contact support.");
        }

        // Uses the shared Helper.MapToUserDto — same as RegisterCommandHandler

        var userDto = Helper.MapToUserDto(user);
        if (user.IsFirstLogin)
        {
            user.IsFirstLogin = false;
            await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
        }

        // ── 7. Generate tokens ────────────────────────────────────────────────
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _jwtTokenService.CreateRefreshTokenAsync(
            user.Id,
            refreshToken,
            request.IpAddress,
            request.DeviceInfo
        );

        return Result<GoogleAuthResponse>.Success(new GoogleAuthResponse
        {
            RequiresRoleSelection = false,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = userDto,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }
}

public class GoogleAuthResponse
{
    public bool RequiresRoleSelection { get; set; }

    // Populated only when RequiresRoleSelection = true
    public string? TempToken { get; set; }

    // Populated only when RequiresRoleSelection = false
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserDto? User { get; set; }
    public DateTime ExpiresAt { get; set; }
}
