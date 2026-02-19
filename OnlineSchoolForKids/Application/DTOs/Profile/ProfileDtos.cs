using Application.Commands.Profile.Users;
using Domain.Entities.Users;

namespace Application.DTOs.Profile;





public class UploadProfilePictureDto
{
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string Message { get; set; } = "Profile picture updated successfully";
}


public class RecentAchievementDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime EarnedDate { get; set; }
}

public class EnrolledCourseDto
{
    public string Name { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public int? Progress { get; set; }
}

public class SocialLinkDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
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

