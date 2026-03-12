using Domain.Entities;
using Domain.Entities.Content.Progress;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;


namespace Application.Commands
{
    public class CreateCourseCommand : IRequest<CourseCreatorDto?>
    {
        public CreateCourseDto Dto { get; set; } = new();
        public string InstructorId { get; set; } = string.Empty;
    }

    public class CreateCourseHandler : IRequestHandler<CreateCourseCommand, CourseCreatorDto?>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<CreateCourseHandler> _logger;

        public CreateCourseHandler(
            ICourseRepository courseRepo,
            ILogger<CreateCourseHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<CourseCreatorDto?> Handle(CreateCourseCommand request, CancellationToken ct)
        {
            try
            {
                var dto = request.Dto;

                var course = new Domain.Entities.Content.Course
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId,
                    AgeGroup = Enum.Parse<AgeGroup>(dto.AgeGroup, true),
                    Price = dto.Price,
                    ThumbnailUrl = dto.ThumbnailUrl ?? "",
                    IsPublished = false,
                    Sections = new List<Section>()
                };

                await _courseRepo.CreateAsync(course, ct);

                _logger.LogInformation("Course created: {CourseId} - {Title}", course.Id, course.Title);

                return new CourseCreatorDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    ThumbnailUrl = course.ThumbnailUrl,
                    Price = course.Price,
                    IsPublished = course.IsPublished,
                    TotalSections = 0,
                    TotalLessons = 0,
                    TotalStudents = 0,
                    CreatedAt = course.CreatedAt,
                    UpdatedAt = course.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return null;
            }

        }
    
    }
    public class UpdateCourseCommand : IRequest<bool>
    {
        public string CourseId { get; set; } = string.Empty;
        public string InstructorId { get; set; } = string.Empty;
        public UpdateCourseDto Dto { get; set; } = new();
    }

    public class UpdateCourseHandler : IRequestHandler<UpdateCourseCommand, bool>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<UpdateCourseHandler> _logger;

        public UpdateCourseHandler(
            ICourseRepository courseRepo,
            ILogger<UpdateCourseHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateCourseCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                if (course == null) return false;

                var dto = request.Dto;

                course.Title = dto.Title;
                course.Description = dto.Description;
                course.CategoryId = dto.CategoryId;
                course.AgeGroup = dto.AgeGroup;
                course.Price = dto.Price;
                course.ThumbnailUrl = dto.ThumbnailUrl ?? course.ThumbnailUrl;
                course.IsPublished = dto.IsPublished;
                course.UpdatedAt = DateTime.UtcNow;

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Course updated: {CourseId}", course.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", request.CourseId);
                return false;
            }
        }
        public class DeleteCourseCommand : IRequest<bool>
        {
            public string CourseId { get; set; } = string.Empty;
            public string InstructorId { get; set; } = string.Empty;
        }

        public class DeleteCourseHandler : IRequestHandler<DeleteCourseCommand, bool>
        {
            private readonly ICourseRepository _courseRepo;
            private readonly ILogger<DeleteCourseHandler> _logger;

            public DeleteCourseHandler(
                ICourseRepository courseRepo,
                ILogger<DeleteCourseHandler> logger)
            {
                _courseRepo = courseRepo;
                _logger = logger;
            }

            public async Task<bool> Handle(DeleteCourseCommand request, CancellationToken ct)
            {
                try
                {
                    var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                    if (course == null) return false;

                    await _courseRepo.DeleteAsync(course.Id, ct);

                    _logger.LogInformation("Course deleted: {CourseId}", course.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting course {CourseId}", request.CourseId);
                    return false;
                }
            }
        }
        public class PublishCourseCommand : IRequest<bool>
        {
            public string CourseId { get; set; } = string.Empty;
            public string InstructorId { get; set; } = string.Empty;
            public bool Publish { get; set; } = true;
        }

        public class PublishCourseHandler : IRequestHandler<PublishCourseCommand, bool>
        {
            private readonly ICourseRepository _courseRepo;
            private readonly ILogger<PublishCourseHandler> _logger;

            public PublishCourseHandler(
                ICourseRepository courseRepo,
                ILogger<PublishCourseHandler> logger)
            {
                _courseRepo = courseRepo;
                _logger = logger;
            }

            public async Task<bool> Handle(PublishCourseCommand request, CancellationToken ct)
            {
                try
                {
                    var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                    if (course == null) return false;

                    course.IsPublished = request.Publish;
                    course.UpdatedAt = DateTime.UtcNow;

                    await _courseRepo.UpdateAsync(course.Id, course, ct);

                    _logger.LogInformation(
                        "Course {Action}: {CourseId}",
                        request.Publish ? "published" : "unpublished",
                        course.Id);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing course {CourseId}", request.CourseId);
                    return false;
                }
            }
        }
    }
    public class PublishCourseRequest
    {
        public bool Publish { get; set; }
    }

    public class CreateCourseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
       
        public string CategoryId { get; set; } = string.Empty;   
        public string AgeGroup { get; set; }
        public decimal Price { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}

public class CourseDto : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public AgeGroup AgeGroup { get; set; }
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

public class CourseCreatorDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsPublished { get; set; }
        public int TotalSections { get; set; }
        public int TotalLessons { get; set; }
        public int TotalStudents { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
public class UpdateCourseDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public AgeGroup AgeGroup { get; set; } 
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PreviewVideoUrl { get; set; }
    public bool IsPublished { get; set; }
}




