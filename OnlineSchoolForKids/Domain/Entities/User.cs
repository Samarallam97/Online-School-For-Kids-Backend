using Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }


    public string? PasswordHash { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public UserRole Role { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime DateOfBirth { get; set; }
    public string Country { get; set; }
    public string? Phone { get; set; }
    public string? Bio { get; set; }

    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
    public bool IsFirstLogin { get; set; } = true;
    public string? GoogleId { get; set; }
    public DateTime? LastLoginAt { get; set; }


    // creator , specialist register data
    public string? PortfolioUrl { get; set; }
    public string? CvLink { get; set; }


    // Parent , Student-specific fields
    public string? LearningGoals { get; set; }
    public List<string>? EnrolledCourseIds { get; set; } 
    public List<string>? AchievementIds { get; set; } 
    public int? TotalHoursLearned { get; set; } = 0;


    // Parent-specific fields
    public string? ParentId { get; set; }

    public List<string>? ChildrenIds { get; set; }

    public bool? ParentalControlsActive { get; set; } 
    public NotificationPreferences? NotificationPreferences { get; set; } 

    public List<PaymentMethod>? PaymentMethods { get; set; }


    // Content Creator specific fields
    public bool? IsVerifiedCreator { get; set; } 

    public List<string>? Expertise { get; set; } 

    public int? TotalStudents { get; set; } 

    public decimal? TotalRevenue { get; set; }

    public double? AverageRating { get; set; } 

    public List<string>? CreatedCourseIds { get; set; } 

    public SocialLinks? SocialLinks { get; set; }

    public PayoutSettings? PayoutSettings { get; set; }

    // Specialist specific fields
    public string? ProfessionalTitle { get; set; }

    public List<string>? Specializations { get; set; } 

    public List<Certification>? Certifications { get; set; } 

    public int? YearsOfExperience { get; set; }

    public List<AvailabilitySlot>? Availability { get; set; } 

    public decimal? HourlyRate { get; set; } 

    public SessionRates? SessionRates { get; set; }

    // Admin specific fields
    public bool? IsSuperAdmin { get; set; } 

    public bool? TwoFactorEnabled { get; set; } 

    public bool? LoginNotifications { get; set; } 

    public bool? SuspiciousActivityAlerts { get; set; } 

    public List<ActivityLogEntry>? ActivityLog { get; set; } 




}

public class NotificationPreferences
{
    public bool ProgressUpdates { get; set; } = true;

    public bool WeeklyReports { get; set; } = true;

    public bool AchievementAlerts { get; set; } = true;

    public bool PaymentReminders { get; set; } = true;
}


public class SocialLinks
{
    public string? Website { get; set; }

    public string? Twitter { get; set; }

    public string? LinkedIn { get; set; }

    public string? YouTube { get; set; }
}

public class PayoutSettings
{
    public string? BankAccountLast4 { get; set; }

    public string? BankName { get; set; }

    public bool IsVerified { get; set; } = false;

    public DateTime? NextPayoutDate { get; set; }

    public decimal NextPayoutAmount { get; set; } = 0;
}

public class Certification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public int Year { get; set; }

    public string? DocumentUrl { get; set; }
}

public class AvailabilitySlot
{
    public DayOfWeek DayOfWeek { get; set; }

    public List<string> TimeSlots { get; set; } = new();
}

public class SessionRates
{
    public decimal ThirtyMinSession { get; set; }
    public decimal SixtyMinSession { get; set; }

    public decimal PlatformFeePercentage { get; set; } = 15;
}

public class ActivityLogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Action { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
