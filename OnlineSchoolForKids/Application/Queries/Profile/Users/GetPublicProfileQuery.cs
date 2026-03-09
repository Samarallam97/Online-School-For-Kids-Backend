using Application.Commands.Profile.Creator;
using Application.Queries.Profile.Creators;
using Application.Queries.Profile.Specialists;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;


namespace Application.Queries.Profile.Users;

public record GetPublicProfileQuery(string TargetUserId) : IRequest<PublicProfileDto>;
public class GetPublicProfileQueryHandler : IRequestHandler<GetPublicProfileQuery, PublicProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICourseRepository _courseRepository;

    public GetPublicProfileQueryHandler(
        IUserRepository userRepository,
        ICourseRepository courseRepository)
    {
        _userRepository = userRepository;
        _courseRepository = courseRepository;
    }

    public async Task<PublicProfileDto> Handle(GetPublicProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        // Base DTO — fields shared across all roles
        var dto = new PublicProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Bio = user.Bio,
            Country = user.Country,
            Role = user.Role.ToString(),
            JoinedDate = user.CreatedAt,
            SocialLinks = user.SocialLinks?.Select(s => new SocialLinkDto
            {
                Id = s.Id,
                Name = s.Name,
                Value = s.Value
            }).ToList()
        };

        switch (user.Role)
        {
            case UserRole.Student:
            case UserRole.Parent:
                await MapStudentOrParent(dto, user, cancellationToken);
                break;

            case UserRole.ContentCreator:
                await MapCreator(dto, user, cancellationToken);
                break;

            case UserRole.Specialist:
                MapSpecialist(dto, user);
                break;
        }

        return dto;
    }

    // ── Student / Parent ────────────────────────────────────────────────────
    private async Task MapStudentOrParent(
        PublicProfileDto dto,
        User user,
        CancellationToken ct)
    {
        dto.TotalHoursLearned = user.TotalHoursLearned ?? 0;
        dto.LearningGoals = user.LearningGoals;
        dto.Achievements = user.AchievementIds?.Count ?? 0;

        // Enrolled courses
        if (user.EnrolledCourseIds != null && user.EnrolledCourseIds.Count > 0)
        {
            dto.EnrolledCourses = user.EnrolledCourseIds.Count;

            var courses = await _courseRepository.GetAllAsync(
                c => user.EnrolledCourseIds.Contains(c.Id), ct);

            //TODO::
            dto.EnrolledCoursesList = courses.Select(c => new EnrolledCourseDto
            {
                Name = c.Title,
                Instructor = "UnKnown",
                Progress = 50
            }).ToList();
        }
        else
        {
            dto.EnrolledCourses = 0;
            dto.EnrolledCoursesList = [];
        }

        // Achievements — adapt to your Achievement entity/repository
        dto.RecentAchievements = [];
    }

    // ── Content Creator ─────────────────────────────────────────────────────
    private async Task MapCreator(
        PublicProfileDto dto,
        Domain.Entities.Users.User user,
        CancellationToken ct)
    {
        dto.Specializations = user.ExpertiseTags;
        dto.TotalStudents = user.TotalStudents ?? 0;
        dto.AverageRating = user.AverageRating ?? 0;

        // Certifications (no file URL exposed publicly)
        dto.Certifications = user.Certifications?.Select(c => new CertificationDto
        {
            Id = c.Id,
            Name = c.Name,
            Issuer = c.Issuer,
            Year = c.Year,
            DocumentUrl = c.DocumentUrl
            
        }).ToList();

        // Work experiences
        dto.Experiences = user.WorkExperiences?.Select(e => new WorkExperienceDto
        {
            Id = e.Id,
            Title = e.Title,
            Place = e.Place,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            IsCurrentRole = e.IsCurrentRole
        }).ToList();

        // Only courses marked as visible on profile
        if (user.CreatedCourseIds != null && user.CreatedCourseIds.Count > 0)
        {
            var allCourses = await _courseRepository.GetAllAsync(
                c => user.CreatedCourseIds.Contains(c.Id) && c.IsVisible, ct);

            dto.TotalCourses = user.CreatedCourseIds.Count;

            dto.VisibleCourses = allCourses.Select(c => new CreatorCourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Thumbnail = c.ThumbnailUrl,
                StudentsCount = c.EnrolledStudentIds?.Count ?? 0,
                Rating = c.Rating,
                Category = c.Category?.Name ?? string.Empty
            }).ToList();
        }
        else
        {
            dto.TotalCourses = 0;
            dto.VisibleCourses = [];
        }
    }

    // ── Specialist ──────────────────────────────────────────────────────────
    private static void MapSpecialist(PublicProfileDto dto, Domain.Entities.Users.User user)
    {
        dto.ProfessionalTitle = user.ProfessionalTitle;
        dto.YearsOfExperience = user.YearsOfExperience ?? 0;
        dto.HourlyRate = user.HourlyRate ?? 0;
        dto.Rating = user.AverageRating ?? 0;
        dto.StudentsHelped = user.TotalStudents ?? 0;
        dto.Specializations = user.ExpertiseTags;

        dto.Certifications = user.Certifications?.Select(c => new CertificationDto
        {
            Id = c.Id,
            Name = c.Name,
            Issuer = c.Issuer,
            Year = c.Year,
            DocumentUrl = c.DocumentUrl

        }).ToList();

        dto.Experiences = user.WorkExperiences?.Select(e => new WorkExperienceDto
        {
            Id = e.Id,
            Title = e.Title,
            Place = e.Place,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            IsCurrentRole = e.IsCurrentRole
        }).ToList();

        // Group slots by day for cleaner display
        dto.AvailabilitySlots = user.Availability?.Select(s => new AvailabilitySlotDto
        {
            Day = s.Day,
            StartTime = s.StartTime,
            EndTime = s.EndTime
        }).ToList();
    }
}

public class PublicProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public string? Country { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; }

    // Common
    public List<SocialLinkDto>? SocialLinks { get; set; }

    // Student / Parent
    public int? EnrolledCourses { get; set; }
    public int? Achievements { get; set; }
    public int? TotalHoursLearned { get; set; }
    public string? LearningGoals { get; set; }
    public List<RecentAchievementDto>? RecentAchievements { get; set; }
    public List<EnrolledCourseDto>? EnrolledCoursesList { get; set; }

    // Creator
    public List<string>? Specializations { get; set; }
    public int? TotalCourses { get; set; }
    public int? TotalStudents { get; set; }
    public double? AverageRating { get; set; }
    public List<CertificationDto>? Certifications { get; set; }
    public List<WorkExperienceDto>? Experiences { get; set; }
    public List<CreatorCourseDto>? VisibleCourses { get; set; }

    // Specialist
    public string? ProfessionalTitle { get; set; }
    public int? YearsOfExperience { get; set; }
    public decimal? HourlyRate { get; set; }
    public double? Rating { get; set; }
    public int? StudentsHelped { get; set; }
    public List<AvailabilitySlotDto>? AvailabilitySlots { get; set; }
}


public class EnrolledCourseDto
{
    public string Name { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public int? Progress { get; set; }
}

public class CertificationDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Issuer { get; set; }
    public int Year { get; set; }
    public string? DocumentUrl { get; set; }
}

public class RecentAchievementDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime EarnedDate { get; set; }
}

