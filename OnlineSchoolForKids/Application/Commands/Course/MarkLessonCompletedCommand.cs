using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using Application.Commands.Leaderboard;
using Application.Queries;
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
    private readonly IMediator _mediator;
    private readonly ILogger<MarkLessonCompleteHandler> _logger;

    public MarkLessonCompleteHandler(
        ILessonProgressRepository lessonProgressRepo,
        IEnrollmentRepository enrollmentRepo,
        ICourseRepository courseRepo,
        IMediator mediator,
        ILogger<MarkLessonCompleteHandler> logger)
    {
        _lessonProgressRepo = lessonProgressRepo;
        _enrollmentRepo = enrollmentRepo;
        _courseRepo = courseRepo;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<MarkLessonCompleteResponse> Handle(
        MarkLessonCompleteCommand request,
        CancellationToken ct)
    {
        try
        {
            var dto = request.Dto;

            var lessonProgress = await _lessonProgressRepo.GetOneAsync(
                lp => lp.UserId == request.UserId &&
                      lp.CourseId == dto.CourseId &&
                      lp.LessonId == dto.LessonId,
                ct);

            bool wasAlreadyCompleted = lessonProgress?.IsCompleted ?? false;

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
            var courseProgress = totalLessons > 0 ? (double)completedLessons / totalLessons * 100 : 0;
            bool courseCompleted = courseProgress >= 100;

            var enrollment = await _enrollmentRepo.GetOneAsync(
                e => e.UserId == request.UserId && e.CourseId == dto.CourseId,
                ct);

            bool courseJustCompleted = false;

            if (enrollment != null)
            {
                enrollment.Progress = courseProgress;
                if (courseCompleted && !enrollment.IsCompleted)
                {
                    enrollment.IsCompleted = true;
                    enrollment.CompletedAt = DateTime.UtcNow;
                    courseJustCompleted = true;
                }
                await _enrollmentRepo.UpdateAsync(enrollment.Id, enrollment, ct);
            }

            _logger.LogInformation(
                "Lesson {LessonId} marked complete for user {UserId}. Course progress: {Progress}%",
                dto.LessonId, request.UserId, courseProgress);

            // ── Award points (only for a lesson newly completed this call) ──
            int pointsEarned = 0;
            if (!wasAlreadyCompleted)
            {
                await _mediator.Send(new AwardPointsCommand
                {
                    Dto = new AwardPointsDto
                    {
                        UserId = request.UserId,
                        Points = 10,
                        Reason = "LessonCompleted",
                        Description = $"Completed lesson {dto.LessonId}",
                        RelatedEntityId = dto.LessonId
                    }
                }, ct);
                pointsEarned += 10;
            }

            if (courseJustCompleted)
            {
                await _mediator.Send(new AwardPointsCommand
                {
                    Dto = new AwardPointsDto
                    {
                        UserId = request.UserId,
                        Points = 100,
                        Reason = "CourseCompleted",
                        Description = $"Completed course {dto.CourseId}",
                        RelatedEntityId = dto.CourseId
                    }
                }, ct);
                pointsEarned += 100;
            }

            var stats = await _mediator.Send(new GetUserStatsQuery { UserId = request.UserId }, ct);

            return new MarkLessonCompleteResponse
            {
                Success = true,
                Message = courseCompleted ? "Course completed! 🎉" : "Lesson completed!",
                CourseCompleted = courseCompleted,
                CourseProgress = courseProgress,
                PointsEarned = pointsEarned,
                TotalPoints = stats?.TotalPoints ?? 0
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
        public double CourseProgress { get; set; }
        public int PointsEarned { get; set; }
        public int TotalPoints { get; set; }
    }
}