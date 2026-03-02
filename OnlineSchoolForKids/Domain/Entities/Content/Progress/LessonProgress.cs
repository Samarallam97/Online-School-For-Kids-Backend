using Domain.Entities.Users;
namespace Domain.Entities.Content.Progress
{
    /// <summary>
    /// Lesson Progress - Tracks individual lesson completion
    /// </summary>
    public class LessonProgress : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        public int TimeSpent { get; set; } = 0; // Seconds
        public int VideoPosition { get; set; } = 0; // Seconds
        public decimal WatchedPercentage { get; set; } = 0;
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
        public Lesson? Lesson { get; set; }
    }
}
