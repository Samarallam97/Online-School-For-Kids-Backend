using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Dtos
{
    public class CreateCourseDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Level is required")]
        public CourseLevel Level { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10000")]
        public decimal Price { get; set; }

        [Range(0, 10000, ErrorMessage = "Discount price must be between 0 and 10000")]
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 500, ErrorMessage = "Duration must be between 1 and 500 hours")]
        public int DurationHours { get; set; }

        public string ThumbnailUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Language is required")]
        [StringLength(50)]
        public string Language { get; set; } = "English";
    }
}


