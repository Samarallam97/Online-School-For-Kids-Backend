using Domain.Entities.Content.Leaderboard;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class PointTransactionRepository:GenericRepository<PointTransaction>, IPointTransactionRepository    
    {
        public PointTransactionRepository(MongoDbContext context):base(context.PointTransactions)
        {
            
        }
    }
}
