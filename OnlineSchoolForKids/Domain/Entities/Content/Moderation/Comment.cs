using Domain.Entities.Users;
namespace Domain.Entities.Content.Moderation
{
    public class Comment : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsFlagged { get; set; } = false;
        public bool IsApproved { get; set; } = true;
        public string? ParentCommentId { get; set; } // For replies
        public User? User { get; set; }
        public Course? Course { get; set; }
    }
}


