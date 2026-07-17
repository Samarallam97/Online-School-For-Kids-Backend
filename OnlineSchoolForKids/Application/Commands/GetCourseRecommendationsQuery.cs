using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

/// <summary>
/// Returns courses recommended based on category similarity.
/// Option 3 implementation: works immediately with real data.
/// When the AI model is retrained on real course data, swap the
/// implementation here to call the Python /recommend/content endpoint
/// instead — the frontend never changes.
/// </summary>
public class GetCourseRecommendationsQuery : IRequest<List<CourseRecommendationDto>>
{
    public string CourseId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int TopN { get; set; } = 5;
}

public class CourseRecommendationDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal Rating { get; set; }
    public int TotalStudents { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public class GetCourseRecommendationsHandler
    : IRequestHandler<GetCourseRecommendationsQuery, List<CourseRecommendationDto>>
{
    private readonly ICourseRepository _courseRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILogger<GetCourseRecommendationsHandler> _logger;

    public GetCourseRecommendationsHandler(
        ICourseRepository courseRepo,
        IUserRepository userRepo,
        ILogger<GetCourseRecommendationsHandler> logger)
    {
        _courseRepo = courseRepo;
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<List<CourseRecommendationDto>> Handle(
        GetCourseRecommendationsQuery request, CancellationToken ct)
    {
        try
        {
            var currentCourse = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (currentCourse == null) return new();

            var allPublished = (await _courseRepo.GetAllAsync(
                c => c.IsPublished && c.Id != request.CourseId, ct)).ToList();

            // Primary: same category. Secondary: same age group. Tertiary: highest rated.
            var recommendations = allPublished
                .OrderByDescending(c => c.CategoryId == currentCourse.CategoryId ? 2 :
                                        c.AgeGroup == currentCourse.AgeGroup ? 1 : 0)
                .ThenByDescending(c => c.Rating)
                .Take(request.TopN)
                .ToList();

            var result = new List<CourseRecommendationDto>();
            foreach (var course in recommendations)
            {
                var instructor = await _userRepo.GetByIdAsync(course.InstructorId, ct);
                result.Add(new CourseRecommendationDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    InstructorName = instructor?.FullName ?? "Unknown",
                    ThumbnailUrl = course.ThumbnailUrl,
                    Price = course.Price,
                    DiscountPrice = course.DiscountPrice,
                    Rating = course.Rating,
                    TotalStudents = course.TotalStudents,
                    CategoryName = course.Category?.Name ?? string.Empty,
                    Language = course.Language
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course recommendations for {CourseId}", request.CourseId);
            return new();
        }
    }
}
