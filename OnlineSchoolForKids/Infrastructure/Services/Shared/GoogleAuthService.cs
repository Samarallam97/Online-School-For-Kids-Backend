using Domain.Interfaces.Services.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Shared;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleAuthService> _logger;
    private const string GoogleTokenInfoUrl = "https://oauth2.googleapis.com/tokeninfo";

    public GoogleAuthService(HttpClient httpClient, ILogger<GoogleAuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GoogleUserInfo?> ValidateGoogleTokenAsync(
        string googleToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{GoogleTokenInfoUrl}?id_token={googleToken}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token validation failed with status: {StatusCode}", response.StatusCode);
                return null;
            }

            var tokenInfo = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);

            if (tokenInfo == null)
            {
                return null;
            }

            return new GoogleUserInfo
            {
                GoogleId = tokenInfo.Sub,
                Email = tokenInfo.Email,
                FullName = tokenInfo.Name ?? tokenInfo.Email,
                ProfilePictureUrl = tokenInfo.Picture,
                EmailVerified = tokenInfo.EmailVerified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return null;
        }
    }

    private class GoogleTokenResponse
    {
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Picture { get; set; }
        public bool EmailVerified { get; set; }
    }
}
