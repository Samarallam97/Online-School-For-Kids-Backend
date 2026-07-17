using Application.Commands.Leaderboard;
using Application.DTOs;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Shared;
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
    private readonly IUserPointsRepository _userPointsRepo;
    private readonly IMediator _mediator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUserPointsRepository userPointsRepo,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _userPointsRepo = userPointsRepo;
        _mediator = mediator;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower(), cancellationToken);
        if (user == null || user.PasswordHash == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

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

        // ── Daily login points + streak (once per calendar day) ────────
        var userPoints = await _userPointsRepo.GetOneAsync(up => up.UserId == user.Id, cancellationToken);
        var alreadyCreditedToday = userPoints != null
            && userPoints.LastActivityDate.Date == DateTime.UtcNow.Date;

        if (!alreadyCreditedToday)
        {
            await _mediator.Send(new AwardPointsCommand
            {
                Dto = new AwardPointsDto
                {
                    UserId = user.Id,
                    Points = 5,
                    Reason = "DailyLogin",
                    Description = "Daily login bonus"
                }
            }, cancellationToken);

            await _mediator.Send(new UpdateStreakCommand { UserId = user.Id }, cancellationToken);
        }

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

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

    private static UserDto MapToUserDto(User user, bool IsFirstLogin = false) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Role = user.Role.ToString(),
        ProfilePictureUrl = user.ProfilePictureUrl,
        IsFirstLogin = user.IsFirstLogin
    };
}