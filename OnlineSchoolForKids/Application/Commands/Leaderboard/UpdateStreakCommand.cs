using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Leaderboard
{
    public class UpdateStreakCommand : IRequest<bool>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class UpdateStreakHandler : IRequestHandler<UpdateStreakCommand, bool>
    {
        private readonly IUserPointsRepository _userPointsRepo;
        private readonly ILogger<UpdateStreakHandler> _logger;

        public UpdateStreakHandler(
            IUserPointsRepository userPointsRepo,
            ILogger<UpdateStreakHandler> logger)
        {
            _userPointsRepo = userPointsRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateStreakCommand request, CancellationToken ct)
        {
            try
            {
                var userPoints = await _userPointsRepo.GetOneAsync(
                    up => up.UserId == request.UserId,
                    ct);

                if (userPoints == null) return false;

                var today = DateTime.UtcNow.Date;
                var lastActivity = userPoints.LastActivityDate.Date;

                // Check if user was active yesterday
                if ((today - lastActivity).Days == 1)
                {
                    // Continue streak
                    userPoints.CurrentStreak++;

                    if (userPoints.CurrentStreak > userPoints.LongestStreak)
                        userPoints.LongestStreak = userPoints.CurrentStreak;
                }
                else if ((today - lastActivity).Days > 1)
                {
                    // Streak broken
                    userPoints.CurrentStreak = 1;
                }

                userPoints.LastActivityDate = DateTime.UtcNow;
                await _userPointsRepo.UpdateAsync(userPoints.Id, userPoints, ct);

                _logger.LogInformation("Streak updated for User {UserId}: {Streak} days",
                    request.UserId, userPoints.CurrentStreak);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating streak");
                return false;
            }
        }
    }
}
