using Domain.Entities.Content.Order;
using Domain.Enums.Content;

namespace Domain.Interfaces.Repositories.Content
{
    public interface IOrderRepository:IGenericRepository<Order>
    {
        Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

        Task<IEnumerable<Order>> GetUserOrdersAsync(string userId, CancellationToken cancellationToken = default);


        Task<(IEnumerable<Order> orders, int totalCount)> GetUserOrdersPaginatedAsync(
            string userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);


        Task<IEnumerable<Order>> GetOrdersByStatusAsync(
            OrderStatus status,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Order>> GetOrdersByPaymentStatusAsync(
            PaymentStatus paymentStatus,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

  
        Task<IEnumerable<Order>> GetUserRecentOrdersAsync(
            string userId,
            int count = 5,
            CancellationToken cancellationToken = default);



        Task<bool> UpdateOrderStatusAsync(
            string id,
            OrderStatus status,
            CancellationToken cancellationToken = default);

        Task<bool> UpdatePaymentStatusAsync(
            string id,
            PaymentStatus status,
            string? transactionId = null,
            CancellationToken cancellationToken = default);


        Task<bool> CancelOrderAsync(string id, CancellationToken cancellationToken = default);

        Task<int> GetUserOrdersCountAsync(string userId, CancellationToken cancellationToken = default);

        Task<decimal> GetUserTotalSpentAsync(string userId, CancellationToken cancellationToken = default);


    }
}


