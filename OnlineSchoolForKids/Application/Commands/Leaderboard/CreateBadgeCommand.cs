using Domain.Entities.Content.Leaderboard;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Commands.Leaderboard
{

    // DTO
    public class CreateBadgeDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public BadgeCategory Category { get; set; }
        public BadgeRequirement Requirement { get; set; } = new();
    }

    // Command
    public class CreateBadgeCommand : IRequest<string>
    {
        public CreateBadgeDto Dto { get; set; } = new();
    }

    // Handler
    public class CreateBadgeCommandHandler : IRequestHandler<CreateBadgeCommand, string>
    {
        private readonly IBadgeRepository _badgeRepository;

        public CreateBadgeCommandHandler(IBadgeRepository badgeRepository)
        {
            _badgeRepository = badgeRepository;
        }

        public async Task<string> Handle(CreateBadgeCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            var badge = new Badge
            {
                Name = dto.Name,
                Description = dto.Description,
                Icon = dto.Icon,
                Category = dto.Category,
                Requirement = dto.Requirement,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _badgeRepository.CreateAsync(badge, cancellationToken);

            return badge.Id;
        }
    }
}