using Domain.Entities;
using Domain.Entities.Content.Order;
using Domain.Enums.Content;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System.Data;

namespace Application.Commands.Order
{
    public class CreateOrderCommand : IRequest<CreateOrderResponse>
    {
        public string UserId { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartItemRepository _cartRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUserRepository _userRepo;
        private readonly IEnrollmentRepository _enrollmentRepository;

        public CreateOrderCommandHandler(
            IOrderRepository orderRepository,
            ICartItemRepository cartRepository,
           ICourseRepository courseRepository,IUserRepository userRepo,
            IEnrollmentRepository enrollmentRepository
            )
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _courseRepository = courseRepository;
            _userRepo = userRepo;
            _enrollmentRepository = enrollmentRepository;
        }

        public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            // Get cart items
            var cartItems = await _cartRepository.GetUserCartItemsAsync( request.UserId,cancellationToken);

            if (!cartItems.Any())
            {
                return new CreateOrderResponse
                {
                    Success = false,
                    Message = "Cart is empty"
                };
            }

            // Get course details
            var courseIds = cartItems.Select(ci => ci.CourseId).ToList();
            var courses = await _courseRepository.GetAllAsync(
                filter: c => courseIds.Contains(c.Id) && c.IsPublished,
                cancellationToken);

            var courseDict = courses.ToDictionary(c => c.Id);

            // Create order items
            var orderItems = new List<OrderItem>();
            decimal subtotal = 0;

            foreach (var cartItem in cartItems)
            {

                var course = courseDict[cartItem.CourseId];

                //Check if already enrolled
               var alreadyEnrolled = await _enrollmentRepository.ExistsAsync(
                   e => e.UserId == request.UserId && e.CourseId == cartItem.CourseId,
                   cancellationToken);

                if (alreadyEnrolled)
                    continue;

                var instructor = await _userRepo.GetByIdAsync(course.InstructorId);
                var instructorName = (instructor != null && instructor.Role == UserRole.ContentCreator)
                    ? instructor.FullName
                    : "Unknown";
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    CourseId = course.Id,
                    CourseTitle = course.Title,
                    CourseThumbnail = course.ThumbnailUrl,
                    InstructorName = instructorName,
                    Price = cartItem.Price,
                    OriginalPrice = course.DiscountPrice.HasValue ? course.Price : null,
                    DiscountPercentage = course.DiscountPrice.HasValue
                        ? (int)Math.Round((1 - (course.DiscountPrice.Value / course.Price)) * 100)
                        : 0
                };

                orderItems.Add(orderItem);
                subtotal += orderItem.Price;
            }

            if (!orderItems.Any())
            {
                return new CreateOrderResponse
                {
                    Success = false,
                    Message = "No valid items to order"
                };
            }

            // Calculate totals
            var tax = subtotal * 0.0m; // 0% tax, adjust as needed
            var total = subtotal + tax;

            // Create order
            var order = new Domain.Entities.Content.Order.Order
            {
                UserId = request.UserId,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = request.PaymentMethod,
                Subtotal = subtotal,
                Tax = tax,
                Total = total,
                Items = orderItems,
                Notes = request.Notes
            };

            var createdOrder = await _orderRepository.CreateAsync(order, cancellationToken);

            // Clear cart after order creation
            await _cartRepository.ClearUserCartAsync(request.UserId, cancellationToken);

            return new CreateOrderResponse
            {
                Success = true,
                Message = "Order created successfully",
                OrderId = createdOrder.Id,
                OrderNumber = createdOrder.OrderNumber,
                Total = createdOrder.Total
            };

        }
     
    } } 







public class CreateOrderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public decimal Total { get; set; }
    }

