using Domain.Enums;
using Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;


namespace Application.Queries
{
    public class GetCartQuery : IRequest<CartDto>
    {
        public string UserId { get; set; }= string.Empty;
    }
    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
    {

        private const decimal TaxRate = 0.0m; // 0% tax, adjust as needed
        private readonly ICartItemRepository _cartItemRepo;
        private readonly IGenericRepository<Domain.Entities.Course> _courseRepo;
        private readonly IUserRepository _userRepo;

        public GetCartQueryHandler(ICartItemRepository cartItemRepo, IGenericRepository<Domain.Entities.Course> courseRepo,IUserRepository userRepo)
        {
            _cartItemRepo = cartItemRepo;
            _courseRepo = courseRepo;
            _userRepo = userRepo;
        }

        public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            // Get all cart items for user
            var cartItems = await _cartItemRepo.GetUserCartItemsAsync(request.UserId );

            if (!cartItems.Any())
            {
                return new CartDto
                {
                    Items = new List<CartItemDto>(),
                    ItemCount = 0,
                    Subtotal = 0,
                    Tax = 0,
                    Total = 0
                };
            }

            var courseIds = cartItems.Select(c => c.CourseId).ToList();

            // Get courses (only published)
            var courses = await _courseRepo.FindAsync(c =>
                courseIds.Contains(c.Id) && c.IsPublished);

            // Get instructors
            var instructorIds = courses
                .Select(c => c.InstructorId)
                .Distinct()
                .ToList();

            var instructors = await _userRepo.FindAsync(u =>
                instructorIds.Contains(u.Id) &&
                u.Role == UserRole.ContentCreator);

            var instructorDict = instructors
                .ToDictionary(i => i.Id, i => i.FullName);

            // Map to DTOs
            var items = cartItems
                .Join(courses,
                    ci => ci.CourseId,
                    c => c.Id,
                    (ci, c) => new CartItemDto
                    {
                        Id = ci.Id,
                        CourseId = c.Id,
                        CourseTitle = c.Title,
                        CourseThumbnail = c.ThumbnailUrl,
                        InstructorName = instructorDict.ContainsKey(c.InstructorId)
                            ? instructorDict[c.InstructorId]
                            : "Unknown",
                        Price = ci.Price,
                        OriginalPrice = c.DiscountPrice.HasValue ? c.Price : null,
                        DiscountPercentage = c.DiscountPrice.HasValue
                            ? (int)Math.Round((1 - (c.DiscountPrice.Value / c.Price)) * 100)
                            : 0,
                        Rating = c.Rating,
                        DurationHours = c.DurationHours,
                        AddedDate = ci.CreatedAt
                    })
                .OrderByDescending(i => i.AddedDate)
                .ToList();


            // Calculate totals
            var subtotal = items.Sum(i => i.Price);
            var tax = subtotal * TaxRate;
            var total = subtotal + tax;

            return new CartDto
            {
                Items = items,
                ItemCount = items.Count,
                Subtotal = subtotal,
                Tax = tax,
                Total = total
            };
        }
    }

    public class GetCartQueryValidator : AbstractValidator<GetCartQuery>
    {
        public GetCartQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");
        }
    }
    public class CartDto
    {
        public IEnumerable<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public int ItemCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }
    public class CartItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string CourseThumbnail { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int DiscountPercentage { get; set; }
        public decimal Rating { get; set; }
        public int DurationHours { get; set; }
        public DateTime AddedDate { get; set; }
    }

}
