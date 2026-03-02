using Domain.Entities.Content;
using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using static MarkLessonCompleteHandler;


public class MarkLessonCompleteCommand : IRequest<MarkLessonCompleteResponse>
{
    public string UserId { get; set; } = string.Empty;
    public MarkLessonCompleteDto Dto { get; set; } = new();
}

public class MarkLessonCompleteHandler : IRequestHandler<MarkLessonCompleteCommand, MarkLessonCompleteResponse>
{
    private readonly ILessonProgressRepository _lessonProgressRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly ILogger<MarkLessonCompleteHandler> _logger;

    public MarkLessonCompleteHandler(
        ILessonProgressRepository lessonProgressRepo,
        IEnrollmentRepository enrollmentRepo,
        ICourseRepository courseRepo,
        ILogger<MarkLessonCompleteHandler> logger)
    {
        _lessonProgressRepo = lessonProgressRepo;
        _enrollmentRepo = enrollmentRepo;
        _courseRepo = courseRepo;
        _logger = logger;
    }

    public async Task<MarkLessonCompleteResponse> Handle(
        MarkLessonCompleteCommand request,
        CancellationToken ct)
    {
        try
        {
            var dto = request.Dto;

            // Get or create lesson progress
            var lessonProgress = await _lessonProgressRepo.GetOneAsync(
                lp => lp.UserId == request.UserId &&
                      lp.CourseId == dto.CourseId &&
                      lp.LessonId == dto.LessonId,
                ct);

            if (lessonProgress != null)
            {
                lessonProgress.IsCompleted = true;
                lessonProgress.CompletedAt = DateTime.UtcNow;
                lessonProgress.WatchedPercentage = 100;
                await _lessonProgressRepo.UpdateAsync(lessonProgress.Id, lessonProgress, ct);
            }
            else
            {
                lessonProgress = new LessonProgress
                {
                    UserId = request.UserId,
                    CourseId = dto.CourseId,
                    LessonId = dto.LessonId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow,
                    WatchedPercentage = 100
                };
                await _lessonProgressRepo.CreateAsync(lessonProgress, ct);
            }

            // Calculate course progress
            var course = await _courseRepo.GetByIdAsync(dto.CourseId, ct);
            if (course == null)
            {
                return new MarkLessonCompleteResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            var totalLessons = course?.Sections?.Sum(s => s.Lessons?.Count ?? 0) ?? 0;
            var allProgress = await _lessonProgressRepo.GetAllAsync(
                lp => lp.UserId == request.UserId && lp.CourseId == dto.CourseId,
                ct);

            var completedLessons = allProgress.Count(lp => lp.IsCompleted);
            var courseProgress = totalLessons > 0 ? (decimal)completedLessons / totalLessons * 100 : 0;
            bool courseCompleted = courseProgress >= 100;

            // Update enrollment
            var enrollment = await _enrollmentRepo.GetOneAsync(
                e => e.UserId == request.UserId && e.CourseId == dto.CourseId,
                ct);

            if (enrollment != null)
            {
                enrollment.Progress = courseProgress;
                if (courseCompleted && !enrollment.IsCompleted)
                {
                    enrollment.IsCompleted = true;
                    enrollment.CompletedAt = DateTime.UtcNow;
                }
                await _enrollmentRepo.UpdateAsync(enrollment.Id, enrollment, ct);
            }

            _logger.LogInformation(
                "Lesson {LessonId} marked complete for user {UserId}. Course progress: {Progress}%",
                dto.LessonId, request.UserId, courseProgress);

            return new MarkLessonCompleteResponse
            {
                Success = true,
                Message = courseCompleted ? "Course completed! 🎉" : "Lesson completed!",
                CourseCompleted = courseCompleted,
                CourseProgress = courseProgress
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking lesson complete");
            return new MarkLessonCompleteResponse
            {
                Success = false,
                Message = "Failed to mark lesson complete"
            };
        }
    }
    public class MarkLessonCompleteDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
    }

    public class MarkLessonCompleteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool CourseCompleted { get; set; } = false;
        public decimal CourseProgress { get; set; }
    }
}

