using Domain.Entities.Users;
namespace Domain.Entities.Content.Leaderboard
{
    public class UserPoints:BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
        public int TotalPoints { get; set; } = 0;
        public int WeeklyPoints { get; set; } = 0;
        public int MonthlyPoints { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0; // Days
        public int LongestStreak { get; set; } = 0;
        public DateTime LastActivityDate { get; set; } = DateTime.UtcNow;
        public int CoursesCompleted { get; set; } = 0;
        public List<string> BadgesEarned { get; set; } = new(); // Badge IDs
        public int Rank { get; set; } = 0;
        public int PreviousRank { get; set; } = 0;
        public User? User { get; set; }
    }
}

