//using Domain.Entities.Content;
//using Domain.Entities.Content.Progress;
//using Domain.Entities.Content.Quiz;
//using Domain.Enums.Content;
//using Domain.Interfaces.Repositories;
//using MediatR;
//using Microsoft.Extensions.Logging;

//namespace Application.Queries.Content
//{
//    public class GetCourseProgressDetailQuery : IRequest<CourseProgressDetailDto?>
//    {
//        public string UserId { get; set; } = string.Empty;
//        public string CourseId { get; set; } = string.Empty;
//    }

//    public class GetCourseProgressDetailHandler : IRequestHandler<GetCourseProgressDetailQuery, CourseProgressDetailDto?>
//    {
//        private readonly IGenericRepository<CourseProgress> _courseProgressRepo;
//        private readonly IGenericRepository<LessonProgress> _lessonProgressRepo;
//        private readonly IGenericRepository<Course> _courseRepo;
//        private readonly IGenericRepository<QuizAttempt> _quizAttemptRepo;
//        private readonly ILogger<GetCourseProgressDetailHandler> _logger;

//        public GetCourseProgressDetailHandler(
//            IGenericRepository<CourseProgress> courseProgressRepo,
//            IGenericRepository<LessonProgress> lessonProgressRepo,
//            IGenericRepository<Course> courseRepo,
//            IGenericRepository<QuizAttempt> quizAttemptRepo,
//            ILogger<GetCourseProgressDetailHandler> logger)
//        {
//            _courseProgressRepo = courseProgressRepo;
//            _lessonProgressRepo = lessonProgressRepo;
//            _courseRepo = courseRepo;
//            _quizAttemptRepo = quizAttemptRepo;
//            _logger = logger;
//        }

//        public async Task<CourseProgressDetailDto?> Handle(GetCourseProgressDetailQuery request, CancellationToken ct)
//        {
//            try
//            {
//                var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
//                if (course == null) return null;

//                var courseProgress = await _courseProgressRepo.GetOneAsync(
//                    cp => cp.UserId == request.UserId && cp.CourseId == request.CourseId,
//                    ct);

//                var lessonProgresses = await _lessonProgressRepo.GetAllAsync(
//                    lp => lp.UserId == request.UserId && lp.CourseId == request.CourseId,
//                    ct);

//                var lessonProgressDict = lessonProgresses.ToDictionary(lp => lp.LessonId);

//                var sections = course.Sections.OrderBy(s => s.Order).Select(s =>
//                {
//                    var lessons = s.Lessons.OrderBy(l => l.Order).Select(l =>
//                    {
//                        var progress = lessonProgressDict.GetValueOrDefault(l.Id);
//                        return new LessonProgressDto
//                        {
//                            LessonId = l.Id,
//                            LessonTitle = l.Title,
//                            Duration = FormatDuration(l.Duration),
//                            IsCompleted = progress?.IsCompleted ?? false,
//                            WatchedPercentage = progress?.WatchedPercentage ?? 0,
//                            LastPosition = progress?.LastPosition ?? 0
//                        };
//                    }).ToList();

//                    return new SectionProgressDto
//                    {
//                        SectionId = s.Id,
//                        SectionTitle = s.Title,
//                        Order = s.Order,
//                        CompletedLessons = lessons.Count(l => l.IsCompleted),
//                        TotalLessons = lessons.Count,
//                        IsCompleted = lessons.All(l => l.IsCompleted),
//                        Lessons = lessons
//                    };
//                }).ToList();

//                var quizAttempts = await _quizAttemptRepo.GetAllAsync(
//                    qa => qa.UserId == request.UserId && qa.CourseId == request.CourseId && qa.Status == QuizAttemptStatus.Completed,
//                    ct);

//                var quizScores = quizAttempts
//                    .GroupBy(qa => qa.QuizId)
//                    .Select(g => g.OrderByDescending(qa => qa.Score).First())
//                    .Select(qa => new QuizScoreDto
//                    {
//                        QuizId = qa.QuizId,
//                        QuizTitle = "Quiz", // Get from Quiz entity
//                        Score = qa.Score ?? 0,
//                        Passed = qa.Passed ?? false,
//                        AttemptNumber = qa.AttemptNumber,
//                        CompletedAt = qa.CompletedAt ?? DateTime.UtcNow
//                    }).ToList();

//                return new CourseProgressDetailDto
//                {
//                    CourseId = course.Id,
//                    CourseTitle = course.Title,
//                    ProgressPercentage = courseProgress?.ProgressPercentage ?? 0,
//                    CompletedLessons = courseProgress?.CompletedLessons ?? 0,
//                    TotalLessons = courseProgress?.TotalLessons ?? 0,
//                    TimeSpent = FormatTimeSpent(courseProgress?.TimeSpent ?? 0),
//                    AverageQuizScore = quizAttempts.Any() ? quizAttempts.Average(qa => qa.Score ?? 0) : null,
//                    Sections = sections,
//                    QuizScores = quizScores
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting course progress detail");
//                return null;
//            }
//        }

//        private static string FormatTimeSpent(int totalSeconds)
//        {
//            var ts = TimeSpan.FromSeconds(totalSeconds);
//            if (ts.TotalHours >= 1)
//                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
//            return $"{ts.Minutes}m";
//        }

//        private static string FormatDuration(int seconds)
//        {
//            var ts = TimeSpan.FromSeconds(seconds);
//            return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
//        }
//    }
//    public class CourseProgressDetailDto
//    {
//        public string CourseId { get; set; } = string.Empty;
//        public string CourseTitle { get; set; } = string.Empty;
//        public decimal ProgressPercentage { get; set; }
//        public int CompletedLessons { get; set; }
//        public int TotalLessons { get; set; }
//        public string TimeSpent { get; set; } = string.Empty;
//        public decimal? AverageQuizScore { get; set; }
//        public List<SectionProgressDto> Sections { get; set; } = new();
//        public List<QuizScoreDto> QuizScores { get; set; } = new();
//    }
//    public class SectionProgressDto
//    {
//        public string SectionId { get; set; } = string.Empty;
//        public string SectionTitle { get; set; } = string.Empty;
//        public int Order { get; set; }
//        public int CompletedLessons { get; set; }
//        public int TotalLessons { get; set; }
//        public bool IsCompleted { get; set; }
//        public List<LessonProgressDto> Lessons { get; set; } = new();
//    }
//    public class LessonProgressDto
//    {
//        public string LessonId { get; set; } = string.Empty;
//        public string LessonTitle { get; set; } = string.Empty;
//        public string Duration { get; set; } = string.Empty;
//        public bool IsCompleted { get; set; }
//        public decimal WatchedPercentage { get; set; }
//        public int LastPosition { get; set; } // Video position in seconds
//    }
//    public class QuizScoreDto
//    {
//        public string QuizId { get; set; } = string.Empty;
//        public string QuizTitle { get; set; } = string.Empty;
//        public decimal Score { get; set; }
//        public bool Passed { get; set; }
//        public int AttemptNumber { get; set; }
//        public DateTime CompletedAt { get; set; }
//    }
//}



