using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Auth;

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
        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure("Account is deactivated. Please contact support.");
        }

        if (!user.EmailVerified)
        {
            return Result<AuthResponse>.Failure("Verify Email first");
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
            User = MapToUserDto(user),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString(),
        EmailVerified = user.EmailVerified,
        ProfilePictureUrl = user.ProfilePictureUrl,
        CreatedAt = user.CreatedAt
    };
}
