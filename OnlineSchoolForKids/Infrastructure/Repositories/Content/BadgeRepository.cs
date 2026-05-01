using Domain.Entities.Content.Leaderboard;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class BadgeRepository:GenericRepository<Badge>, IBadgeRepository
    {
        public BadgeRepository(MongoDbContext context):base(context.Badges)
        {
            
        }
    }
}
