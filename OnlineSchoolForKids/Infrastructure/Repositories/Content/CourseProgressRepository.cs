using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class CourseProgressRepository:GenericRepository<CourseProgress>, ICourseProgressRepository
    {
        public CourseProgressRepository(MongoDbContext context) : base(context.CoursesProgress)
        {

        }
    }
}
