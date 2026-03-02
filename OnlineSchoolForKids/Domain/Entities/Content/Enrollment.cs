using Domain.Entities.Users;

namespace Domain.Entities.Content
{

        public class Enrollment : BaseEntity
        {
            public string UserId { get; set; } = string.Empty;
            public string CourseId { get; set; } = string.Empty;
            public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
            public decimal Progress { get; set; } = 0; // 0-100
            public bool IsCompleted { get; set; } = false;
            public DateTime? CompletedAt { get; set; }
            public string? LastAccessedLessonId { get; set; }
            public DateTime? LastAccessedAt { get; set; }
            public User? User { get; set; }
            public Course? Course { get; set; }
        }
    }
    


