using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Services.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Leaderboard;

public class RecalculateRanksCommand : IRequest<bool> { }

public class RecalculateRanksHandler : IRequestHandler<RecalculateRanksCommand, bool>
{
    private readonly IUserPointsRepository _userPointsRepo;
    private readonly INotificationService _notification;
    private readonly ILogger<RecalculateRanksHandler> _logger;

    public RecalculateRanksHandler(
        IUserPointsRepository userPointsRepo,
        INotificationService notification,
        ILogger<RecalculateRanksHandler> logger)
    {
        _userPointsRepo = userPointsRepo;
        _notification = notification;
        _logger = logger;
    }

    public async Task<bool> Handle(RecalculateRanksCommand request, CancellationToken ct)
    {
        try
        {
            var allUsers = await _userPointsRepo.GetAllAsync(_ => true, ct);

            var sortedUsers = allUsers
                .OrderByDescending(u => u.TotalPoints)
                .ToList();

            var oldRanks = allUsers.ToDictionary(x => x.UserId, x => x.Rank);

            for (int i = 0; i < sortedUsers.Count; i++)
            {
                var userPoints = sortedUsers[i];

                int previousRank = oldRanks.ContainsKey(userPoints.UserId)
                    ? oldRanks[userPoints.UserId]
                    : 0;

                int newRank = i + 1;

                userPoints.PreviousRank = previousRank;
                userPoints.Rank = newRank;

                await _userPointsRepo.UpdateAsync(userPoints.Id, userPoints, ct);

                // 🔔 notification only when rank improves
                if (previousRank > 0 && newRank < previousRank)
                {
                    await _notification.SendRankAchievedNotificationAsync(
                        userPoints.UserId,
                        newRank,
                        previousRank,
                        ct);
                }
            }

            _logger.LogInformation(
                "Ranks recalculated for {Count} users",
                sortedUsers.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating ranks");
            return false;
        }
    }
}