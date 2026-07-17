using Application.DTOs;
using Domain.Entities.Feed;
using Domain.Entities.Users;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services;


public interface IFeedService
{
    Task<FeedResult> GetFeedAsync(int page, int pageSize, string? currentUserId, CancellationToken ct = default);
    Task<PostDto> CreatePostAsync(string authorId, CreatePostRequest request, CancellationToken ct = default);
    Task<bool> DeletePostAsync(string postId, string requesterId, CancellationToken ct = default);
    Task<ReactResponse> ReactAsync(string postId, string userId, string? reactionType, CancellationToken ct = default);
    Task<PostCommentDto> AddCommentAsync(string postId, string authorId, CreateCommentRequest request, CancellationToken ct = default);
    Task<List<PostCommentDto>> GetCommentsAsync(string postId, CancellationToken ct = default);
    Task<bool> SharePostAsync(string postId, CancellationToken ct = default);

    // Follow
    Task<FollowResponse> ToggleFollowAsync(string followerId, string followeeId, CancellationToken ct = default);
    Task<UserFollowStatsDto> GetFollowStatsAsync(string userId, string? viewerId, CancellationToken ct = default);

    // Profile posts
    Task<List<PostDto>> GetUserPostsAsync(string authorId, string? viewerId, int page, int pageSize, CancellationToken ct = default);

    Task<PostDto?> UpdatePostAsync(string postId, string requesterId, UpdatePostRequest request, CancellationToken ct = default);
}


public class FeedService : IFeedService
{
    private static readonly HashSet<string> ValidReactions =
        new() { "like", "love", "haha", "wow", "sad", "angry" };

    private readonly IPostRepository _posts;
    private readonly IPostReactionRepository _reactions;
    private readonly IPostCommentRepository _comments;
    private readonly IUserRepository _users;
    private readonly IFollowRepository _follows;

    public FeedService(
        IPostRepository posts,
        IPostReactionRepository reactions,
        IPostCommentRepository comments,
        IUserRepository users,
        IFollowRepository follows)
    {
        _posts = posts;
        _reactions = reactions;
        _comments = comments;
        _users = users;
        _follows = follows;
    }

    // ── Feed ─────────────────────────────────────────────────────────────────

    public async Task<FeedResult> GetFeedAsync(
        int page, int pageSize, string? currentUserId, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        int skip = (page - 1) * pageSize;

        List<string> followingIds = new();
        List<string> fofIds = new();

        if (!string.IsNullOrEmpty(currentUserId))
        {
            // Who I follow
            followingIds = await _follows.GetFollowingIdsAsync(currentUserId, ct);

            // Who my followees follow (friends-of-friends), excluding myself & direct follows
            var fofTasks = followingIds.Select(id => _follows.GetFollowingIdsAsync(id, ct));
            var fofResults = await Task.WhenAll(fofTasks);
            fofIds = fofResults
                .SelectMany(ids => ids)
                .Distinct()
                .Where(id => id != currentUserId && !followingIds.Contains(id))
                .ToList();
        }

        // Own posts always land in the "following" bucket, so keep them out of "public"
        // to avoid the same post appearing in both sections.
        var ownPostsIds = string.IsNullOrEmpty(currentUserId)
            ? new List<string>()
            : new List<string> { currentUserId };

        var excludedFromPublic = followingIds.Concat(fofIds).Concat(ownPostsIds)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct().ToList();

        // Following bucket excludes nobody — show own posts in following section too
        var followingWithSelf = followingIds.Concat(ownPostsIds).Distinct().ToList();

        var followingPostsTask = _posts.GetByAuthorIdsAsync(followingWithSelf, 0, pageSize, ct);
        var fofPostsTask = _posts.GetByAuthorIdsAsync(fofIds, 0, pageSize, ct);
        var publicTask = _posts.GetPublicExcludingAuthorsAsync(excludedFromPublic, skip, pageSize, ct);

        await Task.WhenAll(followingPostsTask, fofPostsTask, publicTask);

        var followingPosts = followingPostsTask.Result;
        var fofPosts = fofPostsTask.Result;
        var (publicPosts, totalCount) = publicTask.Result;

        // Collect all author IDs and bulk-fetch profiles
        var allPosts = followingPosts.Concat(fofPosts).Concat(publicPosts).ToList();
        var authorIds = allPosts.Select(p => p.AuthorId).Distinct().ToList();
        var profiles = await _users.GetManyByIdsAsync(authorIds, ct);
        var profileMap = profiles.ToDictionary(u => u.Id, u => u);

        // Per-user reactions
        Dictionary<string, string> userReactionMap = new();
        HashSet<string> followingSet = followingIds.ToHashSet();

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var allPostIds = allPosts.Select(p => p.Id).ToList();
            userReactionMap = await _reactions.GetUserReactionsAsync(currentUserId, allPostIds, ct);
        }

        // For fof posts: find which of my followees follows that author ("Followed by Ahmed")
        // Build a map: fofAuthorId -> one of my followees who follows them
        Dictionary<string, string> fofFollowedByMap = new();
        if (fofIds.Count > 0 && followingIds.Count > 0)
        {
            foreach (var fofAuthorId in fofIds)
            {
                // find first followee who follows this fof author
                foreach (var followeeId in followingIds)
                {
                    var followeeFollowing = await _follows.GetFollowingIdsAsync(followeeId, ct);
                    if (followeeFollowing.Contains(fofAuthorId))
                    {
                        profileMap.TryGetValue(followeeId, out var followee);
                        fofFollowedByMap[fofAuthorId] = followee?.FullName ?? "";
                        break;
                    }
                }
            }
        }

        PostDto Map(Post p, string section)
        {
            userReactionMap.TryGetValue(p.Id, out var userReaction);
            profileMap.TryGetValue(p.AuthorId, out var author);

            fofFollowedByMap.TryGetValue(p.AuthorId, out var followedByName);

            return new PostDto
            {
                Id            = p.Id,
                AuthorId      = p.AuthorId,
                Content       = p.Content,
                MediaUrls     = p.MediaUrls,
                MediaType     = p.MediaType,
                Visibility    = p.Visibility,
                CommentsCount = p.CommentsCount,
                SharesCount   = p.SharesCount,
                TotalReactions = p.TotalReactions,
                ReactionCounts = p.ReactionCounts ?? new(),
                UserReaction  = userReaction,
                CreatedAt     = p.CreatedAt,
                FeedSection   = section,
                Author = author == null ? null : new PostAuthorDto
                {
                    Id                   = author.Id,
                    FullName             = author.FullName,
                    AvatarUrl            = author.ProfilePictureUrl,
                    Role                 = author.Role.ToString().ToLower(),
                    IsFollowedByViewer   = followingSet.Contains(author.Id),
                    IsFollowedByFollowing = section == "fof",
                    FollowedByName       = followedByName
                }
            };
        }

        return new FeedResult
        {
            FollowingPosts = followingPosts.Select(p => Map(p, "following")).ToList(),
            FofPosts       = fofPosts.Select(p => Map(p, "fof")).ToList(),
            PublicPosts    = publicPosts.Select(p => Map(p, "public")).ToList(),
            TotalCount     = totalCount,
            Page           = page,
            PageSize       = pageSize,
            HasMore        = (long)skip + pageSize < totalCount
        };
    }

    // ── Create / Delete post ──────────────────────────────────────────────────

    public async Task<PostDto> CreatePostAsync(
        string authorId, CreatePostRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Post content cannot be empty.");

        var visibility = request.Visibility == "private" ? "private" : "public";

        var post = new Post
        {
            AuthorId   = authorId,
            Content    = request.Content.Trim(),
            MediaUrls  = request.MediaUrls ?? new(),
            MediaType  = request.MediaType ?? "text",
            Visibility = visibility,
            ReactionCounts = new()
        };

        var created = await _posts.CreateAsync(post, ct);
        var profiles = await _users.GetManyByIdsAsync(new List<string> { authorId }, ct);
        var author = profiles.FirstOrDefault();

        return MapPostDto(created, author, null, false, false, null, "public");
    }

    public async Task<bool> DeletePostAsync(
        string postId, string requesterId, CancellationToken ct = default)
        => await _posts.DeleteAsync(postId, requesterId, ct);

    // ── Reactions ─────────────────────────────────────────────────────────────

    public async Task<ReactResponse> ReactAsync(
        string postId, string userId, string? reactionType, CancellationToken ct = default)
    {
        if (reactionType != null && !ValidReactions.Contains(reactionType))
            throw new ArgumentException($"Invalid reaction type: {reactionType}");

        var post = await _posts.GetByIdAsync(postId, ct)
            ?? throw new KeyNotFoundException("Post not found.");

        var existing = await _reactions.GetAsync(postId, userId, ct);
        bool toggling = existing != null && existing.ReactionType == reactionType;

        if (reactionType == null || toggling)
        {
            if (existing != null) await _reactions.RemoveAsync(postId, userId, ct);
        }
        else
        {
            await _reactions.UpsertAsync(new PostReaction
            {
                PostId = postId,
                UserId = userId,
                ReactionType = reactionType
            }, ct);
        }

        var counts = await _reactions.GetCountsForPostAsync(postId, ct);
        int total = counts.Values.Sum();
        await _posts.UpdateReactionCountsAsync(postId, counts, total, ct);

        return new ReactResponse
        {
            UserReaction   = toggling ? null : reactionType,
            ReactionCounts = counts,
            TotalReactions = total
        };
    }

    // ── Comments ──────────────────────────────────────────────────────────────

    public async Task<PostCommentDto> AddCommentAsync(
        string postId, string authorId, CreateCommentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Comment cannot be empty.");

        _ = await _posts.GetByIdAsync(postId, ct) ?? throw new KeyNotFoundException("Post not found.");

        var comment = new PostComment { PostId = postId, AuthorId = authorId, Content = request.Content.Trim() };
        var created = await _comments.CreateAsync(comment, ct);
        await _posts.UpdateCommentsCountAsync(postId, 1, ct);

        var profiles = await _users.GetManyByIdsAsync(new List<string> { authorId }, ct);
        return MapCommentDto(created, profiles.FirstOrDefault());
    }

    public async Task<List<PostCommentDto>> GetCommentsAsync(
        string postId, CancellationToken ct = default)
    {
        var comments = await _comments.GetByPostIdAsync(postId, ct);
        if (comments.Count == 0) return new();

        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var profiles = await _users.GetManyByIdsAsync(authorIds, ct);
        var map = profiles.ToDictionary(u => u.Id, u => u);

        return comments.Select(c =>
        {
            map.TryGetValue(c.AuthorId, out var author);
            return MapCommentDto(c, author);
        }).ToList();
    }

    public async Task<bool> SharePostAsync(string postId, CancellationToken ct = default)
        => await _posts.IncrementSharesAsync(postId, ct);

    // ── Follow ────────────────────────────────────────────────────────────────

    public async Task<FollowResponse> ToggleFollowAsync(
        string followerId, string followeeId, CancellationToken ct = default)
    {
        if (followerId == followeeId)
            throw new ArgumentException("You cannot follow yourself.");

        bool isFollowing = await _follows.IsFollowingAsync(followerId, followeeId, ct);

        if (isFollowing)
            await _follows.UnfollowAsync(followerId, followeeId, ct);
        else
            await _follows.FollowAsync(new Follow { FollowerId = followerId, FolloweeId = followeeId }, ct);

        int newCount = await _follows.GetFollowerCountAsync(followeeId, ct);
        return new FollowResponse { IsFollowing = !isFollowing, FollowersCount = newCount };
    }

    public async Task<UserFollowStatsDto> GetFollowStatsAsync(
        string userId, string? viewerId, CancellationToken ct = default)
    {
        var followersTask = _follows.GetFollowerCountAsync(userId, ct);
        var followingTask = _follows.GetFollowingCountAsync(userId, ct);

        bool isFollowed = false;
        if (!string.IsNullOrEmpty(viewerId) && viewerId != userId)
            isFollowed = await _follows.IsFollowingAsync(viewerId, userId, ct);

        await Task.WhenAll(followersTask, followingTask);

        return new UserFollowStatsDto
        {
            FollowersCount     = followersTask.Result,
            FollowingCount     = followingTask.Result,
            IsFollowedByViewer = isFollowed
        };
    }

    // ── Profile posts ─────────────────────────────────────────────────────────

    public async Task<List<PostDto>> GetUserPostsAsync(
        string authorId, string? viewerId, int page, int pageSize, CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        int skip = (page - 1) * pageSize;

        var postList = await _posts.GetByAuthorForViewerAsync(authorId, viewerId, skip, pageSize, ct);
        if (postList.Count == 0) return new();

        var profiles = await _users.GetManyByIdsAsync(new List<string> { authorId }, ct);
        var author = profiles.FirstOrDefault();

        bool viewerFollows = !string.IsNullOrEmpty(viewerId) && viewerId != authorId
            && await _follows.IsFollowingAsync(viewerId, authorId, ct);

        Dictionary<string, string> reactionMap = new();
        if (!string.IsNullOrEmpty(viewerId))
            reactionMap = await _reactions.GetUserReactionsAsync(viewerId, postList.Select(p => p.Id).ToList(), ct);

        return postList.Select(p =>
        {
            reactionMap.TryGetValue(p.Id, out var userReaction);
            return MapPostDto(p, author, userReaction, viewerFollows, false, null, "profile");
        }).ToList();
    }

    public async Task<PostDto?> UpdatePostAsync(
    string postId, string requesterId, UpdatePostRequest request, CancellationToken ct = default)
    {
        if (request.Content != null && string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Post content cannot be empty.");

        string? visibility = request.Visibility == null
            ? null
            : (request.Visibility == "private" ? "private" : "public");

        var updated = await _posts.UpdateAsync(
            postId, requesterId,
            request.Content?.Trim(), visibility, request.MediaUrls, request.MediaType, ct);

        if (updated == null) return null; // not found, or requester doesn't own it

        var profiles = await _users.GetManyByIdsAsync(new List<string> { updated.AuthorId }, ct);
        var author = profiles.FirstOrDefault();

        return MapPostDto(updated, author, null, false, false, null, "profile");
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static PostDto MapPostDto(
        Post p, User? author, string? userReaction,
        bool isFollowed, bool isFof, string? followedByName, string section)
        => new()
        {
            Id             = p.Id,
            AuthorId       = p.AuthorId,
            Content        = p.Content,
            MediaUrls      = p.MediaUrls,
            MediaType      = p.MediaType,
            Visibility     = p.Visibility,
            CommentsCount  = p.CommentsCount,
            SharesCount    = p.SharesCount,
            TotalReactions = p.TotalReactions,
            ReactionCounts = p.ReactionCounts ?? new(),
            UserReaction   = userReaction,
            CreatedAt      = p.CreatedAt,
            FeedSection    = section,
            UpdatedAt = p.UpdatedAt,
            Author = author == null ? null : new PostAuthorDto
            {
                Id                    = author.Id,
                FullName              = author.FullName,
                AvatarUrl             = author.ProfilePictureUrl,
                Role                  = author.Role.ToString().ToLower(),
                IsFollowedByViewer    = isFollowed,
                IsFollowedByFollowing = isFof,
                FollowedByName        = followedByName
            }
        };

    private static PostCommentDto MapCommentDto(PostComment c, User? author) => new()
    {
        Id        = c.Id,
        PostId    = c.PostId,
        AuthorId  = c.AuthorId,
        Content   = c.Content,
        CreatedAt = c.CreatedAt,
        Author = author == null ? null : new PostAuthorDto
        {
            Id        = author.Id,
            FullName  = author.FullName,
            AvatarUrl = author.ProfilePictureUrl,
            Role      = author.Role.ToString().ToLower()
        }
    };
}
