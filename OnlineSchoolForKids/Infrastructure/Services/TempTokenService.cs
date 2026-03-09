using Domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services;

public class TempTokenService : ITempTokenService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(15);

    public TempTokenService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<string> StorePendingGoogleUserAsync(PendingGoogleUser user)
    {
        var token = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        _cache.Set($"pending_google:{token}", user, Expiry);
        return Task.FromResult(token);
    }

    public Task<PendingGoogleUser?> GetPendingGoogleUserAsync(string token)
    {
        _cache.TryGetValue($"pending_google:{token}", out PendingGoogleUser? user);
        return Task.FromResult(user);
    }

    public Task DeletePendingGoogleUserAsync(string token)
    {
        _cache.Remove($"pending_google:{token}");
        return Task.CompletedTask;
    }
}
