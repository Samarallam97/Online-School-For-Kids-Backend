using Domain.Entities.Users;

namespace Domain.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(string userId, string token, string? ipAddress = null, string? deviceInfo = null);
    Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken, string? ipAddress = null);

    string GenerateTempToken(string userId);
    string? ValidateTempToken(string tempToken);
}


public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
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
