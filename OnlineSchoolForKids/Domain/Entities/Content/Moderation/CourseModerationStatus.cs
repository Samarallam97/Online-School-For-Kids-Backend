using Domain.Enums.Content;
namespace Domain.Entities.Content.Moderation
{
    public class CourseModerationStatus
    {
        public ModerationStatus Status { get; set; } = ModerationStatus.Pending;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
        public string? RejectionReason { get; set; }
    }
}
