using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

// <summary>
/// Returns the courses owned by the requesting instructor, regardless of
/// publish status. Used for instructor-facing pages (My Courses grid,
/// course pickers), as opposed to GetCoursesQuery which is the public,
/// published-only browsing list.
/// </summary>
public class GetMyCoursesQuery : IRequest<List<MyCourseDto>>
{
    public string InstructorId { get; set; } = string.Empty;

    /// <summary>Optional case-insensitive title search.</summary>
    public string? SearchQuery { get; set; }

    /// <summary>"draft" | "published" | null (= all)</summary>
    public string? StatusFilter { get; set; }
}

public class GetMyCoursesHandler : IRequestHandler<GetMyCoursesQuery, List<MyCourseDto>>
{
    private readonly ICourseRepository _courseRepo;
    private readonly ILogger<GetMyCoursesHandler> _logger;

    public GetMyCoursesHandler(
        ICourseRepository courseRepo,
        ILogger<GetMyCoursesHandler> logger)
    {
        _courseRepo = courseRepo;
        _logger = logger;
    }

    public async Task<List<MyCourseDto>> Handle(GetMyCoursesQuery request, CancellationToken ct)
    {
        try
        {
            var courses = await _courseRepo.GetAllAsync(
                c => c.InstructorId == request.InstructorId,
                ct);

            var query = courses.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.SearchQuery))
            {
                var term = request.SearchQuery.Trim().ToLowerInvariant();
                query = query.Where(c => c.Title.ToLowerInvariant().Contains(term));
            }

            if (request.StatusFilter == "draft")
                query = query.Where(c => !c.IsPublished);
            else if (request.StatusFilter == "published")
                query = query.Where(c => c.IsPublished);

            return query
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .Select(c => new MyCourseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    ThumbnailUrl = c.ThumbnailUrl,
                    IsPublished = c.IsPublished,
                    Price = c.Price,
                    TotalSections = c.Sections?.Count ?? 0,
                    TotalLessons = c.Sections?.Sum(s => s.Lessons?.Count ?? 0) ?? 0,
                    TotalStudents = c.TotalStudents,
                    Rating = c.Rating,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting courses for instructor {InstructorId}", request.InstructorId);
            return new List<MyCourseDto>();
        }
    }
}

public class MyCourseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public decimal Price { get; set; }
    public int TotalSections { get; set; }
    public int TotalLessons { get; set; }
    public int TotalStudents { get; set; }
    public decimal Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}