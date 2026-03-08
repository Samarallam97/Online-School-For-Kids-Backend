using Application.DTOs.Profile;
using Application.Queries.Profile.Specialists;
using Application.Queries.Profile.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Queries.Admin.GetUserByIdQueryHandler;

namespace Application.Queries.Admin;

public record GetUserByIdQuery(string UserId, bool CallerIsSuperAdmin) : IRequest<AdminUserDetailDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, AdminUserDetailDto>
{
    private readonly IUserRepository _userRepo;
    public GetUserByIdQueryHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<AdminUserDetailDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' not found.");

        // ── Base ──────────────────────────────────────────────────────────
        var dto = new AdminUserDetailDto
        {
            Id                = user.Id,
            Name              = user.FullName,
            Email             = user.Email,
            EmailVerified     = user.EmailVerified,
            Role              = user.Role.ToString(),
            Status            = user.Status.ToString().ToLower(),
            JoinedDate        = user.CreatedAt,
            LastLoginAt       = user.LastLoginAt,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Phone             = user.Phone,
            Country           = user.Country,
            Bio               = user.Bio,
            DateOfBirth       = user.DateOfBirth == default ? null : user.DateOfBirth,
            AuthProvider      = user.AuthProvider.ToString(),
            IsFirstLogin      = user.IsFirstLogin,
            PortfolioUrl      = user.PortfolioUrl,
            CvLink            = user.CvLink,

            // Notification prefs
            NotificationPreferences = user.NotificationPreferences == null ? null : new NotificationPreferencesDto
            {
                WeeklyReports      = user.NotificationPreferences.WeeklyReports,
                Messages            = user.NotificationPreferences.Messages,
                CommentReplies      = user.NotificationPreferences.CommentReplies,
                ProgressUpdates     = user.NotificationPreferences.ProgressUpdates,
                AchievementAlerts   = user.NotificationPreferences.AchievementAlerts,
                PaymentReminders    = user.NotificationPreferences.PaymentReminders,
                CourseEnrollments   = user.NotificationPreferences.CourseEnrollments,
                NewSessionBooking   = user.NotificationPreferences.NewSessionBooking,
                SessionCancellation = user.NotificationPreferences.SessionCancellation,
                SessionReminder     = user.NotificationPreferences.SessionReminder,
                ReviewNotifications = user.NotificationPreferences.ReviewNotifications,
                PayoutAlerts        = user.NotificationPreferences.PayoutAlerts,
                AccountLogin        = user.NotificationPreferences.AccountLogin,
                SuspiciousActivity  = user.NotificationPreferences.SuspiciousActivity,
            },

            // Payment methods
            PaymentMethods = user.PaymentMethods?.Select(pm => new PaymentMethodDto
            {
                Id                   = pm.Id,
                Type                 = pm.Type.ToString().ToLower(),
                IsDefault            = pm.IsDefault,
                Last4                = pm.Last4,
                Brand                = pm.Brand,
                ExpiryMonth          = pm.ExpiryMonth,
                ExpiryYear           = pm.ExpiryYear,
                VodafoneNumber       = pm.VodafoneNumber,
                InstapayId           = pm.InstapayId,
                FawryReferenceNumber = pm.FawryReferenceNumber,
                AccountHolderName    = pm.AccountHolderName,
                BankName             = pm.BankName,
                AccountNumber        = pm.AccountNumber,
                IBAN                 = pm.IBAN,
                CreatedAt            = pm.CreatedAt,
            }).ToList(),
        };

        // ── Role-specific ─────────────────────────────────────────────────
        switch (user.Role)
        {
            case UserRole.Student:
                dto.Courses           = user.EnrolledCourseIds?.Count ?? 0;
                dto.ParentId          = user.ParentId;
                dto.LearningGoals     = user.LearningGoals;
                dto.EnrolledCourseIds = user.EnrolledCourseIds;
                dto.AchievementIds    = user.AchievementIds;
                dto.TotalHoursLearned = user.TotalHoursLearned;
                break;

            case UserRole.Parent:
                dto.Courses           = user.EnrolledCourseIds?.Count ?? 0;
                dto.LearningGoals     = user.LearningGoals;
                dto.EnrolledCourseIds = user.EnrolledCourseIds;
                dto.AchievementIds    = user.AchievementIds;
                dto.TotalHoursLearned = user.TotalHoursLearned;
                dto.ChildrenIds       = user.ChildrenIds;
                dto.ChildInvitations  = user.ChildInvitaions;
                break;

            case UserRole.ContentCreator:
                dto.Courses           = user.CreatedCourseIds?.Count ?? 0;
                dto.IsVerifiedCreator = user.IsVerifiedCreator;
                dto.ExpertiseTags     = user.ExpertiseTags;
                dto.TotalStudents     = user.TotalStudents;
                dto.TotalRevenue      = user.TotalRevenue;
                dto.AverageRating     = user.AverageRating;
                dto.CreatedCourseIds  = user.CreatedCourseIds;
                dto.SocialLinks       = user.SocialLinks?.Select(s => new SocialLinkDto
                {
                    Id        = s.Id,
                    Name      = s.Name,
                    Value     = s.Value,
                    CreatedAt = s.CreatedAt,
                }).ToList();
                dto.WorkExperiences = user.WorkExperiences?.Select(w => new WorkExperienceDto
                {
                    Id           = w.Id,
                    Title        = w.Title,
                    Place        = w.Place,
                    StartDate    = w.StartDate,
                    EndDate      = w.EndDate,
                    IsCurrentRole = w.IsCurrentRole,
                }).ToList();
                break;

            case UserRole.Specialist:
                dto.Courses           = 0;
                dto.ProfessionalTitle = user.ProfessionalTitle;
                dto.ExpertiseTags     = user.ExpertiseTags;
                dto.YearsOfExperience = user.YearsOfExperience;
                dto.HourlyRate        = user.HourlyRate;
                dto.SessionRates      = user.SessionRates == null ? null : new SessionRatesDto
                {
                    ThirtyMinSession      = user.SessionRates.ThirtyMinSession,
                    SixtyMinSession       = user.SessionRates.SixtyMinSession,
                    PlatformFeePercentage = user.SessionRates.PlatformFeePercentage,
                };
                dto.Certifications = user.Certifications?.Select(c => new CertificationDto
                {
                    Id          = c.Id,
                    Name        = c.Name,
                    Issuer      = c.Issuer,
                    Year        = c.Year,
                    DocumentUrl = c.DocumentUrl,
                }).ToList();
                dto.Availability = user.Availability?.Select(a => new AvailabilitySlotDto
                {
                    Id        = a.Id,
                    Day       = a.Day,
                    StartTime = a.StartTime,
                    EndTime   = a.EndTime,
                }).ToList();
                break;

            case UserRole.Admin:
                dto.Courses = 0;
                // Sensitive admin fields only visible to SuperAdmins
                if (request.CallerIsSuperAdmin)
                {
                    dto.IsSuperAdmin             = user.IsSuperAdmin;
                    dto.TwoFactorEnabled         = user.TwoFactorEnabled;
                    dto.LoginNotifications       = user.LoginNotifications;
                    dto.SuspiciousActivityAlerts = user.SuspiciousActivityAlerts;
                    dto.ActivityLog              = user.ActivityLog?.Select(e => new ActivityLogEntryDto
                    {
                        Id         = e.Id,
                        Action     = e.Action,
                        Details    = e.Details,
                        TargetType = e.TargetType,
                        IpAddress  = e.IpAddress,
                        Timestamp  = e.Timestamp,
                    }).ToList();
                }
                break;
        }

        return dto;
    }
}


public class AdminUserDetailDto : AdminUserDto
{
    // ── Base ──────────────────────────────────────────────────────────────
    public bool EmailVerified { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? Bio { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string AuthProvider { get; set; } = string.Empty;
    public bool IsFirstLogin { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // ── Shared creator / specialist registration ──────────────────────────
    public string? PortfolioUrl { get; set; }
    public string? CvLink { get; set; }

    // ── Student / Parent ──────────────────────────────────────────────────
    public string? ParentId { get; set; }
    public string? LearningGoals { get; set; }
    public List<string>? EnrolledCourseIds { get; set; }
    public List<string>? AchievementIds { get; set; }
    public int? TotalHoursLearned { get; set; }
    public int? Courses { get; set; }


    // ── Parent ────────────────────────────────────────────────────────────
    public List<string>? ChildrenIds { get; set; }
    public List<string>? ChildInvitations { get; set; }

    // ── Creator ───────────────────────────────────────────────────────────
    public bool? IsVerifiedCreator { get; set; }
    public List<string>? ExpertiseTags { get; set; }
    public int? TotalStudents { get; set; }
    public decimal? TotalRevenue { get; set; }
    public double? AverageRating { get; set; }
    public List<string>? CreatedCourseIds { get; set; }
    public List<SocialLinkDto>? SocialLinks { get; set; }
    public List<WorkExperienceDto>? WorkExperiences { get; set; }

    // ── Specialist ────────────────────────────────────────────────────────
    public string? ProfessionalTitle { get; set; }
    public List<CertificationDto>? Certifications { get; set; }
    public int? YearsOfExperience { get; set; }
    public List<AvailabilitySlotDto>? Availability { get; set; }
    public decimal? HourlyRate { get; set; }
    public SessionRatesDto? SessionRates { get; set; }

    // ── Admin (visible to SuperAdmin only) ────────────────────────────────
    public bool? IsSuperAdmin { get; set; }
    public bool? TwoFactorEnabled { get; set; }
    public bool? LoginNotifications { get; set; }
    public bool? SuspiciousActivityAlerts { get; set; }
    public List<ActivityLogEntryDto>? ActivityLog { get; set; }

    // ── Shared nested ─────────────────────────────────────────────────────
    public NotificationPreferencesDto? NotificationPreferences { get; set; }
    public List<PaymentMethodDto>? PaymentMethods { get; set; }
}

// ── Nested DTOs ────────────────────────────────────────────────────────────

public class SocialLinkDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class WorkExperienceDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public bool IsCurrentRole { get; set; }
}

public class CertificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? DocumentUrl { get; set; }
}

public class AvailabilitySlotDto
{
    public string Id { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

public class SessionRatesDto
{
    public decimal ThirtyMinSession { get; set; }
    public decimal SixtyMinSession { get; set; }
    public decimal PlatformFeePercentage { get; set; }
}

public class ActivityLogEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class NotificationPreferencesDto
{
    public bool? WeeklyReports { get; set; }
    public bool? Messages { get; set; }
    public bool? CommentReplies { get; set; }
    public bool? ProgressUpdates { get; set; }
    public bool? AchievementAlerts { get; set; }
    public bool? PaymentReminders { get; set; }
    public bool? CourseEnrollments { get; set; }
    public bool? NewSessionBooking { get; set; }
    public bool? SessionCancellation { get; set; }
    public bool? SessionReminder { get; set; }
    public bool? ReviewNotifications { get; set; }
    public bool? PayoutAlerts { get; set; }
    public bool? AccountLogin { get; set; }
    public bool? SuspiciousActivity { get; set; }
}

public class PaymentMethodDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? Last4 { get; set; }
    public string? Brand { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? VodafoneNumber { get; set; }
    public string? InstapayId { get; set; }
    public string? FawryReferenceNumber { get; set; }
    public string? AccountHolderName { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }
    public DateTime CreatedAt { get; set; }
}
