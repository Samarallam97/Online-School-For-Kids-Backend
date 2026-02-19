using Application.Dtos;
using Domain.Enums.Content;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Application.Commands
{
    public class CreateCourseCommand : IRequest<CourseDto>
    {
        public string CreatorId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public CourseLevel Level { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int DurationHours { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Language { get; set; } = "English";
        
    }


    public class UpdateCourseCommand : IRequest<UpdateCourseResponse>
    {
        public string CreatorId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string InstructorId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        public string CategoryId { get; set; } = string.Empty;

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

        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
    }


}

// Delete Course (Soft Delete)
public class DeleteCourseCommand : IRequest<bool>
{
    public string CreatorId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
}

