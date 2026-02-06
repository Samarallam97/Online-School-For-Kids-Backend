using Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

[BsonCollection("users")]
public class User : BaseEntity
{
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("emailVerified")]
    public bool EmailVerified { get; set; } = false;

    [BsonElement("emailVerificationToken")]
    public string? EmailVerificationToken { get; set; }

    [BsonElement("emailVerificationTokenExpiry")]
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    [BsonElement("passwordHash")]
    public string? PasswordHash { get; set; }

    [BsonElement("passwordResetToken")]
    public string? PasswordResetToken { get; set; }

    [BsonElement("passwordResetTokenExpiry")]
    public DateTime? PasswordResetTokenExpiry { get; set; }

    [BsonElement("role")]
    public UserRole Role { get; set; }

    [BsonElement("authProvider")]
    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;

    [BsonElement("googleId")]
    public string? GoogleId { get; set; }

    [BsonElement("profilePictureUrl")]
    public string? ProfilePictureUrl { get; set; }

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    // For parent-child relationship
    [BsonElement("parentId")]
    public string? ParentId { get; set; }

    [BsonElement("childrenIds")]
    public List<string> ChildrenIds { get; set; } = new();
}

// Attribute for collection naming
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }

    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}
