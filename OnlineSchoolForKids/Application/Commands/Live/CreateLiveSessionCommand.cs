using Application.DTOs;
using Domain.Entities.Live;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Live
{
    public record ScheduleLiveSessionCommand(
    string InstructorId,
    string LessonId,
    string Title,
    string? Description,
    bool AllowChat,
    bool AllowQuestions,
    DateTime? ScheduledAt) : IRequest<Result<LiveSessionDto>>;

    public class ScheduleLiveSessionHandler : IRequestHandler<ScheduleLiveSessionCommand, Result<LiveSessionDto>>
    {
        private readonly ILessonRepository _lessonRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly ILiveSessionRepository _liveRepo;
        private readonly ILogger<ScheduleLiveSessionHandler> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ScheduleLiveSessionHandler(
            ILessonRepository lessonRepo,
            ICourseRepository courseRepo,
            ILiveSessionRepository liveRepo,
            ILogger<ScheduleLiveSessionHandler> logger,
            IStringLocalizer<SharedResource> localizer)
        {
            _lessonRepo = lessonRepo;
            _courseRepo = courseRepo;
            _liveRepo = liveRepo;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<Result<LiveSessionDto>> Handle(
            ScheduleLiveSessionCommand request, CancellationToken ct)
        {
            var lesson = await _lessonRepo.GetByIdAsync(request.LessonId, ct);
            if (lesson is null)
                return Result<LiveSessionDto>.Failure(_localizer["LessonNotFound"]);

            var course = await _courseRepo.GetByIdAsync(lesson.CourseId, ct);
            if (course is null)
                return Result<LiveSessionDto>.Failure(_localizer["CourseIsNotFound"]);

            if (course.InstructorId != request.InstructorId)
                return Result<LiveSessionDto>.Failure(_localizer["OnlyInstructorCanScheduleLiveSession"]);

            // Prevent scheduling a second session on the same lesson
            var existing = await _liveRepo.GetByLessonIdAsync(request.LessonId, ct);
            if (existing is not null && existing.Status != "ended")
                return Result<LiveSessionDto>.Failure(_localizer["LessonAlreadyHasActiveOrScheduledSession"]);

            var channelName = $"live_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";

            var session = new LiveSession
            {
                LessonId = request.LessonId,
                HostId = request.InstructorId,
                Title = request.Title,
                Description = request.Description,
                ChannelName = channelName,
                Status = request.ScheduledAt.HasValue ? "scheduled" : "live",
                AllowChat = request.AllowChat,
                AllowQuestions = request.AllowQuestions,
                ScheduledAt = request.ScheduledAt,
                StartedAt = request.ScheduledAt.HasValue ? null : DateTime.UtcNow
            };

            await _liveRepo.CreateAsync(session, ct);

            // Link the lesson to its live session so the curriculum can show it
            lesson.IsLive = true;
            lesson.LiveSessionId = session.Id;
            await _lessonRepo.UpdateAsync(lesson.Id, lesson, ct);

            _logger.LogInformation(
                "Live session scheduled: {Id} for lesson {LessonId} by instructor {InstructorId}",
                session.Id, request.LessonId, request.InstructorId);

            return Result<LiveSessionDto>.Success(MapToDto(session));
        }

        private static LiveSessionDto MapToDto(LiveSession s) => new(
            s.Id, s.LessonId, s.Title, s.Description,
            s.HostId, s.ChannelName, s.Status,
            s.ViewerCount, s.AllowChat, s.AllowQuestions,
            s.ScheduledAt, s.StartedAt, s.EndedAt, s.WhiteboardUrl);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // START LIVE SESSION
    // Instructor clicks "Go Live". Notifies all enrolled students via SignalR.
    // ─────────────────────────────────────────────────────────────────────────────

    public record StartLiveSessionCommand(string InstructorId, string SessionId)
        : IRequest<Result<LiveSessionDto>>;

    public class StartLiveSessionHandler : IRequestHandler<StartLiveSessionCommand, Result<LiveSessionDto>>
    {
        private readonly ILiveSessionRepository _liveRepo;
        private readonly ILessonRepository _lessonRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<StartLiveSessionHandler> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public StartLiveSessionHandler(
            ILiveSessionRepository liveRepo,
            ILessonRepository lessonRepo,
            IEnrollmentRepository enrollmentRepo,
            INotificationService notificationService,
            ILogger<StartLiveSessionHandler> logger,
            IStringLocalizer<SharedResource> localizer)
        {
            _liveRepo = liveRepo;
            _lessonRepo = lessonRepo;
            _enrollmentRepo = enrollmentRepo;
            _notificationService = notificationService;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<Result<LiveSessionDto>> Handle(StartLiveSessionCommand request, CancellationToken ct)
        {
            var session = await _liveRepo.GetByIdAsync(request.SessionId, ct);
            if (session is null)
                return Result<LiveSessionDto>.Failure(_localizer["SessionNotFound"]);

            if (session.HostId != request.InstructorId)
                return Result<LiveSessionDto>.Failure(_localizer["AccessDenied"]);

            if (session.Status == "live")
                return Result<LiveSessionDto>.Failure(_localizer["SessionAlreadyLive"]);

            if (session.Status == "ended")
                return Result<LiveSessionDto>.Failure(_localizer["SessionAlreadyEnded"]);

            session.Status = "live";
            session.StartedAt = DateTime.UtcNow;
            await _liveRepo.UpdateAsync(session.Id, session, ct);

            var lesson = await _lessonRepo.GetByIdAsync(session.LessonId, ct);

            if (lesson is not null)
            {
                // Notify enrolled students — fire-and-forget, doesn't block the response
                _ = NotifyEnrolledStudentsAsync(lesson.CourseId, session, CancellationToken.None);
            }

            _logger.LogInformation("Live session started: {Id}", session.Id);

            return Result<LiveSessionDto>.Success(MapToDto(session));
        }

        private async Task NotifyEnrolledStudentsAsync(
            string courseId, LiveSession session, CancellationToken ct)
        {
            try
            {
                var enrollments = await _enrollmentRepo.GetAllAsync(e => e.CourseId == courseId, ct);

                var tasks = enrollments.Select(e => _notificationService.SendAsync(
                    userId: e.UserId,
                    title: $"🔴 Live session starting: {session.Title}",
                    message: _localizer["CourseSessionGoingLive"],
                    type: Domain.Enums.Content.NotificationType.LiveSessionStarting,
                    actionUrl: $"/live/{session.Id}",
                    ct: ct));

                await Task.WhenAll(tasks);

                _logger.LogInformation(
                    "Live session notifications sent to {Count} students for session {Id}",
                    enrollments.Count(), session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify students about live session {Id}", session.Id);
            }
        }

        private static LiveSessionDto MapToDto(LiveSession s) => new(
            s.Id, s.LessonId, s.Title, s.Description,
            s.HostId, s.ChannelName, s.Status,
            s.ViewerCount, s.AllowChat, s.AllowQuestions,
            s.ScheduledAt, s.StartedAt, s.EndedAt, s.WhiteboardUrl);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // END LIVE SESSION
    // Instructor clicks "Stop Stream" / "End & Save".
    // Uses ILiveNotifier (Domain abstraction) — Application never touches SignalR.
    // ─────────────────────────────────────────────────────────────────────────────

    public record EndLiveSessionCommand(
        string InstructorId,
        string SessionId,
        IFormFile? WhiteboardImage) : IRequest<Result<string>>;

    public class EndLiveSessionHandler : IRequestHandler<EndLiveSessionCommand, Result<string>>
    {
        private readonly ILiveSessionRepository _liveRepo;
        private readonly IFileStorageService _fileStorage;
        private readonly ILiveNotifier _liveNotifier;
        private readonly ILogger<EndLiveSessionHandler> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public EndLiveSessionHandler(
            ILiveSessionRepository liveRepo,
            IFileStorageService fileStorage,
            ILiveNotifier liveNotifier,
            ILogger<EndLiveSessionHandler> logger,
            IStringLocalizer<SharedResource> localizer)
        {
            _liveRepo = liveRepo;
            _fileStorage = fileStorage;
            _liveNotifier = liveNotifier;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<Result<string>> Handle(EndLiveSessionCommand request, CancellationToken ct)
        {
            var session = await _liveRepo.GetByIdAsync(request.SessionId, ct);
            if (session is null)
                return Result<string>.Failure(_localizer["SessionNotFound"]);

            if (session.HostId != request.InstructorId)
                return Result<string>.Failure(_localizer["AccessDenied"]);

            if (session.Status == "ended")
                return Result<string>.Failure(_localizer["SessionAlreadyEnded"]);

            string? whiteboardUrl = null;
            if (request.WhiteboardImage is not null && request.WhiteboardImage.Length > 0)
            {
                var fileName = $"whiteboard-{request.SessionId}-{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                await using var stream = request.WhiteboardImage.OpenReadStream();
                whiteboardUrl = await _fileStorage.UploadFileAsync(stream, fileName, "whiteboards");
            }

            await _liveRepo.EndSessionAsync(request.SessionId, whiteboardUrl, ct);
            await _liveNotifier.NotifySessionEndedAsync(request.SessionId, ct);

            _logger.LogInformation("Live session ended: {Id}", request.SessionId);

            return Result<string>.Success(whiteboardUrl ?? _localizer["SessionAlreadyEnded"]);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // SHARED DTO
    // ─────────────────────────────────────────────────────────────────────────────

    public record LiveSessionDto(
        string Id,
        string LessonId,
        string Title,
        string? Description,
        string HostId,
        string ChannelName,
        string Status,
        int ViewerCount,
        bool AllowChat,
        bool AllowQuestions,
        DateTime? ScheduledAt,
        DateTime? StartedAt,
        DateTime? EndedAt,
        string? WhiteboardUrl);
}