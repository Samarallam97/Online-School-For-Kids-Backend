using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Queries.Leaderboard
{
    // ── Query ──────────────────────────────────────────────────────────────────

    public class GetMyBadgesQuery : IRequest<List<BadgeDto>>
    {
        public string UserId { get; set; } = string.Empty;
    }

    // ── DTO ────────────────────────────────────────────────────────────────────

    public class BadgeRequirementDto
    {
        public string Type { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class BadgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public BadgeRequirementDto Requirement { get; set; } = new();
        public bool IsActive { get; set; }
        public bool IsEarned { get; set; }
    }

    // ── Handler ────────────────────────────────────────────────────────────────

    public class GetMyBadgesHandler : IRequestHandler<GetMyBadgesQuery, List<BadgeDto>>
    {
        private readonly IBadgeRepository _badgeRepo;
        private readonly IUserPointsRepository _userPointsRepo;

        public GetMyBadgesHandler(
            IBadgeRepository badgeRepo,
            IUserPointsRepository userPointsRepo)
        {
            _badgeRepo = badgeRepo;
            _userPointsRepo = userPointsRepo;
        }

        public async Task<List<BadgeDto>> Handle(GetMyBadgesQuery request, CancellationToken ct)
        {
            // 1. Get all active badges
            var allBadges = await _badgeRepo.GetAllAsync(b => b.IsActive, ct);

            // 2. Get user's earned badge IDs
            var userPoints = await _userPointsRepo.GetOneAsync(
                up => up.UserId == request.UserId, ct);

            var earnedIds = userPoints?.BadgesEarned ?? new List<string>();
            var earnedSet = new HashSet<string>(earnedIds);

            // 3. Map to DTOs with IsEarned flag
            return allBadges.Select(b => new BadgeDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Icon = b.Icon,
                Category = b.Category.ToString(),
                Requirement = new BadgeRequirementDto
                {
                    Type = b.Requirement.Type.ToString(),
                    Value = b.Requirement.Value,
                    Description = b.Requirement.Description
                },
                IsActive = b.IsActive,
                IsEarned = earnedSet.Contains(b.Id)
            }).ToList();
        }
    }
}