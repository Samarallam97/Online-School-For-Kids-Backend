using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

[BsonCollection("refreshTokens")]
public class RefreshToken : BaseEntity
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("isRevoked")]
    public bool IsRevoked { get; set; } = false;

    [BsonElement("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    [BsonElement("replacedByToken")]
    public string? ReplacedByToken { get; set; }

    [BsonElement("deviceInfo")]
    public string? DeviceInfo { get; set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
