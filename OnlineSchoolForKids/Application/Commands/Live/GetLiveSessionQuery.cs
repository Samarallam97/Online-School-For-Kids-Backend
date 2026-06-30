using Application.DTOs;
using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Commands.Live
{
    public record GetLiveSessionQuery(string SessionId, string UserId)
     : IRequest<Result<LiveSessionDto>>;

    public class GetLiveSessionQueryHandler : IRequestHandler<GetLiveSessionQuery, Result<LiveSessionDto>>
    {
        private readonly ILiveSessionRepository _repo;

        public GetLiveSessionQueryHandler(ILiveSessionRepository repo) => _repo = repo;

        public async Task<Result<LiveSessionDto>> Handle(GetLiveSessionQuery request, CancellationToken ct)
        {
            var session = await _repo.GetByIdAsync(request.SessionId, ct);
            if (session is null)
                return Result<LiveSessionDto>.Failure("Session not found.");

            return Result<LiveSessionDto>.Success(new LiveSessionDto(
                session.Id,
                session.Title,
                session.Description,
                session.Category,
                session.HostId,
                session.ChannelName,
                session.Status,
                session.ViewerCount,
                session.AllowChat,
                session.AllowQuestions,
                session.ScheduledAt,
                session.StartedAt,
                session.EndedAt,
                session.WhiteboardUrl));
        }
    }

    // ── Get all live sessions (discover page) ────────────────────────────────────

    public record GetLiveSessionsQuery : IRequest<Result<IEnumerable<LiveSessionDto>>>;

    public class GetLiveSessionsQueryHandler
        : IRequestHandler<GetLiveSessionsQuery, Result<IEnumerable<LiveSessionDto>>>
    {
        private readonly ILiveSessionRepository _repo;

        public GetLiveSessionsQueryHandler(ILiveSessionRepository repo) => _repo = repo;

        public async Task<Result<IEnumerable<LiveSessionDto>>> Handle(
            GetLiveSessionsQuery request, CancellationToken ct)
        {
            var sessions = await _repo.GetLiveSessionsAsync(ct);

            var dtos = sessions.Select(s => new LiveSessionDto(
                s.Id, s.Title, s.Description, s.Category,
                s.HostId, s.ChannelName, s.Status,
                s.ViewerCount, s.AllowChat, s.AllowQuestions,
                s.ScheduledAt, s.StartedAt, s.EndedAt, s.WhiteboardUrl));

            return Result<IEnumerable<LiveSessionDto>>.Success(dtos);
        }
    }

    // ── Get chat history for a session ───────────────────────────────────────────

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
                m.Id, m.SessionId, m.SenderId, m.SenderName,
                m.SenderAvatar, m.Content, m.IsHost, m.CreatedAt));

            return Result<IEnumerable<ChatMessageDto>>.Success(dtos);
        }
    }

}
