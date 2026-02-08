using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Error { get; }
    public List<string> Errors { get; }

    private Result(bool isSuccess, T? data, string? error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        Errors = errors ?? new List<string>();
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(List<string> errors) => new(false, default, null, errors);
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public record UserDto
{
    public string Id { get; init; }
    public string FullName { get; init; }
    public string Role { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public bool IsFirstLogin { get; init; }

}

// Base DTO
public class BaseProfileDto
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Phone { get; set; }
    public string Country { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Student Profile
public class StudentProfileDto : BaseProfileDto
{
    public string? LearningGoals { get; set; }
    public int EnrolledCourses { get; set; }
    public int Achievements { get; set; }
    public int TotalHoursLearned { get; set; }
}

// Parent Profile
public class ParentProfileDto : BaseProfileDto
{
    public int ChildrenCount { get; set; }
    public bool ParentalControlsActive { get; set; }
    public NotificationPreferences NotificationPreferences { get; set; }
    public List<PaymentMethodDto> PaymentMethods { get; set; }
}

// Content Creator Profile
public class ContentCreatorProfileDto : BaseProfileDto
{
    public bool IsVerifiedCreator { get; set; }
    public List<string> Expertise { get; set; }
    public int TotalCourses { get; set; }
    public int TotalStudents { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageRating { get; set; }
    public SocialLinks? SocialLinks { get; set; }
    public PayoutSettings? PayoutSettings { get; set; }
}

// Specialist Profile
public class SpecialistProfileDto : BaseProfileDto
{
    public string? ProfessionalTitle { get; set; }
    public List<string> Specializations { get; set; }
    public List<CertificationDto> Certifications { get; set; }
    public int YearsOfExperience { get; set; }
    public List<AvailabilitySlotDto> Availability { get; set; }
    public decimal HourlyRate { get; set; }
    public SessionRates? SessionRates { get; set; }
    public double Rating { get; set; }
    public int StudentsHelped { get; set; }
}

public class PaymentMethodDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "card", "vodafone_cash", "instapay", "fawry", "bank_account"
    public string DisplayInfo { get; set; } = string.Empty; // User-friendly display text
    public bool IsDefault { get; set; }

    // Legacy card fields (for backward compatibility)
    public string? Last4 { get; set; }
    public string? Brand { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
}

public class CertificationDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Issuer { get; set; }
    public int Year { get; set; }
    public string? DocumentUrl { get; set; }
}

public class AvailabilitySlotDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public List<string> TimeSlots { get; set; } = new();
}


public class UpdateProfileDto
{
    // Common fields
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? Bio { get; set; }

    // Student-specific
    public string? LearningGoals { get; set; }

    // Parent-specific
    public bool? ParentalControlsActive { get; set; }
    public NotificationPreferences? NotificationPreferences { get; set; }

    // Content Creator-specific
    public List<string>? Expertise { get; set; }
    public SocialLinks? SocialLinks { get; set; }

    // Specialist-specific
    public string? ProfessionalTitle { get; set; }
    public List<string>? Specializations { get; set; }
    public int? YearsOfExperience { get; set; }
    public decimal? HourlyRate { get; set; }
    public SessionRates? SessionRates { get; set; }
    public List<AvailabilitySlotDto>? Availability { get; set; }
}

public class ChildDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? Avatar { get; set; } // For backward compatibility
    public string? ProfilePictureUrl { get; set; }
    public int Courses { get; set; }
}

public class AddChildDto
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? Email { get; set; }
}

public class AddPaymentMethodDto
{
    public string Type { get; set; } = string.Empty; // "card", "vodafone_cash", "instapay", "fawry", "bank_account"

    // Card fields (legacy/international)
    public string? CardNumber { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? Cvc { get; set; }
    public string? CardholderName { get; set; }

    // Vodafone Cash
    public string? PhoneNumber { get; set; }

    // Instapay
    public string? InstapayId { get; set; }

    // Fawry
    public string? ReferenceNumber { get; set; }

    // Bank Account
    public string? AccountHolderName { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }
}