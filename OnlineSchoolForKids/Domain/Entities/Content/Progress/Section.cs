namespace Domain.Entities.Content.Progress
{
    /// <summary>
    /// Section - Represents a course module that contains lessons
    /// </summary>
    public class Section : BaseEntity
    {
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; } = 0; 
        public bool IsPublished { get; set; } = true;
        // Optional Optimization
        public int LessonsCount { get; set; } = 0;
        public Course? Course { get; set; }
        public ICollection<Lesson>? Lessons { get; set; }
    }
}
