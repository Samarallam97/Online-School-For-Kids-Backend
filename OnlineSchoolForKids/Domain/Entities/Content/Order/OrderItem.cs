using Domain.Entities.Content;
using System.Text.Json.Serialization;

namespace Domain.Entities.Content.Order
{
    public class OrderItem
    {
        public string Id { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string CourseThumbnail { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int DiscountPercentage { get; set; }

        // Navigation (not stored in Redis)
        public Course? Course { get; set; }
    }
}
