using Domain.Entities;
using Domain.Entities.Content;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;

namespace Application.Commands.Course
{
    public class AddToCartCommand : IRequest<AddToCartResponse>
    {
        public string CourseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class AddToCartResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CartItemId { get; set; }  //course
    }


    public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, AddToCartResponse>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ICartItemRepository _cartItemRepo;

        public AddToCartCommandHandler(
            ICourseRepository courseRepo, ICartItemRepository cartItemRepository)
        {
            _courseRepo = courseRepo;
            _cartItemRepo = cartItemRepository;
        }

        public async Task<AddToCartResponse> Handle(AddToCartCommand request, CancellationToken cancellationToken)
        {
            // Check if course exists and is published
            var course = await _courseRepo.GetByIdAsync(request.CourseId);

            if (course == null || !course.IsPublished)
            {
                return new AddToCartResponse
                {
                    Success = false,
                    Message = "Course not found or not available for purchase"
                };
            }
            // Check if user already enrolled in this course
            //var enrollments = await _unitOfWork.Enrollments.FindAsync(e =>
            //    e.UserId == request.UserId && e.CourseId == request.CourseId);

            //if (enrollments.Any())
            //{
            //    return new AddToCartResponse
            //    {
            //        Success = false,
            //        Message = "You are already enrolled in this course"
            //    };
            //}

            //Check if already in cart
            var existingCartItem = await _cartItemRepo.ExistsInCartAsync(request.UserId, request.CourseId);

            if (existingCartItem)
            {
                return new AddToCartResponse
                {
                    Success = false,
                    Message = "Course already in cart",
                    
                };
            }

            // Get current price (use discount price if available)
            var price = course.DiscountPrice ?? course.Price;

            //Create new cart item
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                CourseId = request.CourseId,
                Price = price,
                CreatedAt = DateTime.UtcNow
            };

            await _cartItemRepo.CreateAsync(cartItem);

            return new AddToCartResponse
            {
                Success = true,
                Message = "Course added to cart successfully",
                CartItemId = cartItem.Id
            };
        }    
        
        public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
        {
            public AddToCartCommandValidator()
            {
                RuleFor(x => x.CourseId)
                    .NotEmpty()
                    .WithMessage("Course ID is required");

                RuleFor(x => x.UserId)
                    .NotEmpty()
                    .WithMessage("User ID is required");
            }
        }
    }
}