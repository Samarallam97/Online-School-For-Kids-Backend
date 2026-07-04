using Domain.Entities.Content;
using Domain.Entities.Content.Orders;
using Domain.Enums.Content;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;
using System.Data;

namespace Application.Commands.Orders;

public class CreateOrderCommand : IRequest<CreateOrderResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
    public List<string> CourseIds { get; set; } = new();
    public string? Notes { get; set; }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartItemRepository _cartRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentProcessorFactory _processorFactory;
    private readonly ICouponRepository _couponRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateOrderCommandHandler(IOrderRepository orderRepository, ICartItemRepository cartRepository, ICourseRepository courseRepository, IUserRepository userRepository, IEnrollmentRepository enrollmentRepository, IPaymentRepository paymentRepository, IPaymentProcessorFactory processorFactory, ICouponRepository couponRepository, IStringLocalizer<SharedResource> localizer)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _courseRepository = courseRepository;
        _userRepository = userRepository;
        _enrollmentRepository = enrollmentRepository;
        _paymentRepository = paymentRepository;
        _processorFactory = processorFactory;
        _couponRepository = couponRepository;
        _localizer = localizer;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1. Resolve payment method from user
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user is null)
            return Fail(_localizer["UserNotFound"]);

        var method = string.IsNullOrEmpty(request.PaymentMethodId)
            ? user.PaymentMethods?.FirstOrDefault(m => m.IsDefault) ?? user.PaymentMethods?.FirstOrDefault()
            : user.PaymentMethods?.FirstOrDefault(m => m.Id == request.PaymentMethodId);

        if (method is null)
            return Fail(_localizer["PaymentMethodNotFound"]);

        // 2. Get cart items (fall back to explicit courseIds if provided)
        var cartItems = request.CourseIds.Any()
            ? request.CourseIds.Select(id => new CartItem { CourseId = id }).ToList()
            : (await _cartRepository.GetUserCartItemsAsync(request.UserId, ct)).ToList();

        if (!cartItems.Any())
            return Fail(_localizer["CartIsEmpty"]);

        // 3. Load courses
        var courseIds = cartItems.Select(c => c.CourseId).ToList();
        var courses = await _courseRepository.GetAllAsync(
            filter: c => courseIds.Contains(c.Id) && c.IsPublished, ct);
        var courseDict = courses.ToDictionary(c => c.Id);

        // 4. Build order items, skip already enrolled
        var orderItems = new List<OrderItem>();
        decimal subtotal = 0;

        foreach (var cartItem in cartItems)
        {
            if (!courseDict.TryGetValue(cartItem.CourseId, out var course)) continue;

            var alreadyEnrolled = await _enrollmentRepository.ExistsAsync(
                e => e.UserId == request.UserId && e.CourseId == cartItem.CourseId, ct);
            if (alreadyEnrolled) continue;

            var price = course.DiscountPrice ?? course.Price;
            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid().ToString(),
                CourseId = course.Id,
                CourseTitle = course.Title,
                CourseThumbnail = course.ThumbnailUrl,
                Price = price,
                OriginalPrice = course.DiscountPrice.HasValue ? course.Price : null,
            });
            subtotal += price;
        }

        if (!orderItems.Any())
            return Fail(_localizer["NoValidItems"]);

        // 5. Apply coupon
        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, ct);
            if (coupon is null || !coupon.IsActive || coupon.ExpiresAt < DateTime.UtcNow)
                return Fail(_localizer["InvalidOrExpiredCoupon"]);

            discount = coupon.DiscountType == "percentage"
                ? subtotal * (coupon.Value / 100m)
                : coupon.Value;
            discount = Math.Min(discount, subtotal);
        }

        var total = subtotal - discount;

        // 6. Create order
        var methodType = MapMethodType(method.Type);
        var order = new Domain.Entities.Content.Orders.Order
        {
            UserId = request.UserId,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = method.Type.ToString(),
            PaymentMethodId = method.Id,
            CouponCode = request.CouponCode,
            Subtotal = subtotal,
            Discount = discount,
            Tax = 0,
            Total = total,
            Items = orderItems,
            Notes = request.Notes
        };
        var createdOrder = await _orderRepository.CreateAsync(order, ct);

        // 7. Create pending payment record
        var payment = new Payment
        {
            OrderId = createdOrder.Id,
            UserId = request.UserId,
            Amount = total,
            Currency = "EGP",
            MethodType = methodType,
            PaymentMethodId = method.Id,
            Status = PaymentStatus.Pending,
        };
        payment = await _paymentRepository.CreateAsync(payment, ct);

        // 8. Process payment
        var processor = _processorFactory.Resolve(methodType);
        var result = await processor.ProcessAsync(new ProcessorContext
        {
            OrderId = createdOrder.Id,
            UserId = request.UserId,
            Amount = total,
            Method = method
        }, ct);

        // 9. Update records based on result
        payment.UpdatedAt = DateTime.UtcNow;

        if (result.Success)
        {
            payment.Status = PaymentStatus.Paid;
            payment.TransactionId = result.TransactionId;
            payment.ProviderReferenceNumber = result.ReferenceNumber;
            payment.PaidAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment.Id, payment, ct);
            await _orderRepository.UpdateOrderStatusAsync(createdOrder.Id, OrderStatus.Completed, ct);
            await _orderRepository.UpdatePaymentStatusAsync(createdOrder.Id, PaymentStatus.Paid, result.TransactionId, ct);

            // 10. Enrol user
            foreach (var item in orderItems)
            {
                var exists = await _enrollmentRepository.ExistsAsync(
                    e => e.UserId == request.UserId && e.CourseId == item.CourseId, ct);

                if (!exists)
                    await _enrollmentRepository.CreateAsync(new Enrollment
                    {
                        UserId = request.UserId,
                        CourseId = item.CourseId,
                        Progress = 0,
                        IsCompleted = false,
                        CreatedAt = DateTime.UtcNow
                    }, ct);
            }

            // 11. Clear cart
            await _cartRepository.ClearUserCartAsync(request.UserId, ct);

            return new CreateOrderResponse
            {
                Success = true,
                Message = _localizer["OrderPlacedSuccessfully"],
                OrderId = createdOrder.Id,
                OrderNumber = createdOrder.OrderNumber,
                Total = total
            };
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = result.FailureReason;
            payment.FailedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment.Id, payment, ct);
            await _orderRepository.UpdateOrderStatusAsync(createdOrder.Id, OrderStatus.Failed, ct);
            await _orderRepository.UpdatePaymentStatusAsync(createdOrder.Id, PaymentStatus.Failed, null, ct);

            return Fail(result.FailureReason ?? _localizer["PaymentFailed"]);
        }
    }

    private static CreateOrderResponse Fail(string msg) =>
        new() { Success = false, Message = msg };

    private static PaymentMethodType MapMethodType(Domain.Enums.Users.PaymentMethodType t) => t switch
    {
        Domain.Enums.Users.PaymentMethodType.Card => PaymentMethodType.Card,
        Domain.Enums.Users.PaymentMethodType.VodafoneCash => PaymentMethodType.VodafoneCash,
        Domain.Enums.Users.PaymentMethodType.Instapay => PaymentMethodType.Instapay,
        Domain.Enums.Users.PaymentMethodType.Fawry => PaymentMethodType.Fawry,
        Domain.Enums.Users.PaymentMethodType.BankAccount => PaymentMethodType.BankAccount,
        _ => throw new ArgumentOutOfRangeException(nameof(t))
    };
}
public class CreateOrderRequest
{
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
    public List<string> CourseIds { get; set; } = new();
    public string? Notes { get; set; }
}
public class CreateOrderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public decimal Total { get; set; }
}
