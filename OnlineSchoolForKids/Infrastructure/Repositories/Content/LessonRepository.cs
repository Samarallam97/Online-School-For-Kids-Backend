using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class LessonRepository:GenericRepository<Lesson>, ILessonRepository
    {
        public LessonRepository(MongoDbContext context) : base(context.Lessons)
        {

        }
    }
}
