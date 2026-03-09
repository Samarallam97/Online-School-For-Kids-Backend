using Application.DTOs;
using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public Verify2FACommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITotpService totpService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _totpService = totpService;
    }

    public async Task<Result<AuthResponse>> Handle(Verify2FACommand request, CancellationToken cancellationToken)
    {
        // Validate the temp token and extract userId
        var userId = _jwtTokenService.ValidateTempToken(request.TempToken);
        if (userId == null)
            return Result<AuthResponse>.Failure("Invalid or expired session. Please log in again.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<AuthResponse>.Failure("User not found.");

        // Verify the TOTP code
        if (!_totpService.ValidateCode(user.TwoFactorSecret, request.Code))
            return Result<AuthResponse>.Failure("Invalid or expired 2FA code.");

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