using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries.Content;

public class GetUserWishlistQuery : IRequest<GetUserWishlistResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public class WishlistCourseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string? InstructorAvatarUrl { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int AgeGroup { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public double Rating { get; set; }
    public int TotalStudents { get; set; }
    public int DurationHours { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? LastUpdated { get; set; }
    public string WishlistId { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public bool IsInCart { get; set; }
}

public class GetUserWishlistResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<WishlistCourseDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public class GetUserWishlistQueryHandler
    : IRequestHandler<GetUserWishlistQuery, GetUserWishlistResponse>
{
    private readonly IWishListRepository _wishRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICategoryRepository _categoryRepo;

    public GetUserWishlistQueryHandler(
        IWishListRepository wishRepo,
        ICourseRepository courseRepo,
        IUserRepository userRepo,
        ICategoryRepository categoryRepo)
    {
        _wishRepo     = wishRepo;
        _courseRepo   = courseRepo;
        _userRepo     = userRepo;
        _categoryRepo = categoryRepo;
    }

    public async Task<GetUserWishlistResponse> Handle(
        GetUserWishlistQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Fetch wishlist entries for this user
        var allEntries = await _wishRepo.GetAllAsync(
            w => w.UserId == request.UserId,
            cancellationToken);

        var ordered = allEntries
            .OrderByDescending(w => w.CreatedAt)
            .ToList();

        var totalCount = ordered.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var paged = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        if (paged.Count == 0)
            return new GetUserWishlistResponse
            {
                Success    = true,
                Message    = "Wishlist retrieved successfully",
                Items      = [],
                TotalCount = totalCount,
                TotalPages = totalPages,
                Page       = request.Page,
                PageSize   = request.PageSize,
            };

        // 2. Fetch courses in parallel (MongoDB has no auto-join)
        var courses = await Task.WhenAll(
            paged.Select(w => _courseRepo.GetByIdAsync(w.CourseId, cancellationToken)));

        // 3. Collect unique instructor and category IDs, fetch in parallel
        var instructorIds = courses
            .Where(c => c != null)
            .Select(c => c!.InstructorId)
            .Distinct()
            .ToList();

        var categoryIds = courses
            .Where(c => c != null)
            .Select(c => c!.CategoryId)
            .Distinct()
            .ToList();

        var instructorTasks = instructorIds.Select(id => _userRepo.GetByIdAsync(id, cancellationToken));
        var categoryTasks = categoryIds.Select(id => _categoryRepo.GetByIdAsync(id, cancellationToken));

        var instructors = (await Task.WhenAll(instructorTasks))
            .Where(u => u != null)
            .ToDictionary(u => u!.Id, u => u!);

        var categories = (await Task.WhenAll(categoryTasks))
            .Where(c => c != null)
            .ToDictionary(c => c!.Id, c => c!);

        // 4. Build DTOs
        var dtos = paged
            .Select((w, i) =>
            {
                var course = courses[i];
                if (course == null) return null;

                instructors.TryGetValue(course.InstructorId, out var instructor);
                categories.TryGetValue(course.CategoryId, out var category);

                return new WishlistCourseDto
                {
                    Id                  = course.Id,
                    Title               = course.Title,
                    Description         = course.Description,
                    InstructorId        = course.InstructorId,
                    InstructorName      = instructor?.FullName          ?? string.Empty,
                    InstructorAvatarUrl = instructor?.ProfilePictureUrl,
                    CategoryId          = course.CategoryId,
                    CategoryName        = category?.Name                ?? string.Empty,
                    AgeGroup            = (int)course.AgeGroup,
                    Price               = course.Price,
                    DiscountPrice       = course.DiscountPrice,
                    Rating              = (double)course.Rating,
                    TotalStudents       = course.TotalStudents,
                    DurationHours       = course.DurationHours,
                    ThumbnailUrl        = course.ThumbnailUrl,
                    Language            = course.Language,
                    LastUpdated         = course.UpdatedAt.HasValue
                                            ? course.UpdatedAt.Value.ToString("MMM yyyy")
                                            : null,
                    WishlistId          = w.Id,
                    AddedAt             = w.CreatedAt,
                    IsInCart            = false,
                };
            })
            .Where(d => d != null)
            .ToList();

        return new GetUserWishlistResponse
        {
            Success    = true,
            Message    = "Wishlist retrieved successfully",
            Items      = dtos!,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
