using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Application.Queries.Content
{
    public class GetContinueLearningQuery : IRequest<ContinueLearningDto?>
    {
        public string UserId { get; set; } 
        public string CourseId { get; set; }
    }

    public class GetContinueLearningHandler : IRequestHandler<GetContinueLearningQuery, ContinueLearningDto?>
    {
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly ILessonProgressRepository _lessonProgressRepo;
        private readonly ILogger<GetContinueLearningHandler> _logger;

        public GetContinueLearningHandler(
            IEnrollmentRepository enrollmentRepo,
            ICourseRepository courseRepo,
            ILessonProgressRepository lessonProgressRepo,
            ILogger<GetContinueLearningHandler> logger)
        {
            _enrollmentRepo = enrollmentRepo;
            _courseRepo = courseRepo;
            _lessonProgressRepo = lessonProgressRepo;
            _logger = logger;
        }

        public async Task<ContinueLearningDto?> Handle(GetContinueLearningQuery request, CancellationToken ct)
        {
            try
            {
                var enrollment = await _enrollmentRepo.GetOneAsync(
                    e => e.UserId == request.UserId && e.CourseId == request.CourseId,
                    ct);

                if (enrollment == null) return null;

                var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                if (course == null) return null;

                string? lastLessonTitle = null;
                int? videoPosition = null;

                if (!string.IsNullOrEmpty(enrollment.LastAccessedLessonId))
                {
                    var lesson = course.Sections
                        .SelectMany(s => s.Lessons)
                        .FirstOrDefault(l => l.Id == enrollment.LastAccessedLessonId);

                    lastLessonTitle = lesson?.Title;

                    var lessonProgress = await _lessonProgressRepo.GetOneAsync(
                        lp => lp.UserId == request.UserId &&
                              lp.LessonId == enrollment.LastAccessedLessonId,
                        ct);

                    videoPosition = lessonProgress?.VideoPosition;
                }

                return new ContinueLearningDto
                {
                    CourseId = course.Id,
                    CourseTitle = course.Title,
                    CourseThumbnail = course.ThumbnailUrl,
                    LastAccessedLessonId = enrollment.LastAccessedLessonId,
                    LastAccessedLessonTitle = lastLessonTitle,
                    VideoPosition = videoPosition,
                    Progress = enrollment.Progress,
                    LastAccessedAt = enrollment.LastAccessedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting continue learning data");
                return null;
            }
        }
    }
        public class ContinueLearningDto
        {
            public string CourseId { get; set; } = string.Empty;
            public string CourseTitle { get; set; } = string.Empty;
            public string CourseThumbnail { get; set; } = string.Empty;
            public string? LastAccessedLessonId { get; set; }
            public string? LastAccessedLessonTitle { get; set; }
            public int? VideoPosition { get; set; } // Where they left off
            public decimal Progress { get; set; }
            public DateTime? LastAccessedAt { get; set; }
        }

    }




   