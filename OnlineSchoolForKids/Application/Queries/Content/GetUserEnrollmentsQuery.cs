using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries.Content;

public class GetUserEnrollmentsQuery : IRequest<IEnumerable<EnrolledCourseDto>>
{
    public string UserId { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════════════════════
//  HANDLER
// ═══════════════════════════════════════════════════════════════════════════════

public class GetUserEnrollmentsQueryHandler
    : IRequestHandler<GetUserEnrollmentsQuery, IEnumerable<EnrolledCourseDto>>
{
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly IUserRepository _userRepository;

    public GetUserEnrollmentsQueryHandler(
        IEnrollmentRepository enrollmentRepo,
        ICourseRepository courseRepo, IUserRepository userRepository)
    {
        _enrollmentRepo = enrollmentRepo;
        _courseRepo     = courseRepo;
        _userRepository=userRepository;
    }

    public async Task<IEnumerable<EnrolledCourseDto>> Handle(
        GetUserEnrollmentsQuery request, CancellationToken ct)
    {
        // 1. Get all enrollments for this user
        var enrollments = await _enrollmentRepo.GetAllAsync(
            e => e.UserId == request.UserId, ct);

        if (!enrollments.Any())
            return Enumerable.Empty<EnrolledCourseDto>();

        // 2. Load course details in one batch
        var courseIds = enrollments.Select(e => e.CourseId).ToList();
        var courses = await _courseRepo.GetAllAsync(
            c => courseIds.Contains(c.Id), ct);
        var courseDict = courses.ToDictionary(c => c.Id);


        // 3. Map to DTO
        var result = enrollments
            .Where(e => courseDict.ContainsKey(e.CourseId))
            .Select(e =>
            {
                var course = courseDict[e.CourseId];
                var totalLessons = course.Sections?
                    .SelectMany(s => s.Lessons ?? Enumerable.Empty<dynamic>())
                    .Count() ?? 0;
                var completed = totalLessons > 0
     ? (int)Math.Round(e.Progress / 100.0 * totalLessons)
     : 0;
                var status = e.IsCompleted
                    ? "completed"
                    : e.Progress > 0
                        ? "in_progress"
                        : "not_started";

                return new EnrolledCourseDto
                {
                    Id                = e.Id,
                    CourseId          = course.Id,
                    Title             = course.Title,
                    InstructorName    = course.Instructor?.FullName ?? string.Empty,
                    ThumbnailUrl      = course.ThumbnailUrl,
                    ProgressPercent   = (int)Math.Round(e.Progress),
                    LastAccessedAt    = e.LastAccessedAt,
                    TotalLessons      = totalLessons,
                    CompletedLessons  = completed,
                    Duration          = FormatDuration(course.DurationHours),
                    Status            = status,
                    EnrolledAt        = e.EnrollmentDate,
                    IsCompleted       = e.IsCompleted,
                    CompletedAt       = e.CompletedAt,
                    Rating            = course.Rating,
                    TotalStudents     = course.TotalStudents,
                };
            })
            .OrderByDescending(e => e.LastAccessedAt ?? e.EnrolledAt)
            .ToList();

        return result;
    }

    private static string FormatDuration(double? hours)
    {
        if (hours is null or 0) return "—";
        return hours < 1
            ? $"{(int)(hours * 60)}m"
            : $"{hours:0.#}h";
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  DTO
// ═══════════════════════════════════════════════════════════════════════════════

public class EnrolledCourseDto
{
    public string Id { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int ProgressPercent { get; set; }   // 0-100
    public DateTime? LastAccessedAt { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string Status { get; set; } = "not_started"; // not_started | in_progress | completed
    public DateTime EnrolledAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal Rating { get; set; }
    public int TotalStudents { get; set; }
}