using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public class GetCourseFiltersQuery : IRequest<CourseFiltersDto> { }

public class CourseFiltersDto
{
    public List<string> Languages { get; set; } = new();
    public List<CategoryFilterDto> Categories { get; set; } = new();
    public List<string> AgeGroups { get; set; } = new();
}

public class CategoryFilterDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class GetCourseFiltersHandler : IRequestHandler<GetCourseFiltersQuery, CourseFiltersDto>
{
    private readonly ICourseRepository _courseRepo;
    private readonly ILogger<GetCourseFiltersHandler> _logger;

    public GetCourseFiltersHandler(
        ICourseRepository courseRepo,
        ILogger<GetCourseFiltersHandler> logger)
    {
        _courseRepo = courseRepo;
        _logger = logger;
    }

    public async Task<CourseFiltersDto> Handle(GetCourseFiltersQuery request, CancellationToken ct)
    {
        try
        {
            var courses = (await _courseRepo.GetAllAsync(c => c.IsPublished, ct)).ToList();

            var languages = courses
                .Where(c => !string.IsNullOrWhiteSpace(c.Language))
                .Select(c => c.Language)
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            var categories = courses
                .Where(c => c.Category != null)
                .GroupBy(c => c.CategoryId)
                .Select(g => new CategoryFilterDto
                {
                    Id = g.Key,
                    Name = g.First().Category?.Name ?? string.Empty
                })
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .OrderBy(c => c.Name)
                .ToList();

            var ageGroups = courses
                .Select(c => c.AgeGroup.ToString())
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            return new CourseFiltersDto
            {
                Languages = languages,
                Categories = categories,
                AgeGroups = ageGroups
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course filters");
            return new CourseFiltersDto();
        }
    }
}
