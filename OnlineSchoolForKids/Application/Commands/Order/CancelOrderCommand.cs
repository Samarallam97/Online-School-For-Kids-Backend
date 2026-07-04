using Domain.Interfaces.Repositories.Content;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

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
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CancelOrderCommandHandler(IOrderRepository orderRepository, IStringLocalizer<SharedResource> localizer)
        {
            _orderRepository = orderRepository;
            _localizer = localizer;
        }

        public async Task<CancelOrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

            if (order == null)
                return new CancelOrderResponse { Success = false, Message = _localizer["OrderNotFound"] };

            if (order.UserId != request.UserId)
                return new CancelOrderResponse { Success = false, Message = _localizer["Unauthorized"] };

            var cancelled = await _orderRepository.CancelOrderAsync(request.OrderId, cancellationToken);

            return new CancelOrderResponse
            {
                Success = cancelled,
                Message = cancelled ? _localizer["OrderCancelledSuccessfully"] : _localizer["CannotCancelOrder"]
            };
        }
    }

    public class CancelOrderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}