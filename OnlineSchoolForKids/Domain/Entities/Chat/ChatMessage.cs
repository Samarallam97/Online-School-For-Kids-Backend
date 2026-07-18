namespace Domain.Entities.Chat;


// ── Message ───────────────────────────────────────────────────────────────────
public class ChatMessage : BaseEntity
{
    /// <summary>Either a ConversationId or a GroupId — use ContextType to know which.</summary>
    public string ContextId { get; set; } = string.Empty;
    public ChatContext ContextType { get; set; }

    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }

    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }

    /// <summary>Populated only for DMs. List of user-ids who have read this message.</summary>
    public List<string> ReadBy { get; set; } = new();
    public string? SessionId { get; set; }
    public bool IsHost { get; set; } = false;
    public List<MessageReaction> Reactions { get; set; } = new();

    /// <summary>Non-null if this is a reply to another message.</summary>
    public string? ReplyToMessageId { get; set; }
    public string? ReplyToMessageContent { get; set; }
    public string? ReplyToSenderName { get; set; }

    public bool IsEdited { get; set; } = false;
    //public bool IsDeleted { get; set; } = false;   // soft-delete (overrides BaseEntity)
}

public class MessageReaction
{
    public string Emoji { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public enum ChatContext { DirectMessage, Group, LiveSession }
public enum MessageType { Text, Image, File }
