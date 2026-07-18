using Domain.Entities.Chat;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace API.Hubs
{
    [Authorize]
    public class LiveSessionHub : Hub
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ViewerInfo>>
            _sessions = new();

        private readonly ILiveSessionRepository _sessionRepo;
        private readonly IChatMessageRepository _chatRepo;
        private readonly ILessonRepository _lessonRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;

        public LiveSessionHub(
            ILiveSessionRepository sessionRepo,
            IChatMessageRepository chatRepo,
            ILessonRepository lessonRepo,
            ICourseRepository courseRepo,
            IEnrollmentRepository enrollmentRepo)
        {
            _sessionRepo = sessionRepo;
            _chatRepo = chatRepo;
            _lessonRepo = lessonRepo;
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
        }

        // ── JOIN ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by both the host and students when they open the live session page.
        /// Verifies enrollment/instructor status before joining the SignalR group.
        /// </summary>
        public async Task JoinSession(string sessionId, string username)
        {
            var userId = GetUserId();
            if (userId is null) return;

            var allowed = await IsAllowedAsync(sessionId, userId);
            if (!allowed)
            {
                await Clients.Caller.SendAsync("AccessDenied", "You are not enrolled in this course.");
                return;
            }

            var session = await _sessionRepo.GetByIdAsync(sessionId);
            var isHost = session?.HostId == userId;

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            var viewers = _sessions.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, ViewerInfo>());
            viewers[Context.ConnectionId] = new ViewerInfo(userId, username, isHost, null);

            await BroadcastViewersAsync(sessionId);
        }

        // ── LEAVE ─────────────────────────────────────────────────────────────────

        public async Task LeaveSession(string sessionId)
        {
            await RemoveViewerAsync(sessionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var (sessionId, viewers) in _sessions)
            {
                if (viewers.ContainsKey(Context.ConnectionId))
                {
                    await RemoveViewerAsync(sessionId);
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ── CHAT ──────────────────────────────────────────────────────────────────

        public async Task SendChatMessage(string sessionId, string content)
        {
            var userId = GetUserId();
            if (userId is null || string.IsNullOrWhiteSpace(content)) return;

            // Re-check the caller is still a recognized participant of this session
            var viewers = _sessions.GetValueOrDefault(sessionId);
            var sender = viewers?.GetValueOrDefault(Context.ConnectionId);
            if (sender is null) return; // not joined -> ignore silently

            var message = new ChatMessage
            {
                ContextId = sessionId,
                ContextType = ChatContext.LiveSession,
                SenderId = userId,
                SenderName = sender.Username,
                SenderAvatar = sender.AvatarUrl,
                Content = content.Trim(),
                Type = MessageType.Text,
                IsHost = sender.IsHost
            };

            await _chatRepo.CreateAsync(message);

            await Clients.Group(sessionId).SendAsync("ReceiveChatMessage", new
            {
                id = message.Id,
                sessionId = sessionId,
                userId = message.SenderId,
                username = message.SenderName,
                avatarUrl = message.SenderAvatar,
                content = message.Content,
                isHost = message.IsHost,
                createdAt = message.CreatedAt
            });
        }

        // ── WHITEBOARD ────────────────────────────────────────────────────────────

        /// <summary>Only the host should call this in practice; front-end enforces the UI,
        /// and a non-host stroke is harmless since it only reaches other viewers of
        /// the same enrolled-only group.</summary>
        public async Task SendStroke(string sessionId, object stroke)
        {
            var viewers = _sessions.GetValueOrDefault(sessionId);
            var sender = viewers?.GetValueOrDefault(Context.ConnectionId);

            if (sender is null || !sender.IsHost)
                return;
            await Clients.OthersInGroup(sessionId).SendAsync("ReceiveStroke", stroke);
        }

        public async Task ClearBoard(string sessionId)
        {
            var viewers = _sessions.GetValueOrDefault(sessionId);
            var sender = viewers?.GetValueOrDefault(Context.ConnectionId);

            if (sender is null || !sender.IsHost)
                return;
            await Clients.OthersInGroup(sessionId).SendAsync("BoardCleared");
        }

        // ── SESSION END ───────────────────────────────────────────────────────────

        public async Task EndSession(string sessionId)
        {
            var userId = GetUserId();
            if (userId is null) return;

            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session is null || session.HostId != userId) return;

            await _sessionRepo.EndSessionAsync(sessionId, null);
            await Clients.Group(sessionId).SendAsync("SessionEnded");

            _sessions.TryRemove(sessionId, out _);
        }

        // ── HELPERS ───────────────────────────────────────────────────────────────

        /// <summary>Instructor of the course OR an enrolled student. Same rule as the REST query.</summary>
        private async Task<bool> IsAllowedAsync(string sessionId, string userId)
        {
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session is null) return false;

            var lesson = await _lessonRepo.GetByIdAsync(session.LessonId);
            if (lesson is null) return false;

            var course = await _courseRepo.GetByIdAsync(lesson.CourseId);
            if (course is null) return false;

            if (course.InstructorId == userId) return true;

            var enrollment = await _enrollmentRepo.GetByUserAndCourseAsync(userId, lesson.CourseId);
            return enrollment is not null;
        }

        private async Task RemoveViewerAsync(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var viewers))
            {
                viewers.TryRemove(Context.ConnectionId, out _);

                if (viewers.IsEmpty)
                    _sessions.TryRemove(sessionId, out _);
                else
                    await BroadcastViewersAsync(sessionId);
            }
        }

        private async Task BroadcastViewersAsync(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var viewers)) return;

            var viewerList = viewers.Values.Select(v => new
            {
                id = v.Id,
                username = v.Username,
                isHost = v.IsHost,
                avatarUrl = v.AvatarUrl
            }).ToList();

            _ = _sessionRepo.UpdateViewerCountAsync(sessionId, viewerList.Count);

            await Clients.Group(sessionId).SendAsync("ViewersUpdated", new
            {
                viewers = viewerList,
                viewerCount = viewerList.Count
            });
        }

        private string? GetUserId() =>
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public record ViewerInfo(string Id, string Username, bool IsHost, string? AvatarUrl);
}
