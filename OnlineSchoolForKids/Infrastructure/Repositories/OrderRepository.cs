using Domain.Entities.Order;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Infrastructure.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(MongoDbContext context)
            : base(context.Orders)
        {
            CreateIndexes();
        }


        /// <summary>
        /// Create indexes for better performance
        /// </summary>
        private void CreateIndexes()
        {
            // Index on OrderNumber (unique)
            var orderNumberIndex = Builders<Order>.IndexKeys.Ascending(o => o.OrderNumber);
            _collection.Indexes.CreateOne(new CreateIndexModel<Order>(
                orderNumberIndex,
                new CreateIndexOptions { Unique = true }
            ));

            // Index on UserId
            var userIdIndex = Builders<Order>.IndexKeys.Ascending(o => o.UserId);
            _collection.Indexes.CreateOne(new CreateIndexModel<Order>(userIdIndex));

            // Index on Status
            var statusIndex = Builders<Order>.IndexKeys.Ascending(o => o.Status);
            _collection.Indexes.CreateOne(new CreateIndexModel<Order>(statusIndex));

            // Index on PaymentStatus
            var paymentStatusIndex = Builders<Order>.IndexKeys.Ascending(o => o.PaymentStatus);
            _collection.Indexes.CreateOne(new CreateIndexModel<Order>(paymentStatusIndex));

            // Compound index for UserId + Status
            var userStatusIndex = Builders<Order>.IndexKeys
                .Ascending(o => o.UserId)
                .Ascending(o => o.Status);
            _collection.Indexes.CreateOne(new CreateIndexModel<Order>(userStatusIndex));

            // Index on CreatedAt (descending for sorting)
            var createdAtIndex = Builders<Order>.IndexKeys.Descending(o => o.CreatedAt);
            _collection.Indexes.CreateOne(new CreateIndexModel<Order>(createdAtIndex));
        }

        // Generate unique order number
        private string GenerateOrderNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"ORD-{timestamp}-{random}";
        }

        public new async Task<Order> CreateAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            // Generate order number if not provided
            if (string.IsNullOrEmpty(order.OrderNumber))
            {
                order.OrderNumber = GenerateOrderNumber();
            }

            // Set timestamps
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.IsDeleted = false;

            await _collection.InsertOneAsync(order, cancellationToken: cancellationToken);
            return order;
        }

        public async Task<Order?> GetByOrderNumberAsync(
            string orderNumber,
            CancellationToken cancellationToken = default)
        {
            return await GetOneAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
              .Find(o => !o.IsDeleted && o.UserId == userId)
              .ToListAsync();

        }

        /// <summary>
        /// Get user orders with pagination
        /// </summary>
        public async Task<(IEnumerable<Order> orders, int totalCount)> GetUserOrdersPaginatedAsync(
            string userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var deletedFilter = Builders<Order>.Filter.Eq(o => o.IsDeleted, false);
            var userFilter = Builders<Order>.Filter.Eq(o => o.UserId, userId);
            var combinedFilter = Builders<Order>.Filter.And(deletedFilter, userFilter);

            var sortDefinition = Builders<Order>.Sort.Descending(o => o.CreatedAt);

            // Get total count
            var totalCount = (int)await _collection.CountDocumentsAsync(
                combinedFilter,
                cancellationToken: cancellationToken);

            // Get paginated results
            var orders = await _collection
                .Find(combinedFilter)
                .Sort(sortDefinition)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            return (orders, totalCount);
        }


        public async Task<bool> UpdateOrderStatusAsync(
            string id,
            OrderStatus status,
            CancellationToken cancellationToken = default)
        {
            var updateDefinition = Builders<Order>.Update
                .Set(o => o.Status, status)
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            // If status is completed, set completion date
            if (status == OrderStatus.Completed)
            {
                updateDefinition = updateDefinition.Set(o => o.CompletedAt, DateTime.UtcNow);
            }

            var result = await _collection.UpdateOneAsync(
                o => o.Id == id && !o.IsDeleted,
                updateDefinition,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }
        /// <summary>
        /// ///////////////
        /// </summary>
        public async Task<bool> UpdatePaymentStatusAsync(
            string id,
            PaymentStatus status,
            string? transactionId = null,
            CancellationToken cancellationToken = default)
        {
            var updateDefinition = Builders<Order>.Update
                .Set(o => o.PaymentStatus, status)
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            // Set transaction ID if provided
            if (!string.IsNullOrEmpty(transactionId))
            {
                updateDefinition = updateDefinition.Set(o => o.TransactionId, transactionId);
            }

            var result = await _collection.UpdateOneAsync(
                o => o.Id == id && !o.IsDeleted,
                updateDefinition,
                cancellationToken: cancellationToken);

            // If payment is successful, update order status to Processing
            if (result.ModifiedCount > 0 && status == PaymentStatus.Paid)
            {
                await UpdateOrderStatusAsync(id, OrderStatus.Processing, cancellationToken);
            }

            return result.ModifiedCount > 0;
        }

        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(
         OrderStatus status,
         CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(o => !o.IsDeleted && o.Status == status)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUserOrdersCountAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var count = await CountAsync(o => o.UserId == userId, cancellationToken);
            return (int)count;
        }

        public async Task<bool> CancelOrderAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            // Get the order first to check if it can be cancelled
            var order = await GetByIdAsync(id, cancellationToken);

            if (order == null)
                return false;

            // Cannot cancel completed or already cancelled orders
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                return false;

            // Update status to cancelled
            return await UpdateOrderStatusAsync(id, OrderStatus.Cancelled, cancellationToken);
        }
        public async Task<IEnumerable<Order>> GetOrdersByPaymentStatusAsync(
            PaymentStatus paymentStatus,
            CancellationToken cancellationToken = default)
        {
            var deletedFilter = Builders<Order>.Filter.Eq(o => o.IsDeleted, false);
            var paymentFilter = Builders<Order>.Filter.Eq(o => o.PaymentStatus, paymentStatus);
            var combinedFilter = Builders<Order>.Filter.And(deletedFilter, paymentFilter);

            var sortDefinition = Builders<Order>.Sort.Descending(o => o.CreatedAt);

            return await _collection
                .Find(combinedFilter)
                .Sort(sortDefinition)
                .ToListAsync(cancellationToken);
        }
        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(o => !o.IsDeleted
                          && o.CreatedAt >= startDate
                          && o.CreatedAt <= endDate)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        //retrieves the most recent orders for a specific user, limited by a number (count)
        public async Task<IEnumerable<Order>> GetUserRecentOrdersAsync(
            string userId,
            int count = 5,
            CancellationToken cancellationToken = default)
        {
            var deletedFilter = Builders<Order>.Filter.Eq(o => o.IsDeleted, false);
            var userFilter = Builders<Order>.Filter.Eq(o => o.UserId, userId);
            var combinedFilter = Builders<Order>.Filter.And(deletedFilter, userFilter);

            var sortDefinition = Builders<Order>.Sort.Descending(o => o.CreatedAt);

            return await _collection
                .Find(combinedFilter)
                .Sort(sortDefinition)
                .Limit(count)
                .ToListAsync(cancellationToken);
        }
        public async Task<decimal> GetUserTotalSpentAsync( string userId,CancellationToken cancellationToken = default)
        {
            var orders = await _collection
                .Find(o => !o.IsDeleted && o.UserId == userId && o.Status == OrderStatus.Completed)
                .ToListAsync(cancellationToken);

            return orders.Sum(o => o.Total);
        }

    }
}


