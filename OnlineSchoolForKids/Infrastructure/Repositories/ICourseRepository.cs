using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for course operations
/// </summary>
public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(string id);
    Task<List<Course>> GetByIdsAsync(List<string> ids);
    Task<List<Course>> GetAllAsync();
}

/// <summary>
/// Repository for progress tracking
/// </summary>
public interface IProgressRepository
{
    Task<CourseProgress?> GetCourseProgressAsync(string userId, string courseId);
    Task<List<Achievement>> GetRecentAchievementsAsync(string userId, int count);
    Task UpdateProgressAsync(CourseProgress progress);
}

// Domain Entities (add these to your Domain.Entities namespace)

public class Course
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourseProgress
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public int CompletedLessons { get; set; }
    public DateTime EnrolledDate { get; set; }
    public DateTime? LastAccessedDate { get; set; }
    public int TimeSpentMinutes { get; set; }
}

public class Achievement
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime EarnedAt { get; set; }
}
