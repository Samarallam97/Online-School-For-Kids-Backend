using Domain.Entities.Content.Moderation;
using Domain.Entities.Content.Progress;
using Domain.Enums.Content;

namespace Domain.Entities.Content
{

    public class Course : BaseEntity
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string InstructorId { get; set; } = string.Empty;

        public string CategoryId { get; set; } = string.Empty;

        public CourseLevel Level { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal Rating { get; set; }
        public int TotalStudents { get; set; }
        public int DurationHours { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Language { get; set; } = "English";
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsVisible { get; set; } = true;
        public CourseModerationStatus? ModerationStatus { get; set; }

        // Navigation Properties
        public Category Category { get; set; } = null!;
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Section>? Sections { get; set; }
        public List<string> EnrolledStudentIds { get; set; } = new();
    }
}