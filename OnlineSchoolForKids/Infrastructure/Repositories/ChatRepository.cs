using Domain.Entities.Chat;
using Infrastructure.Data;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;


public class ChatRepository
{
    private readonly IMongoCollection<ChatMessage> _messages;
    private readonly IMongoCollection<Conversation> _conversations;
    private readonly IMongoCollection<Group> _groups;

    public ChatRepository(MongoDbContext ctx)
    {
        _messages      = ctx.ChatMessages;
        _conversations = ctx.Conversations;
        _groups        = ctx.Groups;
    }

    // ── Conversations ─────────────────────────────────────────────────────────

    public async Task<Conversation?> GetConversationAsync(
        string userA, string userB, CancellationToken ct = default)
    {
        var (a, b) = Ordered(userA, userB);
        return await _conversations
            .Find(c => c.ParticipantAId == a && c.ParticipantBId == b && !c.IsDeleted)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Conversation> GetOrCreateConversationAsync(
        string userA, string userB, CancellationToken ct = default)
    {
        var existing = await GetConversationAsync(userA, userB, ct);
        if (existing != null) return existing;

        var (a, b) = Ordered(userA, userB);
        var conv = new Conversation
        {
            ParticipantAId = a,
            ParticipantBId = b,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
            UnreadCounts   = new() { [a] = 0, [b] = 0 }
        };
        await _conversations.InsertOneAsync(conv, cancellationToken: ct);
        return conv;
    }

    public async Task<bool> DeleteConversationAsync(string conversationId, CancellationToken ct = default)
    {
        var update = Builders<Conversation>.Update
            .Set(c => c.IsDeleted, true)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        var result = await _conversations.UpdateOneAsync(c => c.Id == conversationId, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<Conversation?> GetConversationByIdAsync(string conversationId, CancellationToken ct = default) =>
        await _conversations.Find(c => c.Id == conversationId).FirstOrDefaultAsync(ct);

    public async Task<List<Conversation>> GetUserConversationsAsync(
        string userId, CancellationToken ct = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.IsDeleted, false),
            Builders<Conversation>.Filter.Or(
                Builders<Conversation>.Filter.Eq(c => c.ParticipantAId, userId),
                Builders<Conversation>.Filter.Eq(c => c.ParticipantBId, userId)));

        return await _conversations
            .Find(filter)
            .SortByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);
    }

    // ── Groups ────────────────────────────────────────────────────────────────

    public async Task<Group?> GetGroupAsync(string groupId, CancellationToken ct = default) =>
        await _groups.Find(g => g.Id == groupId && !g.IsDeleted).FirstOrDefaultAsync(ct);

    public async Task<List<Group>> GetUserGroupsAsync(string userId, CancellationToken ct = default)
    {
        var filter = Builders<Group>.Filter.And(
            Builders<Group>.Filter.Eq(g => g.IsDeleted, false),
            Builders<Group>.Filter.ElemMatch(g => g.Members,
                Builders<GroupMember>.Filter.Eq(m => m.UserId, userId)));

        return await _groups
            .Find(filter)
            .SortByDescending(g => g.LastMessageAt)
            .ToListAsync(ct);
    }

    public async Task<Group> CreateGroupAsync(Group group, CancellationToken ct = default)
    {
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        await _groups.InsertOneAsync(group, cancellationToken: ct);
        return group;
    }

    public async Task<bool> AddGroupMemberAsync(
        string groupId, GroupMember member, CancellationToken ct = default)
    {
        var update = Builders<Group>.Update
            .Push(g => g.Members, member)
            .Set(g => g.UpdatedAt, DateTime.UtcNow);
        var result = await _groups.UpdateOneAsync(g => g.Id == groupId, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> RemoveGroupMemberAsync(
        string groupId, string userId, CancellationToken ct = default)
    {
        var update = Builders<Group>.Update
            .PullFilter(g => g.Members, m => m.UserId == userId)
            .Set(g => g.UpdatedAt, DateTime.UtcNow);
        var result = await _groups.UpdateOneAsync(g => g.Id == groupId, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task UpdateGroupLastMessageAsync(
        string groupId, string content, DateTime at, CancellationToken ct = default)
    {
        var update = Builders<Group>.Update
            .Set(g => g.LastMessageContent, content)
            .Set(g => g.LastMessageAt, at)
            .Set(g => g.UpdatedAt, at)
            .Inc($"Members.$[].UnreadCount", 1);   // bump everyone's unread
        await _groups.UpdateOneAsync(g => g.Id == groupId, update, cancellationToken: ct);
    }

    public async Task ResetGroupUnreadAsync(
        string groupId, string userId, CancellationToken ct = default)
    {
        var filter = Builders<Group>.Filter.And(
            Builders<Group>.Filter.Eq(g => g.Id, groupId),
            Builders<Group>.Filter.ElemMatch(g => g.Members, m => m.UserId == userId));
        var update = Builders<Group>.Update
            .Set("Members.$.UnreadCount", 0)
            .Set("Members.$.LastReadAt", DateTime.UtcNow);
        await _groups.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    // ── Messages ──────────────────────────────────────────────────────────────

    public async Task<ChatMessage> SaveMessageAsync(ChatMessage msg, CancellationToken ct = default)
    {
        msg.CreatedAt = DateTime.UtcNow;
        msg.UpdatedAt = DateTime.UtcNow;
        await _messages.InsertOneAsync(msg, cancellationToken: ct);
        return msg;
    }

    /// <summary>Cursor-based pagination — pass null cursor for first page.</summary>
    public async Task<(List<ChatMessage> Items, bool HasMore)> GetMessagesAsync(
        string contextId,
        int pageSize = 30,
        string? cursor = null,   // createdAt ISO string of oldest message on current page
        CancellationToken ct = default)
    {
        var filters = new List<FilterDefinition<ChatMessage>>
        {
            Builders<ChatMessage>.Filter.Eq(m => m.ContextId, contextId),
            Builders<ChatMessage>.Filter.Eq("IsDeleted", false),
        };

        if (!string.IsNullOrEmpty(cursor) && DateTime.TryParse(cursor, out var cursorDate))
            filters.Add(Builders<ChatMessage>.Filter.Lt(m => m.CreatedAt, cursorDate));

        var combined = Builders<ChatMessage>.Filter.And(filters);

        var items = await _messages
            .Find(combined)
            .SortByDescending(m => m.CreatedAt)
            .Limit(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        items.Reverse();   // chronological order for the UI
        return (items, hasMore);
    }

    public async Task<ChatMessage?> GetMessageAsync(string msgId, CancellationToken ct = default) =>
        await _messages.Find(m => m.Id == msgId).FirstOrDefaultAsync(ct);

    public async Task<bool> EditMessageAsync(
        string msgId, string newContent, CancellationToken ct = default)
    {
        var update = Builders<ChatMessage>.Update
            .Set(m => m.Content, newContent)
            .Set(m => m.IsEdited, true)
            .Set(m => m.UpdatedAt, DateTime.UtcNow);
        var result = await _messages.UpdateOneAsync(m => m.Id == msgId, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteMessageAsync(string msgId, CancellationToken ct = default)
    {
        var update = Builders<ChatMessage>.Update
            .Set(m => m.IsDeleted, true)
            .Set(m => m.Content, "This message was deleted")
            .Set(m => m.UpdatedAt, DateTime.UtcNow);
        var result = await _messages.UpdateOneAsync(m => m.Id == msgId, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task ToggleReactionAsync(
        string msgId, string emoji, string userId, CancellationToken ct = default)
    {
        var msg = await GetMessageAsync(msgId, ct);
        if (msg is null) return;

        var existing = msg.Reactions.FirstOrDefault(r => r.Emoji == emoji && r.UserId == userId);
        UpdateDefinition<ChatMessage> update;

        UpdateDefinition<ChatMessage> reactionUpdate;

        if (existing is not null)
        {
            var reactionFilter = Builders<MessageReaction>.Filter.And(
                Builders<MessageReaction>.Filter.Eq(r => r.Emoji, emoji),
                Builders<MessageReaction>.Filter.Eq(r => r.UserId, userId));
            reactionUpdate = Builders<ChatMessage>.Update.PullFilter(m => m.Reactions, reactionFilter);
        }
        else
        {
            reactionUpdate = Builders<ChatMessage>.Update.Push(
                m => m.Reactions, new MessageReaction { Emoji = emoji, UserId = userId });
        }

        update = Builders<ChatMessage>.Update.Combine(
            reactionUpdate,
            Builders<ChatMessage>.Update.Set(m => m.UpdatedAt, DateTime.UtcNow));

        await _messages.UpdateOneAsync(m => m.Id == msgId, update, cancellationToken: ct);
    }

    public async Task MarkDmReadAsync(
        string contextId, string userId, CancellationToken ct = default)
    {
        var filter = Builders<ChatMessage>.Filter.And(
            Builders<ChatMessage>.Filter.Eq(m => m.ContextId, contextId),
            Builders<ChatMessage>.Filter.Nin("ReadBy", new[] { userId }));
        var update = Builders<ChatMessage>.Update.AddToSet(m => m.ReadBy, userId);
        await _messages.UpdateManyAsync(filter, update, cancellationToken: ct);
    }

    public async Task UpdateConversationLastMessageAsync(
        string convId, string content, DateTime at,
        string senderId, string otherUserId, CancellationToken ct = default)
    {
        var update = Builders<Conversation>.Update
            .Set(c => c.LastMessageContent, content)
            .Set(c => c.LastMessageAt, at)
            .Set(c => c.UpdatedAt, at)
            .Inc($"UnreadCounts.{otherUserId}", 1);
        await _conversations.UpdateOneAsync(c => c.Id == convId, update, cancellationToken: ct);
    }

    public async Task ResetDmUnreadAsync(
        string convId, string userId, CancellationToken ct = default)
    {
        var update = Builders<Conversation>.Update
            .Set($"UnreadCounts.{userId}", 0);
        await _conversations.UpdateOneAsync(c => c.Id == convId, update, cancellationToken: ct);
    }

    // Infrastructure/Repositories/ChatRepository.cs — add these methods to your existing class
    // Assumes the same _groups : IMongoCollection<Group> field used by CreateGroupAsync / GetGroupAsync

    public async Task UpdateGroupAsync(Group group, CancellationToken ct)
    {
        await _groups.ReplaceOneAsync(
            g => g.Id == group.Id,
            group,
            cancellationToken: ct);
    }

    public async Task DeleteGroupAsync(string groupId, CancellationToken ct)
    {
        await _groups.DeleteOneAsync(g => g.Id == groupId, ct);

        // Also clean up messages tied to this group so they don't orphan in the messages collection
        await _messages.DeleteManyAsync(
            m => m.ContextId == groupId && m.ContextType == ChatContext.Group,
            ct);
    }

    public async Task<Group?> GetGroupByInviteCodeAsync(string inviteCode, CancellationToken ct)
    {
        return await _groups
            .Find(g => g.InviteCode == inviteCode)
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateMemberRoleAsync(string groupId, string userId, GroupRole newRole, CancellationToken ct)
    {
        var filter = Builders<Group>.Filter.And(
            Builders<Group>.Filter.Eq(g => g.Id, groupId),
            Builders<Group>.Filter.ElemMatch(g => g.Members, m => m.UserId == userId));

        var update = Builders<Group>.Update.Set("Members.$.Role", newRole);

        var result = await _groups.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new KeyNotFoundException($"Member '{userId}' not found in group '{groupId}'.");
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private static (string a, string b) Ordered(string x, string y) =>
        string.Compare(x, y, StringComparison.Ordinal) < 0 ? (x, y) : (y, x);
}