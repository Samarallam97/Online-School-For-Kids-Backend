using Application.Dtos;
using Application.Queries;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;
using System.Data;
using CourseEntity = Domain.Entities.Course;
namespace Application.Handlers.Course
{
    public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, PagedResult<CourseDto>>
    {
       
        private readonly IGenericRepository<CourseEntity> _courseRepo;
        private readonly IGenericRepository<Wishlist> _wishRepo;
        private readonly IGenericRepository<User> _userRepo;

        public GetCoursesQueryHandler(IGenericRepository<CourseEntity> courseRepository,IGenericRepository<Wishlist> wishRepo,IGenericRepository<User> userRepo)
        {
            _courseRepo = courseRepository;
            _wishRepo = wishRepo;
            _userRepo = userRepo;
        }

        public async Task<PagedResult<CourseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
        {
            var query = (await _courseRepo.GetAllAsync()).AsQueryable();

            // Apply filters

            if (!string.IsNullOrWhiteSpace(request.CategoryId))
                query = query.Where(c => c.CategoryId == request.CategoryId);

            if (request.Level.HasValue)
                query = query.Where(c => c.Level == request.Level.Value);

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
                var searchTerm = request.SearchQuery.ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(searchTerm) ||
                    c.Description.ToLower().Contains(searchTerm));
            }

            query = query.Where(c => c.IsPublished);

            // Apply sorting
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
                _ => query.OrderByDescending(c => c.IsFeatured).ThenByDescending(c => c.Rating)
            };

            var totalCount = query.Count();
            var items = query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Get wishlist and cart info if user is logged in
            var wishlistCourseIds = new HashSet<string>();
            var cartCourseIds = new HashSet<string>();

            if (!string.IsNullOrEmpty(request.UserId))
            {
                var wishlists = await _wishRepo.FindAsync(w => w.UserId == request.UserId);
                wishlistCourseIds = wishlists.Select(w => w.CourseId).ToHashSet();

                
            }

            var courseDtos = await Task.WhenAll(
            items.Select(c => MapToCourseDto(c, wishlistCourseIds, _userRepo))
);


            return new PagedResult<CourseDto>
            {
                Items = courseDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // ✅ ADD THIS METHOD
        private async Task<CourseDto> MapToCourseDto(CourseEntity course, HashSet<string> wishlistCourseIds,IGenericRepository<User> userRepository)
        {
            var instructor = await _userRepo.GetByIdAsync(course.InstructorId);
            var instructorName = (instructor != null && instructor.Role == UserRole.ContentCreator)
                ? instructor.FullName
                : "Unknown";

            return new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                InstructorName =  instructorName,
                InstructorId = course.InstructorId,
                CategoryName = course.Category?.Name ?? "Unknown",
                CategoryId = course.CategoryId,
                Level = course.Level,
                LevelDisplay = course.Level.ToString(),
                Price = course.Price,
                DiscountPrice = course.DiscountPrice,
                Rating = course.Rating,
                TotalStudents = course.TotalStudents,
                DurationHours = course.DurationHours,
                ThumbnailUrl = course.ThumbnailUrl,
                Language = course.Language,
                IsFeatured = course.IsFeatured,
                IsInWishlist = true,
                IsInCart = false
              
            };


        }

    
    }
}
