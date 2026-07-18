using Application.Commands.Live;
using Application.DTOs;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Queries.Live;

// ── Get single session — gated by enrollment ──────────────────────────────────

public record GetLiveSessionQuery(string SessionId, string UserId)
    : IRequest<Result<LiveSessionDto>>;

public class GetLiveSessionQueryHandler : IRequestHandler<GetLiveSessionQuery, Result<LiveSessionDto>>
{
    private readonly ILiveSessionRepository _liveRepo;
    private readonly ILessonRepository _lessonRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;

    public GetLiveSessionQueryHandler(
        ILiveSessionRepository liveRepo,
        ILessonRepository lessonRepo,
        ICourseRepository courseRepo,
        IEnrollmentRepository enrollmentRepo)
    {
        _liveRepo = liveRepo;
        _lessonRepo = lessonRepo;
        _courseRepo = courseRepo;
        _enrollmentRepo = enrollmentRepo;
    }

    public async Task<Result<LiveSessionDto>> Handle(GetLiveSessionQuery request, CancellationToken ct)
    {
        var session = await _liveRepo.GetByIdAsync(request.SessionId, ct);
        if (session is null)
            return Result<LiveSessionDto>.Failure("Session not found.");

        var lesson = await _lessonRepo.GetByIdAsync(session.LessonId, ct);
        if (lesson is null)
            return Result<LiveSessionDto>.Failure("Lesson not found.");

        var course = await _courseRepo.GetByIdAsync(lesson.CourseId, ct);
        if (course is null)
            return Result<LiveSessionDto>.Failure("Course not found.");

        // Access check: must be the instructor OR an enrolled student.
        // This is the core restriction that replaces "anyone with the link".
        var isInstructor = course.InstructorId == request.UserId;

        if (!isInstructor)
        {
            var enrollment = await _enrollmentRepo.GetByUserAndCourseAsync(
                request.UserId, lesson.CourseId, ct);

            if (enrollment is null)
                return Result<LiveSessionDto>.Failure("You are not enrolled in this course.");
        }

        return Result<LiveSessionDto>.Success(new LiveSessionDto(
            session.Id, session.LessonId, session.Title, session.Description,
            session.HostId, session.ChannelName, session.Status,
            session.ViewerCount, session.AllowChat, session.AllowQuestions,
            session.ScheduledAt, session.StartedAt, session.EndedAt, session.WhiteboardUrl));
    }
}

// ── Get chat history ──────────────────────────────────────────────────────────

public record GetChatHistoryQuery(string SessionId)
    : IRequest<Result<IEnumerable<ChatMessageDto>>>;

public record ChatMessageDto(
    string Id,
    string SessionId,
    string UserId,
    string Username,
    string? AvatarUrl,
    string Content,
    bool IsHost,
    DateTime CreatedAt);

public class GetChatHistoryQueryHandler
    : IRequestHandler<GetChatHistoryQuery, Result<IEnumerable<ChatMessageDto>>>
{
    private readonly IChatMessageRepository _repo;
    public GetChatHistoryQueryHandler(IChatMessageRepository repo) => _repo = repo;

    public async Task<Result<IEnumerable<ChatMessageDto>>> Handle(
        GetChatHistoryQuery request, CancellationToken ct)
    {
        var messages = await _repo.GetBySessionIdAsync(request.SessionId, ct);

        var dtos = messages.Select(m => new ChatMessageDto(
            m.Id, m.ContextId, m.SenderId, m.SenderName,
            m.SenderAvatar, m.Content, m.IsHost, m.CreatedAt));

        return Result<IEnumerable<ChatMessageDto>>.Success(dtos);
    }
}

