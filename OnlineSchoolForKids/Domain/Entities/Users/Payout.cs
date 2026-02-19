using Domain.Enums.Users;

namespace Domain.Entities.Users;

public class Payout : BaseEntity
{
    public string CreatorId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string PaymentMethodId { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Month { get; set; } = string.Empty; // e.g., "January"
    public int Year { get; set; }
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
}


