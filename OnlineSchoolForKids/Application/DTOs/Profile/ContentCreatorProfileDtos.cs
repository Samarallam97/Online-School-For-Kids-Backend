using Domain.Entities;


namespace Application.DTOs.Profile;


public class ContentCreatorProfileDto : BaseProfileDto
{
    public bool IsVerifiedCreator { get; set; }
    public List<string> ExpertiseTags { get; set; }
    public int TotalCourses { get; set; }
    public int TotalStudents { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageRating { get; set; }
}

public class WorkExperienceDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty; // Format: "YYYY-MM"
    public string? EndDate { get; set; } // Format: "YYYY-MM" or null if current
    public bool IsCurrentRole { get; set; }
}

public class PayoutDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty; // pending, processing, completed, failed
    public string PaymentMethodId { get; set; } = string.Empty;
    public string ScheduledDate { get; set; } = string.Empty;
    public string? ProcessedDate { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class PayoutsResponseDto
{
    public List<PayoutDto> Payouts { get; set; } = new();
    public int Total { get; set; }
    public PayoutDto? NextPayout { get; set; }
}

