using Domain.Entities.Content.Quizes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities.Content.Progress
{
    /// <summary>
    /// Lesson - Represents a single lesson inside a course section
    /// </summary>
    [BsonIgnoreExtraElements]
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

        /// <summary>
        /// For lessons created by chunking a longer source video: the start/end
        /// offset (in seconds) within the shared VideoUrl that this lesson covers.
        /// Both 0 when the lesson owns its whole video (e.g. single-lesson mode).
        /// </summary>
        public int StartTimeSeconds { get; set; } = 0;
        public int EndTimeSeconds { get; set; } = 0;
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