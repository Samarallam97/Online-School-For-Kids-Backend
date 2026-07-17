using Domain.Entities.Feed;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;



public class PostRepository : IPostRepository
{
    private readonly IMongoCollection<Post> _posts;

    public PostRepository(MongoDbContext context)
    {
        _posts = context.GetCollection<Post>("posts");
    }

    public async Task<Post> CreateAsync(Post post, CancellationToken ct = default)
    {
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        await _posts.InsertOneAsync(post, cancellationToken: ct);
        return post;
    }

    public async Task<Post?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _posts.Find(p => p.Id == id && !p.IsDeleted).FirstOrDefaultAsync(ct);

    public async Task<(IEnumerable<Post> Items, long TotalCount)> GetPublicPagedAsync(
        int skip, int limit, CancellationToken ct = default)
    {
        var filter = Builders<Post>.Filter.And(
            Builders<Post>.Filter.Eq(p => p.IsDeleted, false),
            Builders<Post>.Filter.Eq(p => p.Visibility, "public"));

        var total = await _posts.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _posts.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip(skip).Limit(limit)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<List<Post>> GetByAuthorIdsAsync(
        List<string> authorIds, int skip, int limit, CancellationToken ct = default)
    {
        if (authorIds.Count == 0) return new();
        var filter = Builders<Post>.Filter.And(
            Builders<Post>.Filter.In(p => p.AuthorId, authorIds),
            Builders<Post>.Filter.Eq(p => p.IsDeleted, false),
            Builders<Post>.Filter.Eq(p => p.Visibility, "public"));
        return await _posts.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip(skip).Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<Post> Items, long TotalCount)> GetPublicExcludingAuthorsAsync(
        List<string> excludeAuthorIds, int skip, int limit, CancellationToken ct = default)
    {
        var filter = Builders<Post>.Filter.And(
            Builders<Post>.Filter.Eq(p => p.IsDeleted, false),
            Builders<Post>.Filter.Eq(p => p.Visibility, "public"),
            excludeAuthorIds.Count > 0
                ? Builders<Post>.Filter.Nin(p => p.AuthorId, excludeAuthorIds)
                : Builders<Post>.Filter.Empty);

        var total = await _posts.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _posts.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip(skip).Limit(limit)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<List<Post>> GetByAuthorForViewerAsync(
        string authorId, string? viewerId, int skip, int limit, CancellationToken ct = default)
    {
        FilterDefinition<Post> filter;
        if (viewerId == authorId)
        {
            // Owner sees all their own posts
            filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(p => p.AuthorId, authorId),
                Builders<Post>.Filter.Eq(p => p.IsDeleted, false));
        }
        else
        {
            // Others only see public posts
            filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(p => p.AuthorId, authorId),
                Builders<Post>.Filter.Eq(p => p.IsDeleted, false),
                Builders<Post>.Filter.Eq(p => p.Visibility, "public"));
        }
        return await _posts.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip(skip).Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<bool> IncrementSharesAsync(string postId, CancellationToken ct = default)
    {
        var upd = Builders<Post>.Update.Inc(p => p.SharesCount, 1).Set(p => p.UpdatedAt, DateTime.UtcNow);
        var r = await _posts.UpdateOneAsync(p => p.Id == postId && !p.IsDeleted, upd, cancellationToken: ct);
        return r.ModifiedCount > 0;
    }

    public async Task<bool> UpdateReactionCountsAsync(
        string postId, Dictionary<string, int> counts, int total, CancellationToken ct = default)
    {
        var upd = Builders<Post>.Update
            .Set(p => p.ReactionCounts, counts)
            .Set(p => p.TotalReactions, total)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);
        var r = await _posts.UpdateOneAsync(p => p.Id == postId && !p.IsDeleted, upd, cancellationToken: ct);
        return r.ModifiedCount > 0;
    }

    public async Task<bool> UpdateCommentsCountAsync(string postId, int delta, CancellationToken ct = default)
    {
        var upd = Builders<Post>.Update.Inc(p => p.CommentsCount, delta).Set(p => p.UpdatedAt, DateTime.UtcNow);
        var r = await _posts.UpdateOneAsync(p => p.Id == postId && !p.IsDeleted, upd, cancellationToken: ct);
        return r.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string postId, string authorId, CancellationToken ct = default)
    {
        var upd = Builders<Post>.Update.Set(p => p.IsDeleted, true).Set(p => p.UpdatedAt, DateTime.UtcNow);
        var r = await _posts.UpdateOneAsync(
            p => p.Id == postId && p.AuthorId == authorId && !p.IsDeleted, upd, cancellationToken: ct);
        return r.ModifiedCount > 0;
    }

    public async Task<Post?> UpdateAsync(
     string postId, string requesterId,
     string? content, string? visibility, List<string>? mediaUrls, string? mediaType,
     CancellationToken ct = default)
    {
        var filter = Builders<Post>.Filter.Where(p =>
            p.Id == postId && p.AuthorId == requesterId && !p.IsDeleted);

        var updates = new List<UpdateDefinition<Post>>();
        if (content != null) updates.Add(Builders<Post>.Update.Set(p => p.Content, content));
        if (visibility != null) updates.Add(Builders<Post>.Update.Set(p => p.Visibility, visibility));
        if (mediaUrls != null) updates.Add(Builders<Post>.Update.Set(p => p.MediaUrls, mediaUrls));
        if (mediaType != null) updates.Add(Builders<Post>.Update.Set(p => p.MediaType, mediaType));
        updates.Add(Builders<Post>.Update.Set(p => p.UpdatedAt, DateTime.UtcNow));

        var update = Builders<Post>.Update.Combine(updates);
        var options = new FindOneAndUpdateOptions<Post> { ReturnDocument = ReturnDocument.After };

        return await _posts.FindOneAndUpdateAsync(filter, update, options, ct);
    }
}
