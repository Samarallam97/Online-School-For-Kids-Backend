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

        // -- Live session fields --------------------------------------------------
        /// <summary>True = this lesson is a live session, not a recorded video.</summary>
        public bool IsLive { get; set; } = false;

        /// <summary>
        /// Points to the LiveSession document that holds all live-session state
        /// (status, channel name, whiteboard URL, viewer count, etc).
        /// Null until the instructor schedules the live session for this lesson.
        /// We do NOT duplicate live-session fields here -- LiveSession is the
        /// single source of truth, looked up via this id.
        /// </summary>
        public string? LiveSessionId { get; set; }

        // -- Existing fields --------------------------------------------------
        public ICollection<Material> Materials { get; set; } = new List<Material>();

        // Navigation
        public Course? Course { get; set; }
        public Section? Section { get; set; }
        public string? QuizId { get; set; }
        public int StartTimeSeconds { get; set; }

        public int EndTimeSeconds { get; set; }

        public bool HasQuiz { get; set; }

        public List<LessonQuiz> Quizzes { get; set; } = new();
    }
}


