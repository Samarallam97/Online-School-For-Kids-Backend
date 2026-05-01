using Domain.Entities.Users;
using Domain.Enums.Content;

namespace Domain.Entities.Content.Leaderboard
{
    public class PointTransaction : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public int Points { get; set; }
        public PointReason Reason { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? RelatedEntityId { get; set; } // Course/Lesson/Quiz ID
        public User? User { get; set; }
    }
}
