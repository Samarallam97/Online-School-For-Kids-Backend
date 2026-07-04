using Application.DTOs;
using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Auth;

public record Verify2FARequest(string TempToken, string Code);

public record Verify2FACommand(
    string TempToken,
    string Code,
    string? IpAddress = null,
    string? DeviceInfo = null
) : IRequest<Result<AuthResponse>>;

public class Verify2FACommandHandler : IRequestHandler<Verify2FACommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITotpService _totpService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public Verify2FACommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITotpService totpService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _totpService = totpService;
        _localizer = localizer;
    }

    public async Task<Result<AuthResponse>> Handle(Verify2FACommand request, CancellationToken cancellationToken)
    {
        // Validate the temp token and extract userId
        var userId = _jwtTokenService.ValidateTempToken(request.TempToken);
        if (userId == null)
            return Result<AuthResponse>.Failure(_localizer["InvalidOrExpiredSession"]);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<AuthResponse>.Failure(_localizer["UserNotFound"]);

        // Verify the TOTP code
        if (!_totpService.ValidateCode(user.TwoFactorSecret, request.Code))
            return Result<AuthResponse>.Failure(_localizer["InvalidTwoFactorCode"]);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        // Generate final tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _jwtTokenService.CreateRefreshTokenAsync(
            user.Id, refreshToken, request.IpAddress, request.DeviceInfo);

        var userDto = MapToUserDto(user);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = userDto,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Role = user.Role.ToString(),
        ProfilePictureUrl = user.ProfilePictureUrl,
        IsFirstLogin = user.IsFirstLogin
    };
}