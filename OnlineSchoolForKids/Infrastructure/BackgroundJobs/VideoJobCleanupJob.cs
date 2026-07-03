using Application.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Runs once a day and deletes stored video files for VideoProcessingJobs
/// that are older than the retention window and were never completed
/// (no chunk ever saved as a lesson). Keeps disk usage from growing
/// unbounded with abandoned upload sessions.
/// </summary>
public class VideoJobCleanupJob : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromHours(24);
    private const int RetentionDays = 7;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoJobCleanupJob> _logger;

    public VideoJobCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<VideoJobCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VideoJobCleanupJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var cleaned = await mediator.Send(
                    new CleanupStaleVideoJobsCommand { RetentionDays = RetentionDays },
                    stoppingToken);

                if (cleaned > 0)
                    _logger.LogInformation(
                        "Cleaned up {Count} stale video processing job file(s) at {Time}.",
                        cleaned, DateTime.UtcNow);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in VideoJobCleanupJob.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}