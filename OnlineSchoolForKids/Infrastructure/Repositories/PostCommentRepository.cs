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

public class PostCommentRepository : IPostCommentRepository
{
    private readonly IMongoCollection<PostComment> _comments;

    public PostCommentRepository(MongoDbContext context)
    {
        _comments = context.GetCollection<PostComment>("postComments");
    }

    public async Task<PostComment> CreateAsync(PostComment comment, CancellationToken ct = default)
    {
        comment.CreatedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;
        await _comments.InsertOneAsync(comment, cancellationToken: ct);
        return comment;
    }

    public async Task<PostComment?> GetByIdAsync(string commentId, CancellationToken ct = default)
    {
        return await _comments.Find(c => c.Id == commentId && !c.IsDeleted).FirstOrDefaultAsync(ct);
    }

    public async Task<List<PostComment>> GetByPostIdAsync(string postId, CancellationToken ct = default)
    {
        return await _comments
            .Find(c => c.PostId == postId && !c.IsDeleted)
            .SortBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(string commentId, string authorId, CancellationToken ct = default)
    {
        var update = Builders<PostComment>.Update
            .Set(c => c.IsDeleted, true)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        var result = await _comments.UpdateOneAsync(
            c => c.Id == commentId && c.AuthorId == authorId && !c.IsDeleted,
            update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }
}