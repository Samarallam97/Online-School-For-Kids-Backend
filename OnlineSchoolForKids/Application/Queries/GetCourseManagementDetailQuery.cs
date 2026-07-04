using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;


namespace Application.Queries;

/// <summary>
/// Full course detail for the instructor-facing course management page
/// (Overview + Curriculum tabs). Unlike GetCourseByIdQuery (public,
/// published-only, student-facing), this works for unpublished/draft
/// courses and is scoped to the owning instructor.
/// </summary>
public class GetCourseManagementDetailQuery : IRequest<CourseManagementDetailDto?>
{
    public string CourseId { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
}

public class GetCourseManagementDetailHandler
    : IRequestHandler<GetCourseManagementDetailQuery, CourseManagementDetailDto?>
{
    private readonly ICourseRepository _courseRepo;
    private readonly ILogger<GetCourseManagementDetailHandler> _logger;

    public GetCourseManagementDetailHandler(
        ICourseRepository courseRepo,
        ILogger<GetCourseManagementDetailHandler> logger)
    {
        _courseRepo = courseRepo;
        _logger = logger;
    }

    public async Task<CourseManagementDetailDto?> Handle(
        GetCourseManagementDetailQuery request, CancellationToken ct)
    {
        try
        {
            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null || course.InstructorId != request.InstructorId)
                return null;

            var sections = (course.Sections ?? new List<Domain.Entities.Content.Progress.Section>())
                .OrderBy(s => s.Order)
                .Select(s => new ManagementSectionDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    Order = s.Order,
                    Lessons = (s.Lessons ?? new List<Lesson>())
                        .OrderBy(l => l.Order)
                        .Select(l => new ManagementLessonDto
                        {
                            Id = l.Id,
                            Title = l.Title,
                            Duration = l.Duration,
                            Order = l.Order,
                            IsFree = l.IsFree,
                            IsPublished = l.IsPublished,
                            HasVideo = !string.IsNullOrEmpty(l.VideoUrl),
                            MaterialsCount = l.Materials?.Count ?? 0,
                            HasQuiz = !string.IsNullOrEmpty(l.QuizId)
                        }).ToList()
                }).ToList();

            return new CourseManagementDetailDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                CategoryId = course.CategoryId,
                ThumbnailUrl = course.ThumbnailUrl,
                Price = course.Price,
                DiscountPrice = course.DiscountPrice,
                AgeGroup = course.AgeGroup.ToString(),
                Language = course.Language,
                IsPublished = course.IsPublished,
                Rating = course.Rating,
                TotalStudents = course.TotalStudents,
                TotalSections = sections.Count,
                TotalLessons = sections.Sum(s => s.Lessons.Count),
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                Sections = sections
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course management detail {CourseId}", request.CourseId);
            return null;
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────

public class CourseManagementDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public decimal Rating { get; set; }
    public int TotalStudents { get; set; }
    public int TotalSections { get; set; }
    public int TotalLessons { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ManagementSectionDto> Sections { get; set; } = new();
}

public class ManagementSectionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public List<ManagementLessonDto> Lessons { get; set; } = new();
}

public class ManagementLessonDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int Order { get; set; }
    public bool IsFree { get; set; }
    public bool IsPublished { get; set; }
    public bool HasVideo { get; set; }
    public int MaterialsCount { get; set; }
    public bool HasQuiz { get; set; }
}