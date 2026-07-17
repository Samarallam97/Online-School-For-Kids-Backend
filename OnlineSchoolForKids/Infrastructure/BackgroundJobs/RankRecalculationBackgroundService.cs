using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Commands.Leaderboard;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;



public class RankRecalculationBackgroundService : BackgroundService
{
    private static readonly TimeSpan RecalculationInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RankRecalculationBackgroundService> _logger;

    public RankRecalculationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RankRecalculationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunRecalculationAsync(stoppingToken);

            try
            {
                await Task.Delay(RecalculationInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunRecalculationAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            _logger.LogInformation("Starting scheduled rank recalculation");
            var success = await mediator.Send(new RecalculateRanksCommand(), ct);
            _logger.LogInformation(
                "Scheduled rank recalculation finished: {Result}",
                success ? "success" : "failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled rank recalculation");
        }
    }
}