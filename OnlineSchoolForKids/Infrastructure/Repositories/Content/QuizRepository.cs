using Domain.Entities.Content.Quiz;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class QuizRepository : GenericRepository<Quiz>, IQuizRepository
    {
        public QuizRepository(MongoDbContext context) : base(context.Quizzes)
        {

        }
    }
}
