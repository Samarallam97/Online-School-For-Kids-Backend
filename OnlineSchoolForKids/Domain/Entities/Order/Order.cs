using Domain.Enums;
namespace Domain.Entities.Order
{
    public class Order:BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public Domain.Enums.PaymentMethod PaymentMethod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        public string? PaymentIntentId { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation (not stored in Redis)
        public User? User { get; set; }
    }

}
