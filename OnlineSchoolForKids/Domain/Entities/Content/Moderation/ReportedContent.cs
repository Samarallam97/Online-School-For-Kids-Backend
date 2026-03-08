using Domain.Enums.Content;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace Domain.Entities.Content.Moderation
{
    public class ReportedContent : BaseEntity
    {
        public string ReportedBy { get; set; } = string.Empty; // User ID
        public string ReportedByName { get; set; } = string.Empty;
        [BsonRepresentation(BsonType.String)]
        public ContentType ContentType { get; set; }
        public string ContentId { get; set; } = string.Empty; // Course/Comment ID
        public string ContentTitle { get; set; } = string.Empty;
        [BsonRepresentation(BsonType.String)]
        public ReportReason Reason { get; set; }
        public string Description { get; set; } = string.Empty;
        [BsonRepresentation(BsonType.String)]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public int ReportCount { get; set; } = 1; // Total reports for same content
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
        public ModerationAction? Action { get; set; }
    }

}
