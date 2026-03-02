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

            // Navigation
            public Course? Course { get; set; }
            public Section? Section { get; set; }
            public string? QuizId { get; set; }
        }
    }

