using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Commands.Order
{
    public class CancelOrderCommand : IRequest<CancelOrderResponse>
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, CancelOrderResponse>
    {
        private readonly IOrderRepository _orderRepository;

        public CancelOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<CancelOrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

            if (order == null)
                return new CancelOrderResponse { Success = false, Message = "Order not found" };

            if (order.UserId != request.UserId)
                return new CancelOrderResponse { Success = false, Message = "Unauthorized" };

            var cancelled = await _orderRepository.CancelOrderAsync(request.OrderId, cancellationToken);

            return new CancelOrderResponse
            {
                Success = cancelled,
                Message = cancelled ? "Order cancelled successfully" : "Cannot cancel this order"
            };
        }
    }

    public class CancelOrderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}