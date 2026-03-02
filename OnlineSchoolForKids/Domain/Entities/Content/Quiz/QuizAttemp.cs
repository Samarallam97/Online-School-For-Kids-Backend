using Domain.Entities.Users;
using Domain.Enums.Content;
namespace Domain.Entities.Content.Quiz
{
    public class QuizAttempt : BaseEntity
    {
        public string QuizId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public int AttemptNumber { get; set; } = 1;
        public QuizAttemptStatus Status { get; set; } = QuizAttemptStatus.InProgress;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<QuizAnswer> Answers { get; set; } = new();
        public decimal? Score { get; set; } // Percentage
        public int? TotalPoints { get; set; }
        public int? EarnedPoints { get; set; }
        public bool? Passed { get; set; }
        public int? TimeSpent { get; set; } // Seconds
        public Quiz? Quiz { get; set; }
        public User? User { get; set; }
    }



    
}


