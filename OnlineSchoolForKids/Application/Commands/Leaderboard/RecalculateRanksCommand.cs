using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Leaderboard
{
    public class RecalculateRanksCommand : IRequest<bool>
    {
    }

    public class RecalculateRanksHandler : IRequestHandler<RecalculateRanksCommand, bool>
    {
        private readonly IUserPointsRepository _userPointsRepo;
        private readonly ILogger<RecalculateRanksHandler> _logger;

        public RecalculateRanksHandler(
            IUserPointsRepository userPointsRepo,
            ILogger<RecalculateRanksHandler> logger)
        {
            _userPointsRepo = userPointsRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(RecalculateRanksCommand request, CancellationToken ct)
        {
            try
            {
                var allUsers = await _userPointsRepo.GetAllAsync(_ => true, ct);
                var sortedUsers = allUsers.OrderByDescending(u => u.TotalPoints).ToList();

                for (int i = 0; i < sortedUsers.Count; i++)
                {
                    var user = sortedUsers[i];
                    user.PreviousRank = user.Rank;
                    user.Rank = i + 1;
                    await _userPointsRepo.UpdateAsync(user.Id, user, ct);
                }

                _logger.LogInformation("Ranks recalculated for {Count} users", sortedUsers.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating ranks");
                return false;
            }
        }
    }
}


