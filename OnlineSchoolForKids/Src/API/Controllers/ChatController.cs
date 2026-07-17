using API.Hubs;
using Application.DTOs;
using Domain.Entities.Chat;
using Domain.Interfaces.Repositories.Users;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]

[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatRepository _repo;
    private readonly IUserRepository _users;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IConfiguration _config;

    public ChatController(
        ChatRepository repo,
        IUserRepository users,
        IHubContext<ChatHub> hub,
        IConfiguration config)
    {
        _repo  = repo;
        _users = users;
        _hub   = hub;
        _config=config;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CONVERSATIONS  (1-to-1)
    // ═══════════════════════════════════════════════════════════════════════════

    /// GET /api/chat/conversations  → list user's DM conversations
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var me = UserId();
        var convs = await _repo.GetUserConversationsAsync(me, ct);

        // Batch-load the other participants
        var otherIds = convs.Select(c => c.ParticipantAId == me
            ? c.ParticipantBId : c.ParticipantAId).ToList();
        var users = await _users.GetManyByIdsAsync(otherIds, ct);
        var userMap = users.ToDictionary(u => u.Id);

        var result = convs.Select(c =>
        {
            var otherId = c.ParticipantAId == me ? c.ParticipantBId : c.ParticipantAId;
            userMap.TryGetValue(otherId, out var other);
            return new ConversationDto
            {
                Id                = c.Id,
                ParticipantId     = otherId,
                ParticipantName   = other?.FullName   ?? "Unknown",
                ParticipantAvatar = other?.ProfilePictureUrl,
                LastMessage       = c.LastMessageContent ?? "",
                LastMessageAt     = c.LastMessageAt,
                UnreadCount       = c.UnreadCounts.TryGetValue(me, out var n) ? n : 0,
                IsOnline          = ChatHub.IsOnline(otherId),
            };
        });

        return Ok(result);
    }

    /// POST /api/chat/conversations  → start or return existing conversation
    [HttpPost("conversations")]
    public async Task<IActionResult> StartConversation(
        [FromBody] StartConversationRequest req, CancellationToken ct)
    {
        var me = UserId();
        var conv = await _repo.GetOrCreateConversationAsync(me, req.OtherUserId, ct);

        var other = await _users.GetByIdAsync(req.OtherUserId, ct);
        return Ok(new ConversationDto
        {
            Id                = conv.Id,
            ParticipantId     = req.OtherUserId,
            ParticipantName   = other?.FullName         ?? "Unknown",
            ParticipantAvatar = other?.ProfilePictureUrl,
            LastMessage       = conv.LastMessageContent ?? "",
            LastMessageAt     = conv.LastMessageAt,
            UnreadCount       = 0,
            IsOnline          = ChatHub.IsOnline(req.OtherUserId),
        });
    }

    /// DELETE /api/chat/conversations/{id}  → delete a conversation (both sides)
    [HttpDelete("conversations/{id}")]
    public async Task<IActionResult> DeleteConversation(string id, CancellationToken ct)
    {
        var me = UserId();
        var conv = await _repo.GetConversationByIdAsync(id, ct);
        if (conv is null) return NotFound(new { message = "Conversation not found" });

        if (conv.ParticipantAId != me && conv.ParticipantBId != me)
            return Forbid();

        var ok = await _repo.DeleteConversationAsync(id, ct);
        if (!ok) return NotFound(new { message = "Conversation not found" });

        var otherId = conv.ParticipantAId == me ? conv.ParticipantBId : conv.ParticipantAId;
        await _hub.Clients.User(otherId).SendAsync("ConversationDeleted", new { conversationId = id });

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GROUPS
    // ═══════════════════════════════════════════════════════════════════════════

    /// GET /api/chat/groups  → list user's groups
    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups(CancellationToken ct)
    {
        var me = UserId();
        var groups = await _repo.GetUserGroupsAsync(me, ct);

        var dtos = groups.Select(g =>
        {
            var member = g.Members.FirstOrDefault(m => m.UserId == me);
            return new GroupDto
            {
                Id           = g.Id,
                Name         = g.Name,
                Description  = g.Description,
                AvatarUrl    = g.AvatarUrl,
                CourseId     = g.CourseId,
                MembersCount = g.Members.Count,
                LastMessage  = g.LastMessageContent ?? "",
                LastMessageAt = g.LastMessageAt,
                UnreadCount  = member?.UnreadCount ?? 0,
                MyRole       = member?.Role.ToString().ToLower() ?? "member",
            };
        });

        return Ok(dtos);
    }

    /// POST /api/chat/groups  → create a new group
    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupRequest req, CancellationToken ct)
    {
        var me = UserId();
        var self = await _users.GetByIdAsync(me, ct);

        // Build members list — include the creator as Owner
        var memberIds = req.MemberIds.Distinct().ToList();
        if (!memberIds.Contains(me)) memberIds.Add(me);

        var users = await _users.GetManyByIdsAsync(memberIds, ct);
        var members = users.Select(u => new GroupMember
        {
            UserId    = u.Id,
            UserName  = u.FullName,
            UserAvatar = u.ProfilePictureUrl,
            Role      = u.Id == me ? GroupRole.Owner : GroupRole.Member,
        }).ToList();

        var group = await _repo.CreateGroupAsync(new Group
        {
            Name          = req.Name,
            Description   = req.Description,
            CourseId      = req.CourseId,
            CreatedByUserId = me,
            Members       = members,
        }, ct);

        // Notify all members via SignalR
        foreach (var m in members.Where(m => m.UserId != me))
            await _hub.Clients.User(m.UserId).SendAsync("AddedToGroup", new GroupDto
            {
                Id           = group.Id,
                Name         = group.Name,
                Description  = group.Description,
                MembersCount = group.Members.Count,
                MyRole       = "member",
            });

        return Ok(new GroupDto
        {
            Id           = group.Id,
            Name         = group.Name,
            Description  = group.Description,
            CourseId     = group.CourseId,
            MembersCount = group.Members.Count,
            MyRole       = "owner",
        });
    }

    /// GET /api/chat/groups/{groupId}/members
    [HttpGet("groups/{groupId}/members")]
    public async Task<IActionResult> GetGroupMembers(string groupId, CancellationToken ct)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();
        if (!group.Members.Any(m => m.UserId == UserId())) return Forbid();

        var dtos = group.Members.Select(m => new GroupMemberDto(
            m.UserId, m.UserName, m.UserAvatar,
            m.Role.ToString().ToLower(),
            ChatHub.IsOnline(m.UserId)));

        return Ok(dtos);
    }

    /// POST /api/chat/groups/{groupId}/members
    [HttpPost("groups/{groupId}/members")]
    public async Task<IActionResult> AddMember(
        string groupId, [FromBody] AddGroupMemberRequest req, CancellationToken ct)
    {
        var me = UserId();
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();


        var caller = group.Members.FirstOrDefault(m => m.UserId == me);
        if (caller is null || caller.Role == GroupRole.Member) return Forbid();
        if (!HasPermission(caller, group.WhoCanAddMembers)) return Forbid();

        if (group.Members.Any(m => m.UserId == req.UserId))
            return Conflict(new { message = "User is already a member" });

        var user = await _users.GetByIdAsync(req.UserId, ct);
        if (user is null) return NotFound(new { message = "User not found" });

        var member = new GroupMember
        {
            UserId    = user.Id,
            UserName  = user.FullName,
            UserAvatar = user.ProfilePictureUrl,
        };

        await _repo.AddGroupMemberAsync(groupId, member, ct);
        await _hub.Clients.User(req.UserId).SendAsync("AddedToGroup", new { groupId, groupName = group.Name });

        return Ok();
    }

    /// DELETE /api/chat/groups/{groupId}/members/{userId}
    [HttpDelete("groups/{groupId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(
        string groupId, string userId, CancellationToken ct)
    {
        var me = UserId();
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();

        var caller = group.Members.FirstOrDefault(m => m.UserId == me);
        // Can remove self (leave), or admin/owner can remove others
        if (userId != me && (caller is null || caller.Role == GroupRole.Member)) return Forbid();

        await _repo.RemoveGroupMemberAsync(groupId, userId, ct);
        await _hub.Clients.User(userId).SendAsync("RemovedFromGroup", new { groupId });

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MESSAGES  (shared endpoint — contextType=dm|group)
    // ═══════════════════════════════════════════════════════════════════════════

    /// GET /api/chat/conversations/{id}/messages?cursor=&pageSize=
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetDmMessages(
        string conversationId,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 30,
        CancellationToken ct = default)
    {
        var (items, hasMore) = await _repo.GetMessagesAsync(conversationId, pageSize, cursor, ct);
        var dtos = items.Select(ToDto).ToList();
        return Ok(new PagedResult<ChatMessageDto>(
            dtos, hasMore, hasMore ? items.First().CreatedAt.ToString("O") : null));
    }

    [HttpGet("groups/{groupId}/messages")]
    public async Task<IActionResult> GetGroupMessages(
    string groupId,
    [FromQuery] string? cursor = null,
    [FromQuery] int pageSize = 30,
    CancellationToken ct = default)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();
        var me = UserId();
        var caller = group.Members.FirstOrDefault(m => m.UserId == me);
        if (caller is null) return Forbid();

        var (items, hasMore) = await _repo.GetMessagesAsync(groupId, pageSize, cursor, ct);

        // If the group hides history from new members, clip anything before they joined
        if (!group.ShowOldChatToNewMembers)
        {
            items = items.Where(m => m.CreatedAt >= caller.JoinedAt).ToList();
            hasMore = hasMore && items.Count > 0; // stop "load earlier" once we hit the join boundary
        }

        return Ok(new PagedResult<ChatMessageDto>(
            items.Select(ToDto).ToList(),
            hasMore,
            hasMore ? items.FirstOrDefault()?.CreatedAt.ToString("O") : null));
    }

    /// POST /api/chat/conversations/{id}/messages
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendDmMessage(
        string conversationId,
        [FromBody] SendMessageRequest req,
        CancellationToken ct)
    {
        var me = UserId();
        var self = await _users.GetByIdAsync(me, ct);

        // Validate conversation membership
        var convs = await _repo.GetUserConversationsAsync(me, ct);
        var conv = convs.FirstOrDefault(c => c.Id == conversationId);
        if (conv is null) return Forbid();

        var otherId = conv.ParticipantAId == me ? conv.ParticipantBId : conv.ParticipantAId;

        ChatMessage? replyMsg = null;
        if (!string.IsNullOrEmpty(req.ReplyToMessageId))
            replyMsg = await _repo.GetMessageAsync(req.ReplyToMessageId, ct);

        var msg = await _repo.SaveMessageAsync(new ChatMessage
        {
            ContextId   = conversationId,
            ContextType = ChatContext.DirectMessage,
            SenderId    = me,
            SenderName  = self?.FullName ?? "Unknown",
            SenderAvatar = self?.ProfilePictureUrl,
            Content     = req.Content,
            Type        = ParseType(req.Type),
            FileUrl     = req.FileUrl,
            FileName    = req.FileName,
            ReadBy      = new List<string> { me },
            ReplyToMessageId      = replyMsg?.Id,
            ReplyToMessageContent = replyMsg?.Content,
            ReplyToSenderName     = replyMsg?.SenderName,
        }, ct);

        await _repo.UpdateConversationLastMessageAsync(
            conversationId, req.Content, msg.CreatedAt, me, otherId, ct);

        var dto = ToDto(msg);

        // Push to both participants via SignalR
        await _hub.Clients.User(otherId).SendAsync("NewMessage", dto);
        await _hub.Clients.User(me).SendAsync("NewMessage", dto);

        return Ok(dto);
    }

    /// POST /api/chat/groups/{id}/messages
    [HttpPost("groups/{groupId}/messages")]
    public async Task<IActionResult> SendGroupMessage(
        string groupId,
        [FromBody] SendMessageRequest req,
        CancellationToken ct)
    {
        var me = UserId();
        var self = await _users.GetByIdAsync(me, ct);
        var group = await _repo.GetGroupAsync(groupId, ct);

        if (group is null) return NotFound();
        if (!group.Members.Any(m => m.UserId == me)) return Forbid();

        ChatMessage? replyMsg = null;
        if (!string.IsNullOrEmpty(req.ReplyToMessageId))
            replyMsg = await _repo.GetMessageAsync(req.ReplyToMessageId, ct);

        var msg = await _repo.SaveMessageAsync(new ChatMessage
        {
            ContextId   = groupId,
            ContextType = ChatContext.Group,
            SenderId    = me,
            SenderName  = self?.FullName ?? "Unknown",
            SenderAvatar = self?.ProfilePictureUrl,
            Content     = req.Content,
            Type        = ParseType(req.Type),
            FileUrl     = req.FileUrl,
            FileName    = req.FileName,
            ReplyToMessageId      = replyMsg?.Id,
            ReplyToMessageContent = replyMsg?.Content,
            ReplyToSenderName     = replyMsg?.SenderName,
        }, ct);

        await _repo.UpdateGroupLastMessageAsync(groupId, req.Content, msg.CreatedAt, ct);

        var dto = ToDto(msg);

        // Broadcast to everyone in the SignalR group room
        await _hub.Clients.Group($"group:{groupId}").SendAsync("NewMessage", dto);

        return Ok(dto);
    }

    /// PUT /api/chat/messages/{id}
    [HttpPut("messages/{messageId}")]
    public async Task<IActionResult> EditMessage(
        string messageId,
        [FromBody] EditMessageRequest req,
        CancellationToken ct)
    {
        var msg = await _repo.GetMessageAsync(messageId, ct);
        if (msg is null) return NotFound();
        if (msg.SenderId != UserId()) return Forbid();

        await _repo.EditMessageAsync(messageId, req.NewContent, ct);
        msg.Content  = req.NewContent;
        msg.IsEdited = true;

        var dto = ToDto(msg);
        var room = msg.ContextType == ChatContext.Group
            ? $"group:{msg.ContextId}" : null;

        if (room is not null)
            await _hub.Clients.Group(room).SendAsync("MessageEdited", dto);
        else
        {
            var convs = await _repo.GetUserConversationsAsync(UserId(), ct);
            var conv = convs.FirstOrDefault(c => c.Id == msg.ContextId);
            if (conv is not null)
            {
                var other = conv.ParticipantAId == UserId()
                    ? conv.ParticipantBId : conv.ParticipantAId;
                await _hub.Clients.User(other).SendAsync("MessageEdited", dto);
                await _hub.Clients.User(UserId()).SendAsync("MessageEdited", dto);
            }
        }

        return Ok(dto);
    }

    /// DELETE /api/chat/messages/{id}
    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(string messageId, CancellationToken ct)
    {
        var msg = await _repo.GetMessageAsync(messageId, ct);
        if (msg is null) return NotFound();
        if (msg.SenderId != UserId()) return Forbid();

        await _repo.DeleteMessageAsync(messageId, ct);

        var payload = new { messageId, contextId = msg.ContextId };
        if (msg.ContextType == ChatContext.Group)
            await _hub.Clients.Group($"group:{msg.ContextId}").SendAsync("MessageDeleted", payload);
        else
        {
            var convs = await _repo.GetUserConversationsAsync(UserId(), ct);
            var conv = convs.FirstOrDefault(c => c.Id == msg.ContextId);
            if (conv is not null)
            {
                var other = conv.ParticipantAId == UserId()
                    ? conv.ParticipantBId : conv.ParticipantAId;
                await _hub.Clients.User(other).SendAsync("MessageDeleted", payload);
                await _hub.Clients.User(UserId()).SendAsync("MessageDeleted", payload);
            }
        }

        return NoContent();
    }

    /// POST /api/chat/messages/{id}/reactions
    [HttpPost("messages/{messageId}/reactions")]
    public async Task<IActionResult> ToggleReaction(
        string messageId, [FromBody] string emoji, CancellationToken ct)
    {
        var me = UserId();
        var msg = await _repo.GetMessageAsync(messageId, ct);
        if (msg is null) return NotFound();

        await _repo.ToggleReactionAsync(messageId, emoji, me, ct);

        var updatedMsg = await _repo.GetMessageAsync(messageId, ct);
        var payload = new { messageId, reactions = updatedMsg!.Reactions };

        if (msg.ContextType == ChatContext.Group)
            await _hub.Clients.Group($"group:{msg.ContextId}").SendAsync("ReactionsUpdated", payload);
        else
        {
            var convs = await _repo.GetUserConversationsAsync(me, ct);
            var conv = convs.FirstOrDefault(c => c.Id == msg.ContextId);
            if (conv is not null)
            {
                var other = conv.ParticipantAId == me ? conv.ParticipantBId : conv.ParticipantAId;
                await _hub.Clients.User(other).SendAsync("ReactionsUpdated", payload);
                await _hub.Clients.User(me).SendAsync("ReactionsUpdated", payload);
            }
        }

        return Ok(updatedMsg!.Reactions);
    }


    // ── GET /api/chat/groups/{groupId}/settings — owner/admin can view full settings ──
    [HttpGet("groups/{groupId}/settings")]
    public async Task<IActionResult> GetGroupSettings(string groupId, CancellationToken ct)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();
        var caller = group.Members.FirstOrDefault(m => m.UserId == UserId());
        if (caller is null) return Forbid();

        return Ok(new GroupSettingsDto(
            group.Name,
            group.Description,
            group.AvatarUrl,
            PermissionToString(group.WhoCanInvite),
            PermissionToString(group.WhoCanAddMembers),
            group.ShowOldChatToNewMembers,
            group.InviteLinkEnabled
        ));
    }

    // ── PUT /api/chat/groups/{groupId}/settings — owner only ──────────────────
    [HttpPut("groups/{groupId}/settings")]
    public async Task<IActionResult> UpdateGroupSettings(
        string groupId, [FromBody] UpdateGroupSettingsRequest req, CancellationToken ct)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();
        var caller = group.Members.FirstOrDefault(m => m.UserId == UserId());
        if (caller is null || caller.Role != GroupRole.Owner)
            return Forbid(); // only the owner can change settings

        if (req.Name is not null) group.Name = req.Name;
        group.Description = req.Description ?? group.Description;
        group.AvatarUrl = req.AvatarUrl ?? group.AvatarUrl;
        if (req.WhoCanInvite is not null) group.WhoCanInvite = ParsePermission(req.WhoCanInvite);
        if (req.WhoCanAddMembers is not null) group.WhoCanAddMembers = ParsePermission(req.WhoCanAddMembers);
        if (req.ShowOldChatToNewMembers.HasValue) group.ShowOldChatToNewMembers = req.ShowOldChatToNewMembers.Value;
        if (req.InviteLinkEnabled.HasValue) group.InviteLinkEnabled = req.InviteLinkEnabled.Value;

        await _repo.UpdateGroupAsync(group, ct);

        await _hub.Clients.Group($"group:{groupId}").SendAsync("GroupSettingsUpdated", new
        {
            groupId,
            name = group.Name,
            description = group.Description,
            avatarUrl = group.AvatarUrl,
        });

        return Ok(new GroupSettingsDto(
            group.Name, group.Description, group.AvatarUrl,
            PermissionToString(group.WhoCanInvite), PermissionToString(group.WhoCanAddMembers),
            group.ShowOldChatToNewMembers, group.InviteLinkEnabled));
    }

    // ── DELETE /api/chat/groups/{groupId} — owner only, deletes the group entirely ──
    [HttpDelete("groups/{groupId}")]
    public async Task<IActionResult> DeleteGroup(string groupId, CancellationToken ct)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();
        var caller = group.Members.FirstOrDefault(m => m.UserId == UserId());
        if (caller is null || caller.Role != GroupRole.Owner) return Forbid();

        await _repo.DeleteGroupAsync(groupId, ct);

        // Notify everyone so their client removes the group from the sidebar
        await _hub.Clients.Group($"group:{groupId}").SendAsync("GroupDeleted", new { groupId });

        return NoContent();
    }

    // ── POST /api/chat/groups/{groupId}/invite-link — (re)generate the invite code ──
    [HttpPost("groups/{groupId}/invite-link")]
    public async Task<IActionResult> RegenerateInviteLink(string groupId, CancellationToken ct)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();
        var caller = group.Members.FirstOrDefault(m => m.UserId == UserId());
        if (!HasPermission(caller, group.WhoCanInvite)) return Forbid();

        group.InviteCode = Guid.NewGuid().ToString("N")[..10];
        group.InviteLinkEnabled = true;
        await _repo.UpdateGroupAsync(group, ct);

        var baseUrl = _config["FrontUrl"] ?? "http://localhost:5173";
        return Ok(new InviteLinkDto(group.InviteCode, $"{baseUrl}/join/{group.InviteCode}", true));
    }

    // ── GET /api/chat/groups/{groupId}/invite-link — current invite link, if caller may share it ──
    [HttpGet("groups/{groupId}/invite-link")]
    public async Task<IActionResult> GetInviteLink(string groupId, CancellationToken ct)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();
        var caller = group.Members.FirstOrDefault(m => m.UserId == UserId());
        if (!HasPermission(caller, group.WhoCanInvite)) return Forbid();

        var baseUrl = _config["FrontUrl"] ?? "http://localhost:5173";
        return Ok(new InviteLinkDto(group.InviteCode, $"{baseUrl}/join/{group.InviteCode}", group.InviteLinkEnabled));
    }

    // ── GET /api/chat/groups/join/{inviteCode} — preview before joining (no auth requirement ideally, but kept under [Authorize] since whole controller is) ──
    [HttpGet("groups/join/{inviteCode}")]
    public async Task<IActionResult> PreviewInvite(string inviteCode, CancellationToken ct)
    {
        var group = await _repo.GetGroupByInviteCodeAsync(inviteCode, ct);
        if (group is null || !group.InviteLinkEnabled) return NotFound(new { message = "This invite link is invalid or has expired." });

        return Ok(new GroupPreviewDto(group.Id, group.Name, group.Description, group.AvatarUrl, group.Members.Count));
    }

    // ── POST /api/chat/groups/join/{inviteCode} — actually join ──
    [HttpPost("groups/join/{inviteCode}")]
    public async Task<IActionResult> JoinByInvite(string inviteCode, CancellationToken ct)
    {
        var group = await _repo.GetGroupByInviteCodeAsync(inviteCode, ct);
        if (group is null || !group.InviteLinkEnabled)
            return NotFound(new { message = "This invite link is invalid or has expired." });

        var me = UserId();
        if (group.Members.Any(m => m.UserId == me))
            return Ok(new GroupDto { Id = group.Id, Name = group.Name, MembersCount = group.Members.Count, MyRole = "member" });

        var self = await _users.GetByIdAsync(me, ct);
        var member = new GroupMember
        {
            UserId = me,
            UserName = self?.FullName ?? "Unknown",
            UserAvatar = self?.ProfilePictureUrl,
            Role = GroupRole.Member,
            JoinedAt = DateTime.UtcNow,
        };
        await _repo.AddGroupMemberAsync(group.Id, member, ct);

        await _hub.Clients.Group($"group:{group.Id}").SendAsync("MemberJoined", new GroupMemberDto(
            member.UserId, member.UserName, member.UserAvatar, "member", true));

        return Ok(new GroupDto { Id = group.Id, Name = group.Name, MembersCount = group.Members.Count + 1, MyRole = "member" });
    }

    // ── GET /api/chat/users/search?email= — used by "add member" flow ──
    [HttpGet("users/search")]
    public async Task<IActionResult> SearchUserByEmail([FromQuery] string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { message = "Email is required." });

        var user = await _users.GetByEmailAsync(email.Trim(), ct); // implement on IUserRepository if not present
        if (user is null)
            return Ok(new UserSearchResultDto("", "", email, null)); // empty Id signals "not found" to the client

        return Ok(new UserSearchResultDto(user.Id, user.FullName, user.Email, user.ProfilePictureUrl));
    }

    // ── POST /api/chat/groups/{groupId}/members/by-email — add member by email, with "not on system" handling ──
    [HttpPost("groups/{groupId}/members/by-email")]
    public async Task<IActionResult> AddMemberByEmail(
        string groupId, [FromBody] AddMemberByEmailRequest req, CancellationToken ct)
    {
        var me = UserId();
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();

        var caller = group.Members.FirstOrDefault(m => m.UserId == me);
        if (!HasPermission(caller, group.WhoCanAddMembers)) return Forbid();

        var user = await _users.GetByEmailAsync(req.Email.Trim(), ct);
        var baseUrl = _config["FrontUrl"] ?? "http://localhost:5173";

        if (user is null)
        {
            // Not on the platform — give the caller a shareable invite link instead
            return Ok(new AddMemberByEmailResponse(
                UserFound: false,
                Added: false,
                InviteShareUrl: $"{baseUrl}/join/{group.InviteCode}",
                Message: "This person doesn't have an account yet. Share the invite link below so they can join and be added."
            ));
        }

        if (group.Members.Any(m => m.UserId == user.Id))
            return Ok(new AddMemberByEmailResponse(true, false, null, "This user is already a member of the group."));

        var role = req.Role.ToLower() == "admin" ? GroupRole.Admin : GroupRole.Member;
        var member = new GroupMember
        {
            UserId = user.Id,
            UserName = user.FullName,
            UserAvatar = user.ProfilePictureUrl,
            Role = role,
            JoinedAt = DateTime.UtcNow,
        };

        await _repo.AddGroupMemberAsync(groupId, member, ct);
        await _hub.Clients.User(user.Id).SendAsync("AddedToGroup", new { groupId, groupName = group.Name });
        await _hub.Clients.Group($"group:{groupId}").SendAsync("MemberJoined", new GroupMemberDto(
            member.UserId, member.UserName, member.UserAvatar, role.ToString().ToLower(), ChatHub.IsOnline(user.Id)));

        return Ok(new AddMemberByEmailResponse(true, true, null, "Member added successfully."));
    }

    // ── PUT /api/chat/groups/{groupId}/members/{userId}/role — change a member's role (owner only) ──
    [HttpPut("groups/{groupId}/members/{userId}/role")]
    public async Task<IActionResult> ChangeMemberRole(
        string groupId, string userId, [FromBody] string role, CancellationToken ct)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return NotFound();
        var caller = group.Members.FirstOrDefault(m => m.UserId == UserId());
        if (caller is null || caller.Role != GroupRole.Owner) return Forbid();
        if (userId == UserId()) return BadRequest(new { message = "You can't change your own role." });

        var newRole = role.ToLower() switch
        {
            "admin" => GroupRole.Admin,
            "member" => GroupRole.Member,
            _ => GroupRole.Member,
        };

        await _repo.UpdateMemberRoleAsync(groupId, userId, newRole, ct);
        await _hub.Clients.Group($"group:{groupId}").SendAsync("MemberRoleChanged", new { groupId, userId, role = role.ToLower() });

        return NoContent();
    }
    // ── Helpers ───────────────────────────────────────────────────────────────

    private string UserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static MessageType ParseType(string t) => t.ToLower() switch
    {
        "image" => MessageType.Image,
        "file" => MessageType.File,
        _ => MessageType.Text,
    };

    private static ChatMessageDto ToDto(ChatMessage m) => new()
    {
        Id           = m.Id,
        ContextId    = m.ContextId,
        SenderId     = m.SenderId,
        SenderName   = m.SenderName,
        SenderAvatar = m.SenderAvatar,
        Content      = m.Content,
        Type         = m.Type.ToString().ToLower(),
        FileUrl      = m.FileUrl,
        FileName     = m.FileName,
        Timestamp    = m.CreatedAt,
        IsEdited     = m.IsEdited,
        IsDeleted    = m.IsDeleted,
        ReadBy       = m.ReadBy,
        Reactions    = m.Reactions
            .Select(r => new MessageReactionDto(r.Emoji, r.UserId)).ToList(),
        ReplyTo      = m.ReplyToMessageId is null ? null : new ReplyContextDto(
            m.ReplyToMessageId, m.ReplyToSenderName ?? "", m.ReplyToMessageContent ?? ""),
    };

    private static bool HasPermission(GroupMember? caller, GroupPermission required)
    {
        if (caller is null) return false;
        return required switch
        {
            GroupPermission.OwnerOnly => caller.Role == GroupRole.Owner,
            GroupPermission.AdminsAndOwner => caller.Role == GroupRole.Owner || caller.Role == GroupRole.Admin,
            GroupPermission.AllMembers => true,
            _ => false,
        };
    }

    private static GroupPermission ParsePermission(string? s) => s?.ToLower() switch
    {
        "owner" => GroupPermission.OwnerOnly,
        "admins" => GroupPermission.AdminsAndOwner,
        "all" => GroupPermission.AllMembers,
        _ => GroupPermission.AdminsAndOwner,
    };

    private static string PermissionToString(GroupPermission p) => p switch
    {
        GroupPermission.OwnerOnly => "owner",
        GroupPermission.AdminsAndOwner => "admins",
        GroupPermission.AllMembers => "all",
        _ => "admins",
    };

}