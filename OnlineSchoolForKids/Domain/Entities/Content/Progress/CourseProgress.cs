using Domain.Entities.Users;

namespace Domain.Entities.Content.Progress
{
    public class CourseProgress : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string EnrollmentId { get; set; } = string.Empty;
        public int CompletedLessons { get; set; } = 0;
        public int TotalLessons { get; set; } = 0;
        public decimal ProgressPercentage { get; set; } = 0;
        public int TimeSpent { get; set; } = 0; // Total seconds
        public decimal? AverageQuizScore { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public User? User { get; set; }
        public Course? Course { get; set; }
    }

   

}
