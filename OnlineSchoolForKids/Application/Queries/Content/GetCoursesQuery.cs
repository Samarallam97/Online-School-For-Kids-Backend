using Domain.Enums.Users;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using CourseEntity = Domain.Entities.Content.Course;
using MediatR;

namespace Application.Queries.Content;

public class GetCoursesQuery : IRequest<PagedResult<GetCoursesDto>>
{
    public string? CategoryId { get; set; }
    public int? AgeGroup { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinRating { get; set; }
    public string? Language { get; set; }
    public string? SearchQuery { get; set; }
    public string SortBy { get; set; } = "relevance";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? UserId { get; set; }
}

public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, PagedResult<GetCoursesDto>>
{
    private readonly ICourseRepository _courseRepo;
    private readonly IWishListRepository _wishRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICategoryRepository _categoryRepo;

    public GetCoursesQueryHandler(
        ICourseRepository courseRepo,
        IWishListRepository wishRepo,
        IUserRepository userRepo,
        ICategoryRepository categoryRepo)
    {
        _courseRepo   = courseRepo;
        _wishRepo     = wishRepo;
        _userRepo     = userRepo;
        _categoryRepo = categoryRepo;
    }

    public async Task<PagedResult<GetCoursesDto>> Handle(
        GetCoursesQuery request,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = (await _courseRepo.GetAllAsync()).AsQueryable();

        // ── Published only ────────────────────────────────────────────────────
        query = query.Where(c => c.IsPublished);

        // ── Filters ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.CategoryId))
            query = query.Where(c => c.CategoryId == request.CategoryId);

        if (request.AgeGroup.HasValue)
            query = query.Where(c => (int)c.AgeGroup == request.AgeGroup.Value);

        if (request.MinPrice.HasValue)
            query = query.Where(c => c.Price >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(c => c.Price <= request.MaxPrice.Value);

        if (request.MinRating.HasValue)
            query = query.Where(c => c.Rating >= request.MinRating.Value);

        if (!string.IsNullOrWhiteSpace(request.Language))
            query = query.Where(c => c.Language == request.Language);

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var term = request.SearchQuery.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Description.ToLower().Contains(term));
        }

        // ── Sorting ───────────────────────────────────────────────────────────
        query = request.SortBy.ToLower() switch
        {
            "price" => request.SortOrder == "asc"
                ? query.OrderBy(c => c.Price)
                : query.OrderByDescending(c => c.Price),
            "date" => request.SortOrder == "asc"
                ? query.OrderBy(c => c.CreatedAt)
                : query.OrderByDescending(c => c.CreatedAt),
            "rating" => request.SortOrder == "asc"
                ? query.OrderBy(c => c.Rating)
                : query.OrderByDescending(c => c.Rating),
            _ => query.OrderByDescending(c => c.IsFeatured)
                      .ThenByDescending(c => c.Rating),
        };

        // ── Pagination ────────────────────────────────────────────────────────
        var totalCount = query.Count();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(request.Page, 1, totalPages);

        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // ── Batch load instructors ────────────────────────────────────────────
        var instructorIds = items.Select(c => c.InstructorId).Distinct().ToList();
        var instructors = await Task.WhenAll(
            instructorIds.Select(id => _userRepo.GetByIdAsync(id, ct)));
        var instructorMap = instructors
            .Where(u => u != null)
            .ToDictionary(u => u!.Id, u => u!);

        // ── Batch load categories ─────────────────────────────────────────────
        var categoryIds = items.Select(c => c.CategoryId).Distinct().ToList();
        var cats = await Task.WhenAll(
            categoryIds.Select(id => _categoryRepo.GetByIdAsync(id, ct)));
        var categoryMap = cats
            .Where(c => c != null)
            .ToDictionary(c => c!.Id, c => c!);

        // ── Wishlist lookup ───────────────────────────────────────────────────
        var wishlistIds = new HashSet<string>();
        if (!string.IsNullOrEmpty(request.UserId))
        {
            var wishlists = await _wishRepo.GetAllAsync(w => w.UserId == request.UserId, ct);
            wishlistIds   = wishlists.Select(w => w.CourseId).ToHashSet();
        }

        // ── Map to DTOs ───────────────────────────────────────────────────────
        var dtos = items.Select(c =>
        {
            instructorMap.TryGetValue(c.InstructorId, out var instructor);
            categoryMap.TryGetValue(c.CategoryId, out var category);

            return new GetCoursesDto
            {
                Id                  = c.Id,
                Title               = c.Title,
                Description         = c.Description,

                InstructorId        = c.InstructorId,
                InstructorName      = instructor?.Role == UserRole.ContentCreator
                                        ? instructor.FullName
                                        : "Unknown",
                InstructorAvatarUrl = instructor?.ProfilePictureUrl,

                CategoryId          = c.CategoryId,
                CategoryName        = category?.Name ?? "Unknown",

                AgeGroup            = (int)c.AgeGroup,
                Price               = c.Price,
                DiscountPrice       = c.DiscountPrice,
                Rating              = c.Rating,
                TotalStudents       = c.TotalStudents,
                DurationHours       = c.DurationHours,
                ThumbnailUrl        = c.ThumbnailUrl       ?? string.Empty,
                Language            = c.Language           ?? string.Empty,
                LastUpdated         = (c.UpdatedAt ?? c.CreatedAt).ToString("MMMM yyyy"),
                IsInWishlist        = wishlistIds.Contains(c.Id),
                IsInCart            = false,
            };
        }).ToArray();

        return new PagedResult<GetCoursesDto>
        {
            Items      = dtos,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
        };
    }
}

// ── Shared result + DTO types ─────────────────────────────────────────────────

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class GetCoursesDto
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

    public decimal Rating { get; set; }
    public int TotalStudents { get; set; }
    public decimal DurationHours { get; set; }

    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? LastUpdated { get; set; } = string.Empty;

    public bool IsInWishlist { get; set; }
    public bool IsInCart { get; set; }
}