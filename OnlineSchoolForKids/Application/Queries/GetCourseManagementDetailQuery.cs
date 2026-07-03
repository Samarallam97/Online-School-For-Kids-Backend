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

            var sections = (course.Sections ?? new List<Section>())
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
                            VideoUrl = l.VideoUrl,
                            Transcript = l.Description,
                            HasQuiz = l.HasQuiz,
                            Quizzes = l.Quizzes.Select(q => new LessonQuizDto
                            {
                                Difficulty = q.Difficulty,
                                Questions = q.Questions.Select(qq => new LessonQuizQuestionResultDto
                                {
                                    Question = qq.Text,
                                    Options = qq.Options.OrderBy(o => o.Order).Select(o => o.Text).ToList(),
                                    CorrectAnswer = qq.CorrectAnswer,
                                    Explanation = qq.Explanation ?? string.Empty
                                }).ToList()
                            }).ToList(),
                            Materials = (l.Materials ?? new List<Material>())
                                .Select(m => new MaterialDto
                                {
                                    Id = m.Id,
                                    Title = m.Title,
                                    Type = m.Type,
                                    Url = m.Url,
                                    FileSize = m.FileSize
                                }).ToList()
                        }).ToList()
                }).ToList();

            return new CourseManagementDetailDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Subtitle = course.Subtitle,
                CategoryId = course.CategoryId,
                ThumbnailUrl = course.ThumbnailUrl,
                PreviewVideoUrl = course.PreviewVideoUrl,
                Price = course.Price,
                DiscountPrice = course.DiscountPrice,
                AgeGroup = course.AgeGroup.ToString(),
                Language = course.Language,
                WhatYoullLearn = course.WhatYoullLearn,
                Requirements = course.Requirements,
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
    public string? Subtitle { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? PreviewVideoUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public List<string> WhatYoullLearn { get; set; } = new();
    public List<string> Requirements { get; set; } = new();
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
    public string? VideoUrl { get; set; }
    /// <summary>Transcript text, stored on Lesson.Description.</summary>
    public string? Transcript { get; set; }
    public bool HasQuiz { get; set; }
    /// <summary>Full quiz content per difficulty — not just which levels exist,
    /// so the lesson editor can load existing questions for real editing.</summary>
    public List<LessonQuizDto> Quizzes { get; set; } = new();
    public List<MaterialDto> Materials { get; set; } = new();
}

public class LessonQuizDto
{
    public string Difficulty { get; set; } = string.Empty;
    public List<LessonQuizQuestionResultDto> Questions { get; set; } = new();
}

public class LessonQuizQuestionResultDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswer { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class MaterialDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long FileSize { get; set; }
}