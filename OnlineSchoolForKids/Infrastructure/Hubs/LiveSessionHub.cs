using Domain.Entities.Chat;
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
        // In-memory store: sessionId → { connectionId → ViewerInfo }
        // Shared across all hub instances via static ConcurrentDictionary.
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ViewerInfo>>
            _sessions = new();

        private readonly ILiveSessionRepository _sessionRepo;
        private readonly IChatMessageRepository _chatRepo;

        public LiveSessionHub(
            ILiveSessionRepository sessionRepo,
            IChatMessageRepository chatRepo)
        {
            _sessionRepo = sessionRepo;
            _chatRepo = chatRepo;
        }

        // ── JOIN ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by both the host and viewers when they open the session page.
        /// Adds them to the SignalR group and updates the viewer list.
        /// </summary>
        public async Task JoinSession(string sessionId, string username, bool isHost)
        {
            var userId = GetUserId();
            if (userId is null) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            var viewers = _sessions.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, ViewerInfo>());

            viewers[Context.ConnectionId] = new ViewerInfo(
                Id: userId,
                Username: username,
                IsHost: isHost,
                AvatarUrl: null);

            // Broadcast updated viewer list + count to everyone in the session
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
            // Find which session this connection belonged to and clean up
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

        /// <summary>
        /// Called when any user sends a chat message.
        /// Saves it to MongoDB and broadcasts to everyone in the session.
        /// </summary>
        public async Task SendChatMessage(string sessionId, string content)
        {
            var userId = GetUserId();
            if (userId is null) return;

            if (string.IsNullOrWhiteSpace(content)) return;

            // Get sender info from the viewer list
            var viewers = _sessions.GetValueOrDefault(sessionId);
            var sender = viewers?.GetValueOrDefault(Context.ConnectionId);
            var username = sender?.Username ?? "Guest";
            var isHost = sender?.IsHost ?? false;

            // Persist to MongoDB
            var message = new ChatMessage
            {
                SessionId = sessionId,
                SenderId = userId,
                SenderName = username,
                SenderAvatar = sender?.AvatarUrl,
                Content = content.Trim(),
                IsHost = isHost
            };

            await _chatRepo.CreateAsync(message);

            // Broadcast to everyone in the session (including sender, for consistency)
            await Clients.Group(sessionId).SendAsync("ReceiveChatMessage", new
            {
                id = message.Id,
                sessionId = sessionId,
                userId = userId,
                username = username,
                avatarUrl = sender?.AvatarUrl,
                content = message.Content,
                isHost = isHost,
                createdAt = message.CreatedAt
            });
        }

        // ── WHITEBOARD ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the HOST only when they draw a stroke.
        /// Broadcasts the stroke to all viewers so their canvases update in real-time.
        ///
        /// stroke shape:
        /// {
        ///   type:   "path" | "line" | "rect" | "circle" | "text" | "clear",
        ///   color:  "#ff0000",
        ///   width:  2,
        ///   points: [[x,y], ...]   // for path/line
        ///   x, y, w, h             // for shapes
        ///   text:   "..."          // for text
        /// }
        /// </summary>
        public async Task SendStroke(string sessionId, object stroke)
        {
            await Clients
                .OthersInGroup(sessionId)
                .SendAsync("ReceiveStroke", stroke);
        }

        /// <summary>Called by host to clear the board for everyone.</summary>
        public async Task ClearBoard(string sessionId)
        {
            await Clients
                .OthersInGroup(sessionId)
                .SendAsync("BoardCleared");
        }

        // ── SESSION END ───────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the host when they click "Stop Stream".
        /// Marks the session as ended in MongoDB and broadcasts to all viewers
        /// so LiveSessionPage.tsx navigates them away automatically.
        /// </summary>
        public async Task EndSession(string sessionId, string? whiteboardUrl = null)
        {
            var userId = GetUserId();
            if (userId is null) return;

            // Verify caller is the host
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session is null || session.HostId != userId) return;

            // Persist ended status + whiteboard URL
            await _sessionRepo.EndSessionAsync(sessionId, whiteboardUrl);

            // Broadcast to ALL viewers — LiveSessionPage.tsx listens for "SessionEnded"
            // and redirects to home, matching the Supabase realtime behaviour in the original
            await Clients.Group(sessionId).SendAsync("SessionEnded");

            // Clean up in-memory presence for this session
            _sessions.TryRemove(sessionId, out _);
        }

        // ── HELPERS ───────────────────────────────────────────────────────────────

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

            // Update viewer count in MongoDB (fire-and-forget)
            _ = _sessionRepo.UpdateViewerCountAsync(sessionId, viewerList.Count);

            // Broadcast updated list + count to everyone
            await Clients.Group(sessionId).SendAsync("ViewersUpdated", new
            {
                viewers = viewerList,
                viewerCount = viewerList.Count
            });
        }

        private string? GetUserId() =>
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>Viewer presence record stored in memory during the session.</summary>
    public record ViewerInfo(string Id, string Username, bool IsHost, string? AvatarUrl);

}



