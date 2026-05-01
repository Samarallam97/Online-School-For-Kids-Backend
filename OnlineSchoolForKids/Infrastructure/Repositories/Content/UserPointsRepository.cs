using Domain.Entities.Content.Leaderboard;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class UserPointsRepository:GenericRepository<UserPoints>, IUserPointsRepository
    {
        public UserPointsRepository(MongoDbContext context):base(context.UserPoints)
        {
            
        }
    }
}
