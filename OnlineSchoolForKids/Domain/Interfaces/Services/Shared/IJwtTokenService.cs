using Domain.Entities.Users;

namespace Domain.Interfaces.Services.Shared;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(string userId, string token, string? ipAddress = null, string? deviceInfo = null);
    Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken, string? ipAddress = null);

    string GenerateTempToken(string userId);
    string? ValidateTempToken(string tempToken);
}