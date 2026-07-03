using Domain.Entities.Content.Quizes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities.Content.Progress
{
    /// <summary>
    /// Lesson - Represents a single lesson inside a course section
    /// </summary>
    public class Lesson : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string CourseId { get; set; } = string.Empty;
        [BsonRepresentation(BsonType.ObjectId)]
        public string SectionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public int Duration { get; set; } = 0; // Seconds
        public int Order { get; set; } = 0;
        public bool IsPreview { get; set; } = false;
        public bool IsPublished { get; set; } = true;
        public bool IsFree { get; set; } = false;
        public ICollection<Material> Materials { get; set; } = new List<Material>();

        public List<LessonQuiz> Quizzes { get; set; } = new();

        // Navigation
        public Course? Course { get; set; }
        public Section? Section { get; set; }

        /// <summary>True once at least one difficulty level has questions.</summary>
        public bool HasQuiz => Quizzes.Count > 0;
    }
}