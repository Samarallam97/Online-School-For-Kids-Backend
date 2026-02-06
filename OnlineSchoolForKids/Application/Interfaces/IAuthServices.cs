using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(string userId, string token, string? ipAddress = null, string? deviceInfo = null);
    Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task SendVerificationEmailAsync(string to, string userName, DateTime expiry, string verificationLink, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string to, string userName, DateTime expiry, string resetLink, CancellationToken cancellationToken = default);
    Task SendPasswordChangedEmailAsync(string email);
    Task SendWelcomeEmailAsync(string to, string userName, CancellationToken cancellationToken = default);
}

public interface IGoogleAuthService
{
    Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string googleToken, CancellationToken cancellationToken = default);
}

public class GoogleUserInfo
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool EmailVerified { get; set; }
}
