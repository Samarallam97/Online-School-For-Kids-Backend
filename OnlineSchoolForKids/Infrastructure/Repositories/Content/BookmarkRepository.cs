using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class BookmarkRepository:GenericRepository<Bookmark>,IBookmarkRepository
    {
        public BookmarkRepository(MongoDbContext context) : base(context.Bookmarks)
        {

        }
    }
}

