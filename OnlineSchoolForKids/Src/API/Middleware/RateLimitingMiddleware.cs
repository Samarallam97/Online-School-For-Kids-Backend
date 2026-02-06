using System.Collections.Concurrent;

namespace API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _requestLimit;
    private readonly TimeSpan _timeWindow;
    private static readonly ConcurrentDictionary<string, (DateTime WindowStart, int RequestCount)> _clients = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        int requestLimit = 100,
        int timeWindowSeconds = 60)
    {
        _next = next;
        _requestLimit = requestLimit;
        _timeWindow = TimeSpan.FromSeconds(timeWindowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var now = DateTime.UtcNow;

        var (windowStart, requestCount) = _clients.GetOrAdd(clientId, (now, 0));

        // Check if we're still in the same time window
        if (now - windowStart > _timeWindow)
        {
            // Reset the window
            _clients[clientId] = (now, 1);
        }
        else
        {
            // Increment the request count
            requestCount++;
            _clients[clientId] = (windowStart, requestCount);

            if (requestCount > _requestLimit)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = (windowStart + _timeWindow - now).TotalSeconds
                });
                return;
            }
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use IP address as identifier
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        // Could also use user ID if authenticated
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return userId ?? ipAddress ?? "unknown";
    }

    // Cleanup old entries periodically
    public static void CleanupOldEntries()
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
        var keysToRemove = _clients
            .Where(kvp => kvp.Value.WindowStart < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _clients.TryRemove(key, out _);
        }
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(
        this IApplicationBuilder builder,
        int requestLimit = 100,
        int timeWindowSeconds = 60)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(requestLimit, timeWindowSeconds);
    }
}

