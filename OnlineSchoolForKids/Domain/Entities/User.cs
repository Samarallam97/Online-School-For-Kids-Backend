using Domain.Enums;

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


    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
    public string? GoogleId { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Role-specific fields
    public string? Expertise { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? CvLink { get; set; }


    public string? ParentId { get; set; }
    public List<string>? ChildrenIds { get; set; }

}