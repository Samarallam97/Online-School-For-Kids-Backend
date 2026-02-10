using Domain.Entities;
using Domain.Enums;

namespace Application.Dtos
{
    public class CourseDto : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string InstructorId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public CourseLevel Level { get; set; }
        public string LevelDisplay { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal Rating { get; set; }
        public int TotalStudents { get; set; }
        public int DurationHours { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public bool IsInWishlist { get; set; }
        public bool IsInCart { get; set; }
    }
    public class UpdateCourseResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CourseDto? Course { get; set; }
    }
    public class AddToFavouriteDto
    {
        public string CourseId { get; set; }=  string.Empty;
    }
}

