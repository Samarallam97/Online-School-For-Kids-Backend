namespace Domain.Entities.Content.Progress
{
    public class Bookmark : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;

        public string CourseId { get; set; } = string.Empty;

        public string LessonId { get; set; } = string.Empty;
    }
}
