using Application.Commands.Profile.Users;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs.Profile;

public class ParentProfileDto : BaseProfileDto
{
    public int ChildrenCount { get; set; }
    public string? LearningGoals { get; set; }
    public int EnrolledCourses { get; set; }
    public int Achievements { get; set; }
    public int TotalHoursLearned { get; set; }

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

public class SearchChildDto
{
    public bool Exists { get; set; }
    public ChildProfileDto? Child { get; set; }
}

/// <summary>
/// DTO for child profile when found
/// </summary>
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

/// <summary>
/// DTO for creating a new child account
/// </summary>
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

/// <summary>
/// DTO for child's course progress
/// </summary>
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

/// <summary>
/// DTO for course progress
/// </summary>
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

/// <summary>
/// DTO for achievement
/// </summary>
public class AchievementDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime EarnedAt { get; set; }
}

