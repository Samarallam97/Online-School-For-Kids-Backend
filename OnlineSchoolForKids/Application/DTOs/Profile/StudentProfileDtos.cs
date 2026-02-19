using Application.Commands.Profile.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Profile;

public class StudentProfileDto : BaseProfileDto
{
    public string? LearningGoals { get; set; }
    public int EnrolledCourses { get; set; }
    public int Achievements { get; set; }
    public int TotalHoursLearned { get; set; }
}



public class AcceptParentInviteDto
{
    public string Message { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
}

public class ParentInfoDto
{
    public string ParentId { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string? ParentProfilePictureUrl { get; set; }
    public DateTime? LinkedSince { get; set; }
}
