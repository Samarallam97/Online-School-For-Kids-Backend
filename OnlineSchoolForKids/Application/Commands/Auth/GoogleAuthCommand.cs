using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Auth;

public record GoogleAuthCommand(
    string GoogleToken,
    UserRole Role,
    string? IpAddress = null,
    string? DeviceInfo = null
) : IRequest<Result<AuthResponse>>;

public class GoogleAuthCommandHandler : IRequestHandler<GoogleAuthCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenService _jwtTokenService;

    public GoogleAuthCommandHandler(
        IUserRepository userRepository,
        IGoogleAuthService googleAuthService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _googleAuthService = googleAuthService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
    {
        // Validate Google token
        var googleUserInfo = await _googleAuthService.ValidateGoogleTokenAsync(request.GoogleToken, cancellationToken);

        if (googleUserInfo == null)
        {
            return Result<AuthResponse>.Failure("Invalid Google token.");
        }

        // Check if user exists by Google ID or email
        var user = await _userRepository.GetByGoogleIdAsync(googleUserInfo.GoogleId, cancellationToken)
                   ?? await _userRepository.GetByEmailAsync(googleUserInfo.Email.ToLower(), cancellationToken);

        if (user == null)
        {
            // Create new user
            user = new User
            {
                FullName = googleUserInfo.FullName,
                Email = googleUserInfo.Email.ToLower(),
                GoogleId = googleUserInfo.GoogleId,
                Role = request.Role,
                AuthProvider = AuthProvider.Google,
                EmailVerified = googleUserInfo.EmailVerified,
                ProfilePictureUrl = googleUserInfo.ProfilePictureUrl,
                LastLoginAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user, cancellationToken);
        }
        else
        {
            // Update existing user
            if (user.GoogleId == null)
            {
                user.GoogleId = googleUserInfo.GoogleId;
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.EmailVerified = true; // Google verifies emails

            if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(googleUserInfo.ProfilePictureUrl))
            {
                user.ProfilePictureUrl = googleUserInfo.ProfilePictureUrl;
            }

            await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
        }

        // Generate tokens
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
            User = MapToUserDto(user),
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
