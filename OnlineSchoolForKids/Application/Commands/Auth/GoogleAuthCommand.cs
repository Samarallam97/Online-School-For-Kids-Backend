using Application.Commands.Leaderboard;
using Application.DTOs;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using MediatR;

namespace Application.Commands.Auth;

public record GoogleAuthRequest(
    string GoogleId,
    string Email,
    string FullName,
    string? ProfilePictureUrl,
    bool EmailVerified,
    UserRole? Role
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
    private readonly IUserPointsRepository _userPointsRepo;
    private readonly IMediator _mediator;

    public GoogleAuthCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITempTokenService tempTokenService,
        IUserPointsRepository userPointsRepo,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _tempTokenService = tempTokenService;
        _userPointsRepo = userPointsRepo;
        _mediator = mediator;
    }

    public async Task<Result<GoogleAuthResponse>> Handle(
        GoogleAuthCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByGoogleIdAsync(request.GoogleId, cancellationToken)
                   ?? await _userRepository.GetByEmailAsync(request.Email.ToLower(), cancellationToken);

        bool isNewUser = user == null;

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

            if (request.Role == UserRole.ContentCreator || request.Role == UserRole.Specialist)
                user.Status = UserStatus.Pending;

            await _userRepository.CreateAsync(user, cancellationToken);
        }
        else
        {
            if (user!.GoogleId == null)
                user.GoogleId = request.GoogleId;

            user.LastLoginAt = DateTime.UtcNow;
            user.EmailVerified = true;

            if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(request.ProfilePictureUrl))
                user.ProfilePictureUrl = request.ProfilePictureUrl;

            await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

            // ── Daily login points + streak (existing users only) ──────
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
        }

        if (user!.Status != UserStatus.Active)
        {
            return Result<GoogleAuthResponse>.Failure(
                "Account is deactivated or not approved. Please contact support.");
        }

        var userDto = Helper.MapToUserDto(user);
        if (user.IsFirstLogin)
        {
            user.IsFirstLogin = false;
            await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
        }

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
    public string? TempToken { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserDto? User { get; set; }
    public DateTime ExpiresAt { get; set; }
}