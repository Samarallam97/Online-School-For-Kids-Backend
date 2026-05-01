using Domain.Enums.Content;

namespace Domain.Entities.Content.Leaderboard
{
    public class Badge : BaseEntity
    {
       public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty; // URL or emoji
        public BadgeCategory Category { get; set; }
        public BadgeRequirement Requirement { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }
}
