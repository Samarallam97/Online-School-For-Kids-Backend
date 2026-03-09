using Application.DTOs;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using MediatR;

namespace Application.Commands.Auth;

public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

public record LoginCommand(
    string Email,
    string Password,
    bool RememberMe,
    string? IpAddress = null,
    string? DeviceInfo = null
) : IRequest<Result<AuthResponse>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower(), cancellationToken);

        if (user == null || user.PasswordHash == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        // Check if account is active
        if (user.Status != UserStatus.Active)
        {
            return Result<AuthResponse>.Failure("Account is deactivated or not approved. Please contact support.");
        }

        if (!user.EmailVerified)
        {
            return Result<AuthResponse>.Failure("Verify Email first");
        }

        UserDto userDto = new();

        if (user.IsFirstLogin)
        {
            userDto = MapToUserDto(user, true); 
            user.IsFirstLogin = false;
        }
        else
        {
            userDto = MapToUserDto(user, false);
        }

        if (user.Role == UserRole.Admin && user.TwoFactorEnabled == true)
        {
            // Generate a short-lived temp token to identify the pending session
            var tempToken = _jwtTokenService.GenerateTempToken(user.Id);

            return Result<AuthResponse>.Success(new AuthResponse
            {
                Requires2FA = true,
                TempToken = tempToken
            });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Set refresh token expiry based on RememberMe
        var tokenExpiry = request.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7);

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

    private static UserDto MapToUserDto(User user , bool IsFirstLogin = false) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Role = user.Role.ToString(),
        ProfilePictureUrl = user.ProfilePictureUrl,
        IsFirstLogin = user.IsFirstLogin

    };
}
