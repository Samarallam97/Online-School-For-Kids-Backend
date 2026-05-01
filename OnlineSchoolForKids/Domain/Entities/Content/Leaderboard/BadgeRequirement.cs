using Domain.Enums.Content;

namespace Domain.Entities.Content.Leaderboard
{
    public class BadgeRequirement
    {
        public BadgeRequirementType Type { get; set; }
        public int Value { get; set; } // e.g., 5 courses, 30 day streak
        public string Description { get; set; } = string.Empty;
    }
}
