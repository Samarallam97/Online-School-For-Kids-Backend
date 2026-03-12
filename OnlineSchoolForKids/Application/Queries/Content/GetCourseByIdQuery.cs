using AutoMapper;
using Domain.Enums.Content;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using FluentValidation;
using MediatR;

namespace Application.Queries.Content;

public class GetCourseByIdQuery : IRequest<CourseDto>
{
    public string CourseId { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, CourseDto?>
{
    private readonly ICourseRepository _courseRepo;
    private readonly IWishListRepository _wishListRepo;
    private readonly ICartItemRepository _cartItemRepo;
    private readonly IUserRepository _userRepo;

    private const int RelatedCoursesCount = 4;

    public GetCourseByIdQueryHandler(
        ICourseRepository courseRepo,
        IWishListRepository wishListRepo,
        ICartItemRepository cartItemRepo,
        IUserRepository userRepo)
    {
        _courseRepo = courseRepo;
        _wishListRepo = wishListRepo;
        _cartItemRepo = cartItemRepo;
        _userRepo = userRepo;
    }

    public async Task<CourseDto?> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _courseRepo.GetByIdAsync(request.CourseId);

        if (course == null || !course.IsPublished)
            return null;

        // ── Wishlist / cart status ────────────────────────────────────────
        var isInWishlist = false;
        var isInCart = false;

        if (!string.IsNullOrEmpty(request.UserId))
        {
            var wishlist = await _wishListRepo.GetAllAsync(w =>
                w.UserId == request.UserId && w.CourseId == request.CourseId);
            isInWishlist = wishlist.Any();

            var cartItem = await _cartItemRepo.ExistsInCartAsync(request.UserId, request.CourseId);
            isInCart = cartItem != null;
        }

        // ── Instructor ────────────────────────────────────────────────────
        var instructor = await _userRepo.GetByIdAsync(course.InstructorId);
        var instructorName = instructor?.Role == UserRole.ContentCreator
            ? instructor.FullName
            : "Unknown";

        // Aggregate instructor stats across all their published courses
        var allCourses = (await _courseRepo.GetAllAsync()).ToList();
        var instructorCourses = allCourses
            .Where(c => c.InstructorId == course.InstructorId && c.IsPublished)
            .ToList();

        var instructorStudentsCount = instructorCourses.Sum(c => c.TotalStudents);
        var instructorRating = instructorCourses.Any()
            ? Math.Round(instructorCourses.Average(c => (double)c.Rating), 1)
            : 0;

        // ── Related courses (same category, excluding current) ────────────
        var relatedCourses = allCourses
            .Where(c => c.CategoryId == course.CategoryId
                     && c.Id != course.Id
                     && c.IsPublished)
            .OrderByDescending(c => c.Rating)
            .Take(RelatedCoursesCount)
            .ToList();

        var relatedCourseDtos = new List<RelatedCourseDto>();
        foreach (var rc in relatedCourses)
        {
            var rcInstructor = await _userRepo.GetByIdAsync(rc.InstructorId);
            relatedCourseDtos.Add(new RelatedCourseDto
            {
                Id = rc.Id,
                Title = rc.Title,
                Instructor = rcInstructor?.FullName ?? "Unknown",
                Thumbnail = rc.ThumbnailUrl,
                Rating = rc.Rating,
                Price = rc.DiscountPrice ?? rc.Price,
            });
        }

        // ── Sections → Modules ────────────────────────────────────────────
        // Lesson.Duration is stored in SECONDS
        var moduleDtos = course.Sections?
            .Where(s => s.IsPublished)
            .OrderBy(s => s.Order)
            .Select(s => new CourseModuleDto
            {
                Id = s.Id,
                Title = s.Title,
                Order = s.Order,
                LessonsCount = s.LessonsCount > 0
                    ? s.LessonsCount
                    : s.Lessons?.Count(l => l.IsPublished) ?? 0,
                Duration = FormatSeconds(
                    s.Lessons?.Where(l => l.IsPublished).Sum(l => l.Duration) ?? 0),
                Lessons = s.Lessons?
                    .Where(l => l.IsPublished)
                    .OrderBy(l => l.Order)
                    .Select(l => new CourseLessonDto
                    {
                        Id = l.Id,
                        Title = l.Title,
                        // IsPreview OR IsFree both mean the lesson is freely viewable
                        IsFree = l.IsFree || l.IsPreview,
                        Order = l.Order,
                        Duration = FormatSeconds(l.Duration),
                    }).ToList() ?? new(),
            }).ToList() ?? new();

        var totalLectures = moduleDtos.Sum(m => m.LessonsCount);

        // ── Assemble DTO ──────────────────────────────────────────────────
        return new CourseDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,

            InstructorId = course.InstructorId,
            InstructorName = instructorName,
            InstructorAvatarUrl = instructor?.ProfilePictureUrl,
            InstructorBio = instructor?.Bio ?? string.Empty,
            InstructorRating = (decimal)instructorRating,
            InstructorReviewsCount = 0,          // No Review entity yet
            InstructorStudentsCount = instructorStudentsCount,
            InstructorCoursesCount = instructorCourses.Count,

            CategoryId = course.CategoryId,
            CategoryName = course.Category?.Name ?? "Unknown",
            AgeGroup = course.AgeGroup,
            LevelDisplay = course.AgeGroup.ToString(),

            Price = course.Price,
            DiscountPrice = course.DiscountPrice,

            Rating = course.Rating,
            TotalStudents = course.TotalStudents,
            DurationHours = course.DurationHours,
            LecturesCount = totalLectures,

            ThumbnailUrl = course.ThumbnailUrl,
            Language = course.Language,
            IsFeatured = course.IsFeatured,
            IsInWishlist = isInWishlist,
            IsInCart = isInCart,
            LastUpdated = (course.UpdatedAt ?? course.CreatedAt).ToString("MMMM yyyy"),

            Modules = moduleDtos,
            RelatedCourses = relatedCourseDtos,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Formats seconds → "mm:ss" for lessons, "Xh Ym" for sections.</summary>
    private static string FormatSeconds(int totalSeconds)
    {
        if (totalSeconds <= 0) return "0:00";
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;

        if (hours > 0)
            return minutes > 0
                ? $"{hours}h {minutes}m"
                : $"{hours}h";

        return $"{minutes}:{seconds:D2}";
    }
}

public class GetCourseByIdQueryValidator : AbstractValidator<GetCourseByIdQuery>
{
    public GetCourseByIdQueryValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty()
            .WithMessage("Course ID is required");
    }
}

public class CourseLessonDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>Formatted as "mm:ss"</summary>
    public string Duration { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public int Order { get; set; }
}

public class CourseModuleDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>Formatted as "X hours Y mins"</summary>
    public string Duration { get; set; } = string.Empty;
    public int LessonsCount { get; set; }
    public int Order { get; set; }
    public List<CourseLessonDto> Lessons { get; set; } = new();
}

public class RelatedCourseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public decimal Price { get; set; }
}

// ── Main DTO ──────────────────────────────────────────────────────────────

public class CourseDto
{
    // ── Core ──────────────────────────────────────────────────────────────
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ── Instructor ────────────────────────────────────────────────────────
    public string InstructorId { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string? InstructorAvatarUrl { get; set; }
    public string InstructorBio { get; set; } = string.Empty;
    public decimal InstructorRating { get; set; }
    public int InstructorReviewsCount { get; set; }
    public int InstructorStudentsCount { get; set; }
    public int InstructorCoursesCount { get; set; }

    // ── Category / Classification ─────────────────────────────────────────
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public AgeGroup AgeGroup { get; set; }
    public string LevelDisplay { get; set; } = string.Empty;

    // ── Pricing ───────────────────────────────────────────────────────────
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }

    // ── Stats ─────────────────────────────────────────────────────────────
    public decimal Rating { get; set; }
    public int TotalStudents { get; set; }
    public int DurationHours { get; set; }
    public int LecturesCount { get; set; }

    // ── Meta ──────────────────────────────────────────────────────────────
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsInWishlist { get; set; }
    public bool IsInCart { get; set; }
    /// <summary>Formatted as "MMMM yyyy", e.g. "January 2024"</summary>
    public string LastUpdated { get; set; } = string.Empty;

    // ── Curriculum ────────────────────────────────────────────────────────
    public List<CourseModuleDto> Modules { get; set; } = new();

    // ── Related ───────────────────────────────────────────────────────────
    public List<RelatedCourseDto> RelatedCourses { get; set; } = new();
}
