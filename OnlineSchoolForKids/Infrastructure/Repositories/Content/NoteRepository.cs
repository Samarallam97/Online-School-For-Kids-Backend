using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class NoteRepository: GenericRepository<Note>,INoteRepository
    {
        public NoteRepository(MongoDbContext context) : base(context.Notes)
        {

        }
    }
}
