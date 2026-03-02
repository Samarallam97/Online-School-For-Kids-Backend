using Domain.Entities.Content.Quiz;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class AttemptRepository:GenericRepository<QuizAttempt>,IAttemptRepository
    {
        public AttemptRepository(MongoDbContext context) : base(context.QuizAttempts)
        {

        }
    }
}
