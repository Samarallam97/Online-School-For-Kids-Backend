using Domain.Entities.Feed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces.Repositories;

public interface IPostRepository
{
    Task<Post> CreateAsync(Post post, CancellationToken ct = default);
    Task<Post?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>All public posts for the public feed (unauthenticated / fallback)</summary>
    Task<(IEnumerable<Post> Items, long TotalCount)> GetPublicPagedAsync(
        int skip, int limit, CancellationToken ct = default);

    /// <summary>Posts by a specific set of author IDs (following / fof feeds)</summary>
    Task<List<Post>> GetByAuthorIdsAsync(
        List<string> authorIds, int skip, int limit, CancellationToken ct = default);

    /// <summary>All public posts excluding given author IDs</summary>
    Task<(IEnumerable<Post> Items, long TotalCount)> GetPublicExcludingAuthorsAsync(
        List<string> excludeAuthorIds, int skip, int limit, CancellationToken ct = default);

    /// <summary>Posts by a single author visible to a viewer (public + own private)</summary>
    Task<List<Post>> GetByAuthorForViewerAsync(
        string authorId, string? viewerId, int skip, int limit, CancellationToken ct = default);

    Task<bool> IncrementSharesAsync(string postId, CancellationToken ct = default);
    Task<bool> UpdateReactionCountsAsync(string postId, Dictionary<string, int> counts, int total, CancellationToken ct = default);
    Task<bool> UpdateCommentsCountAsync(string postId, int delta, CancellationToken ct = default);
    Task<bool> DeleteAsync(string postId, string authorId, CancellationToken ct = default);

    Task<Post?> UpdateAsync(
    string postId, string requesterId,
    string? content, string? visibility, List<string>? mediaUrls, string? mediaType,
    CancellationToken ct = default);
}

public interface IPostReactionRepository
{
    Task<PostReaction?> GetAsync(string postId, string userId, CancellationToken ct = default);
    Task UpsertAsync(PostReaction reaction, CancellationToken ct = default);
    Task RemoveAsync(string postId, string userId, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetUserReactionsAsync(string userId, List<string> postIds, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetCountsForPostAsync(string postId, CancellationToken ct = default);
}

public interface IPostCommentRepository
{
    Task<PostComment> CreateAsync(PostComment comment, CancellationToken ct = default);
    Task<PostComment?> GetByIdAsync(string commentId, CancellationToken ct = default);
    Task<List<PostComment>> GetByPostIdAsync(string postId, CancellationToken ct = default);
    Task<bool> DeleteAsync(string commentId, string authorId, CancellationToken ct = default);
}