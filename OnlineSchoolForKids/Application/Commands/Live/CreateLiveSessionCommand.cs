using Application.DTOs;
using Domain.Entities.Live;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Services.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Live
{
    public record CreateLiveSessionCommand(
    string HostId,
    string Title,
    string? Description,
    string? Category,
    bool AllowChat,
    bool AllowQuestions,
    DateTime? ScheduledAt) : IRequest<Result<LiveSessionDto>>;

    public class CreateLiveSessionHandler : IRequestHandler<CreateLiveSessionCommand, Result<LiveSessionDto>>
    {
        private readonly ILiveSessionRepository _repo;
        private readonly ILogger<CreateLiveSessionHandler> _logger;

        public CreateLiveSessionHandler(
            ILiveSessionRepository repo,
            ILogger<CreateLiveSessionHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Result<LiveSessionDto>> Handle(
            CreateLiveSessionCommand request, CancellationToken ct)
        {
            var channelName = $"live_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";

            var session = new LiveSession
            {
                HostId = request.HostId,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                ChannelName = channelName,
                Status = request.ScheduledAt.HasValue ? "scheduled" : "live",
                AllowChat = request.AllowChat,
                AllowQuestions = request.AllowQuestions,
                ScheduledAt = request.ScheduledAt,
                StartedAt = request.ScheduledAt.HasValue ? null : DateTime.UtcNow
            };

            await _repo.CreateAsync(session, ct);

            _logger.LogInformation(
                "Live session created: {Id} by host {HostId} — \"{Title}\"",
                session.Id, request.HostId, request.Title);

            return Result<LiveSessionDto>.Success(MapToDto(session));
        }

        private static LiveSessionDto MapToDto(LiveSession s) => new(
            s.Id, s.Title, s.Description, s.Category,
            s.HostId, s.ChannelName, s.Status,
            s.ViewerCount, s.AllowChat, s.AllowQuestions,
            s.ScheduledAt, s.StartedAt, s.EndedAt, s.WhiteboardUrl);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // END LIVE SESSION
    // Called when the host clicks "Stop Stream" in GoLivePage.tsx
    //
    // Uses ILiveNotifier (Domain abstraction) instead of IHubContext directly,
    // so Application never depends on SignalR — Infrastructure provides the
    // real implementation via DI.
    // ─────────────────────────────────────────────────────────────────────────────

    public record EndLiveSessionCommand(
        string HostId,
        string SessionId,
        IFormFile? WhiteboardImage) : IRequest<Result<string>>;

    public class EndLiveSessionHandler : IRequestHandler<EndLiveSessionCommand, Result<string>>
    {
        private readonly ILiveSessionRepository _repo;
        private readonly IFileStorageService _fileStorage;
        private readonly ILiveNotifier _liveNotifier;
        private readonly ILogger<EndLiveSessionHandler> _logger;

        public EndLiveSessionHandler(
            ILiveSessionRepository repo,
            IFileStorageService fileStorage,
            ILiveNotifier liveNotifier,
            ILogger<EndLiveSessionHandler> logger)
        {
            _repo = repo;
            _fileStorage = fileStorage;
            _liveNotifier = liveNotifier;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(EndLiveSessionCommand request, CancellationToken ct)
        {
            var session = await _repo.GetByIdAsync(request.SessionId, ct);
            if (session is null)
                return Result<string>.Failure("Session not found.");

            if (session.HostId != request.HostId)
                return Result<string>.Failure("Access denied.");

            if (session.Status == "ended")
                return Result<string>.Failure("Session already ended.");

            string? whiteboardUrl = null;
            if (request.WhiteboardImage is not null && request.WhiteboardImage.Length > 0)
            {
                var fileName = $"whiteboard-{request.SessionId}-{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                await using var stream = request.WhiteboardImage.OpenReadStream();
                whiteboardUrl = await _fileStorage.UploadFileAsync(stream, fileName, "whiteboards");
            }

            await _repo.EndSessionAsync(request.SessionId, whiteboardUrl, ct);

            // Broadcast via the abstraction — Infrastructure's LiveNotifier
            // handles the actual SignalR call.
            await _liveNotifier.NotifySessionEndedAsync(request.SessionId, ct);

            _logger.LogInformation("Live session ended: {Id}", request.SessionId);

            return Result<string>.Success(whiteboardUrl ?? "Session ended.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // SHARED DTO
    // ─────────────────────────────────────────────────────────────────────────────

    public record LiveSessionDto(
        string Id,
        string Title,
        string? Description,
        string? Category,
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