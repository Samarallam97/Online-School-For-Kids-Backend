using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class LessonProgressRepository:GenericRepository<LessonProgress>,ILessonProgressRepository
    {
        public LessonProgressRepository(MongoDbContext context) : base(context.LessonsProgress)
        {

        }
    }
}
