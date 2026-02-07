using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;

    public JwtTokenService(
        IOptions<JwtSettings> jwtSettings,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository)
    {
        _jwtSettings = jwtSettings.Value;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("emailVerified", user.EmailVerified.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(
        string userId,
        string token,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo
        };

        await _refreshTokenRepository.CreateAsync(refreshToken);
        return refreshToken;
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress = null)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (token == null || !token.IsActive)
        {
            return null;
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(token.UserId);
        if (user == null || user.Status != UserStatus.Active)
        {
            return null;
        }

        // Revoke old token
        await _refreshTokenRepository.RevokeTokenAsync(refreshToken);

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        // Save new refresh token
        var newToken = await CreateRefreshTokenAsync(user.Id, newRefreshToken, ipAddress, token.DeviceInfo);

        // Update old token to point to new one
        token.ReplacedByToken = newRefreshToken;
        await _refreshTokenRepository.UpdateAsync(token.Id, token);

        return (newAccessToken, newRefreshToken);
    }
}
