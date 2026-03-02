namespace Domain.Entities.Content.Progress
{
    /// <summary>
    /// Section Progress - Groups lessons by section
    /// </summary>
    public class SectionProgress : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public int CompletedLessons { get; set; } = 0;
        public int TotalLessons { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
    }
}
