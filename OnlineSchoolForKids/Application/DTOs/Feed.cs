using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs;

public class PostAuthorDto
{
    public string Id { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsFollowedByViewer { get; set; }
    public bool IsFollowedByFollowing { get; set; }        // friend-of-friend
    public string? FollowedByName { get; set; }            // "Followed by Ahmed" label
}

public class PostDto
{
    public string Id { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> MediaUrls { get; set; } = new();
    public string MediaType { get; set; } = "text";
    public string Visibility { get; set; } = "public";
    public int CommentsCount { get; set; }
    public int SharesCount { get; set; }
    public int TotalReactions { get; set; }
    public Dictionary<string, int> ReactionCounts { get; set; } = new();
    public string? UserReaction { get; set; }
    public DateTime CreatedAt { get; set; }
    public PostAuthorDto? Author { get; set; }
    public string FeedSection { get; set; } = "public"; // "following" | "fof" | "public"
    public DateTime? UpdatedAt { get; set; }
}

public class PostCommentDto
{
    public string Id { get; set; } = string.Empty;
    public string PostId { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public PostAuthorDto? Author { get; set; }
}

public class CreatePostRequest
{
    public string Content { get; set; } = string.Empty;
    public List<string>? MediaUrls { get; set; }
    public string MediaType { get; set; } = "text";
    public string Visibility { get; set; } = "public"; // "public" | "private"
}

public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class ReactRequest
{
    public string? ReactionType { get; set; }
}

public class ReactResponse
{
    public string? UserReaction { get; set; }
    public Dictionary<string, int> ReactionCounts { get; set; } = new();
    public int TotalReactions { get; set; }
}

public class FollowResponse
{
    public bool IsFollowing { get; set; }
    public int FollowersCount { get; set; }
}

public class UserFollowStatsDto
{
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowedByViewer { get; set; }
}

public class FeedResult
{
    public List<PostDto> FollowingPosts { get; set; } = new();  // people I follow
    public List<PostDto> FofPosts { get; set; } = new();        // friends-of-friends
    public List<PostDto> PublicPosts { get; set; } = new();     // everyone else
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

public class UpdatePostRequest
{
    public string? Content { get; set; }
    public string? Visibility { get; set; }      // "public" | "private"
    public List<string>? MediaUrls { get; set; }
    public string? MediaType { get; set; }
}