using Domain.Entities.Content.Moderation;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class CommentRepository:GenericRepository<Comment>,ICommentRepository
    {
        public CommentRepository(MongoDbContext context):base(context.Comments)
        {
            
        }
    }
}
