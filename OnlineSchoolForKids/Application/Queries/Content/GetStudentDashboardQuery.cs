using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.Extensions.Logging;


namespace Application.Queries.Content
{
    public class GetStudentDashboardQuery : IRequest<StudentDashboardDto>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GetStudentDashboardHandler : IRequestHandler<GetStudentDashboardQuery, StudentDashboardDto>
    {
        private readonly ICourseProgressRepository _courseProgressRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly IAttemptRepository _quizAttemptRepo;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetStudentDashboardHandler> _logger;

        public GetStudentDashboardHandler(
            ICourseProgressRepository courseProgressRepo,
            ICourseRepository courseRepo,
            IAttemptRepository quizAttemptRepo,
            IUserRepository userRepository,
            ILogger<GetStudentDashboardHandler> logger)
        {
            _courseProgressRepo = courseProgressRepo;
            _courseRepo = courseRepo;
            _quizAttemptRepo = quizAttemptRepo;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<StudentDashboardDto> Handle(GetStudentDashboardQuery request, CancellationToken ct)
        {
            try
            {
                var courseProgresses = await _courseProgressRepo.GetAllAsync(
                    cp => cp.UserId == request.UserId,
                    ct);

                var courseIds = courseProgresses.Select(cp => cp.CourseId).ToList();
                var courses = await _courseRepo.GetAllAsync(
                    c => courseIds.Contains(c.Id),
                    ct);

                var quizAttempts = await _quizAttemptRepo.GetAllAsync(
                    qa => qa.UserId == request.UserId && courseIds.Contains(qa.CourseId),
                    ct);

                var courseDict = courses.ToDictionary(c => c.Id);
                var instructorIds = courses.Select(c => c.InstructorId).ToList();

                var instructors = await _userRepository.GetAllAsync(
                    u => instructorIds.Contains(u.Id) && u.Role == UserRole.ContentCreator,
                    ct);

                var instructorDict = instructors.ToDictionary(i => i.Id);
                var enrolledCourses = courseProgresses.Select(cp =>
                {
                    var course = courseDict.GetValueOrDefault(cp.CourseId);
                    if (course == null) return null;

                    var courseQuizAttempts = quizAttempts.Where(qa => qa.CourseId == cp.CourseId && qa.Score.HasValue);
                    var avgScore = courseQuizAttempts.Any() ? courseQuizAttempts.Average(qa => qa.Score!.Value) : (decimal?)null;

                    var instructorName = instructorDict.TryGetValue(course.InstructorId, out var instructor)  ? instructor.FullName : "Unknown";
                    return new EnrolledCourseProgressDto
                    {
                        CourseId = cp.CourseId,
                        CourseTitle = course.Title,
                        CourseThumbnail = course.ThumbnailUrl,
                        InstructorName = instructorName,
                        ProgressPercentage = cp.ProgressPercentage,
                        CompletedLessons = cp.CompletedLessons,
                        TotalLessons = cp.TotalLessons,
                        TimeSpent = FormatTimeSpent(cp.TimeSpent),
                        AverageQuizScore = avgScore,
                        LastAccessedAt = cp.LastAccessedAt
                    };
                }).Where(c => c != null).ToList()!;

                var stats = new StudentStatsDto
                {
                    TotalCoursesEnrolled = enrolledCourses.Count,
                    CompletedCourses = enrolledCourses.Count(c => c.ProgressPercentage >= 100),
                    InProgressCourses = enrolledCourses.Count(c => c.ProgressPercentage < 100),
                    TotalTimeSpent = FormatTimeSpent(courseProgresses.Sum(cp => cp.TimeSpent)),
                    AverageProgress = enrolledCourses.Any() ? enrolledCourses.Average(c => c.ProgressPercentage) : 0
                };

                return new StudentDashboardDto
                {
                    EnrolledCourses = enrolledCourses,
                    Stats = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student dashboard");
                return new StudentDashboardDto();
            }
        }

        private static string FormatTimeSpent(int totalSeconds)
        {
            var ts = TimeSpan.FromSeconds(totalSeconds);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m";
        }
    }
    public class StudentDashboardDto
    {
        public List<EnrolledCourseProgressDto> EnrolledCourses { get; set; } = new();
        public StudentStatsDto Stats { get; set; } = new();
    }

    public class EnrolledCourseProgressDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string CourseThumbnail { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public decimal ProgressPercentage { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public string TimeSpent { get; set; } = string.Empty; // Formatted: "26h 15m"
        public decimal? AverageQuizScore { get; set; }
        public DateTime? LastAccessedAt { get; set; }
    }

    public class StudentStatsDto
    {
        public int TotalCoursesEnrolled { get; set; }
        public int CompletedCourses { get; set; }
        public int InProgressCourses { get; set; }
        public string TotalTimeSpent { get; set; } = string.Empty;
        public decimal AverageProgress { get; set; }
    }

}
