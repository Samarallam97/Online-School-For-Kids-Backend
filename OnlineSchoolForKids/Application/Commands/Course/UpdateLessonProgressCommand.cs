using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using static UpdateLessonProgressHandler;

public class UpdateLessonProgressCommand : IRequest<UpdateLessonProgressResponse>
{
    public string UserId { get; set; } = string.Empty;
    public UpdateLessonProgressDto Dto { get; set; } = new();
}

public class UpdateLessonProgressHandler : IRequestHandler<UpdateLessonProgressCommand, UpdateLessonProgressResponse>
{
    private readonly ILessonProgressRepository _lessonProgressRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly ILessonRepository _lessonRepository;
    private readonly ICourseProgressRepository _courseProgressRepository;
    private readonly ILogger<UpdateLessonProgressHandler> _logger;

    public UpdateLessonProgressHandler(
        ILessonProgressRepository lessonProgressRepo,
        IEnrollmentRepository enrollmentRepo,
        ICourseRepository courseRepo,
        ILessonRepository lessonRepository,
        ICourseProgressRepository courseProgressRepository,
        ILogger<UpdateLessonProgressHandler> logger)
    {
        _lessonProgressRepo = lessonProgressRepo;
        _enrollmentRepo = enrollmentRepo;
        _courseRepo = courseRepo;
        _lessonRepository = lessonRepository;
        _courseProgressRepository = courseProgressRepository;
        _logger = logger;
    }

    public async Task<UpdateLessonProgressResponse> Handle(
        UpdateLessonProgressCommand request,
        CancellationToken ct)
    {
        try
        {
            var dto = request.Dto;
            var lessonExists = await _lessonRepository.ExistsAsync(l => l.Id == dto.LessonId && l.CourseId == dto.CourseId, ct);
         
            if (!lessonExists)
            {
                return new UpdateLessonProgressResponse
                {
                    Success = false,
                    Message = "Invalid course or lesson"
                };
            }
            // Get or create lesson progress
            var lessonProgress = await _lessonProgressRepo.GetOneAsync(
                lp => lp.UserId == request.UserId &&
                      lp.CourseId == dto.CourseId &&
                      lp.LessonId == dto.LessonId,
                ct);

            if (lessonProgress != null)
            {
                // Update existing
                lessonProgress.VideoPosition = dto.VideoPosition;
                lessonProgress.TimeSpent += dto.TimeSpent;
                lessonProgress.IsCompleted = dto.IsCompleted;
                lessonProgress.LastAccessedAt = DateTime.UtcNow;

                if (dto.IsCompleted && !lessonProgress.CompletedAt.HasValue)
                {
                    lessonProgress.CompletedAt = DateTime.UtcNow;
                    lessonProgress.WatchedPercentage = 100;
                }

                await _lessonProgressRepo.UpdateAsync(lessonProgress.Id, lessonProgress, ct);
            }
            else
            {
                // Create new
                lessonProgress = new LessonProgress
                {
                    UserId = request.UserId,
                    CourseId = dto.CourseId,
                    LessonId = dto.LessonId,
                    VideoPosition = dto.VideoPosition,
                    TimeSpent = dto.TimeSpent,
                    IsCompleted = dto.IsCompleted,
                    CompletedAt = dto.IsCompleted ? DateTime.UtcNow : null,
                    WatchedPercentage = dto.IsCompleted ? 100 : 0,
                    LastAccessedAt = DateTime.UtcNow
                };

                await _lessonProgressRepo.CreateAsync(lessonProgress, ct);
            }

            // Update enrollment (Continue Learning data)
            var enrollment = await _enrollmentRepo.GetOneAsync(
                e => e.UserId == request.UserId && e.CourseId == dto.CourseId,
                ct);

            if (enrollment != null)
            {
                enrollment.LastAccessedLessonId = dto.LessonId;
                enrollment.LastAccessedAt = DateTime.UtcNow;

                // Calculate overall progress
                var course = await _courseRepo.GetByIdAsync(dto.CourseId, ct);
                if (course != null)
                {
                    var allLessons = course.Sections.SelectMany(s => s.Lessons).ToList();
                    var totalLessons = allLessons.Count;

                    var allProgress = await _lessonProgressRepo.GetAllAsync(
                        lp => lp.UserId == request.UserId && lp.CourseId == dto.CourseId,
                        ct);

                    var completedLessons = allProgress.Count(lp => lp.IsCompleted);
                    var progressPercentage = totalLessons > 0 ? (decimal)completedLessons / totalLessons * 100 : 0;
                    enrollment.Progress = progressPercentage;

                    // Check if course completed
                    if (enrollment.Progress >= 100 && !enrollment.IsCompleted)
                    {
                        enrollment.IsCompleted = true;
                        enrollment.CompletedAt = DateTime.UtcNow;
                    }

                    await _enrollmentRepo.UpdateAsync(enrollment.Id, enrollment, ct);

                    var courseProgress = await _courseProgressRepository.GetOneAsync(
                        cp => cp.UserId == request.UserId && cp.CourseId == dto.CourseId,
                        ct);

                    if (courseProgress == null)
                    {
                        courseProgress = new CourseProgress
                        {
                            UserId = request.UserId,
                            CourseId = dto.CourseId,
                            EnrollmentId = enrollment.Id,
                            CompletedLessons = completedLessons,
                            TotalLessons = totalLessons,
                            ProgressPercentage = progressPercentage,
                            TimeSpent = allProgress.Sum(lp => lp.TimeSpent),
                            LastAccessedAt = DateTime.UtcNow,
                            CompletedAt = enrollment.IsCompleted ? enrollment.CompletedAt : null,
                        };
                        await _courseProgressRepository.CreateAsync(courseProgress, ct);
                    }
                    else
                    {
                        courseProgress.CompletedLessons = completedLessons;
                        courseProgress.TotalLessons = totalLessons;
                        courseProgress.ProgressPercentage = progressPercentage;
                        courseProgress.TimeSpent = allProgress.Sum(lp => lp.TimeSpent);
                        courseProgress.LastAccessedAt = DateTime.UtcNow;
                        courseProgress.CompletedAt = enrollment.IsCompleted ? enrollment.CompletedAt : null;

                        await _courseProgressRepository.UpdateAsync(courseProgress.Id, courseProgress, ct);
                    }

                    return new UpdateLessonProgressResponse
                    {
                        Success = true,
                        Message = "Progress updated",
                        CourseProgress = enrollment.Progress
                    };
                }

                return new UpdateLessonProgressResponse
                {
                    Success = true,
                    Message = "Progress updated"
                };
            }
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lesson progress");
            return new UpdateLessonProgressResponse
            {
                Success = false,
                Message = "Failed to update progress"
            };
        }
        return new UpdateLessonProgressResponse
        {
            Success = true,
            Message = "Progress updated"
        };
    }
    public class UpdateLessonProgressDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public int VideoPosition { get; set; } // Current position in seconds
        public int TimeSpent { get; set; } // Time spent in this session (seconds)
        public bool IsCompleted { get; set; } = false;
    }

    public class UpdateLessonProgressResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal CourseProgress { get; set; } // Overall course progress %
    }
}