using Application.Commands.Profile.Users;
using Application.Queries.Profile.Specialists;
using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs;

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

    // 2FA fields
    public bool Requires2FA { get; set; } = false;
    public string? TempToken { get; set; }
}

public record UserDto
{
    public string Id { get; init; }
    public string FullName { get; init; }
    public string Role { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public bool IsFirstLogin { get; init; }

}


public class StudentProfileDto : BaseProfileDto
{
    public string? LearningGoals { get; set; }
    public int EnrolledCourses { get; set; }
    public int Achievements { get; set; }
    public int TotalHoursLearned { get; set; }
}






public class SpecialistProfileDto : BaseProfileDto
{
    public string? ProfessionalTitle { get; set; }
    public List<string> ExpertiseTags { get; set; }
    public List<CertificationDto> Certifications { get; set; }
    public int YearsOfExperience { get; set; }
    public List<AvailabilitySlotDto> Availability { get; set; }
    public decimal HourlyRate { get; set; }
    public SessionRates? SessionRates { get; set; }
    public double Rating { get; set; }
    public int StudentsHelped { get; set; }
}







public class UpdateSocialLinkDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class AddUpdateSocialLinkDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ParentProfileDto : BaseProfileDto
{
    public int ChildrenCount { get; set; }
    public string? LearningGoals { get; set; }
    public int EnrolledCourses { get; set; }
    public int Achievements { get; set; }
    public int TotalHoursLearned { get; set; }

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

    // Card fields
    public string? CardNumber { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? Cvv { get; set; }
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



public class ChildProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsAlreadyLinked { get; set; }
    public string? CurrentParentId { get; set; }
}

public class CreateChildDto
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;
}

public class ChildProgressDto
{
    public string ChildId { get; set; } = string.Empty;
    public string ChildName { get; set; } = string.Empty;
    public List<CourseProgressDto> EnrolledCourses { get; set; } = new();
    public int TotalCoursesEnrolled { get; set; }
    public int CompletedCourses { get; set; }
    public double AverageProgress { get; set; }
    public int TotalHoursLearned { get; set; }
    public List<AchievementDto> RecentAchievements { get; set; } = new();
}

public class CourseProgressDto
{
    public string CourseId { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string? CourseThumbnail { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public DateTime EnrolledDate { get; set; }
    public DateTime? LastAccessedDate { get; set; }
    public int TimeSpentMinutes { get; set; }
}

public class AchievementDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime EarnedAt { get; set; }
}


public class ContentCreatorProfileDto : BaseProfileDto
{
    public bool IsVerifiedCreator { get; set; }
    public List<string> ExpertiseTags { get; set; }
    public int TotalCourses { get; set; }
    public int TotalStudents { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageRating { get; set; }
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

