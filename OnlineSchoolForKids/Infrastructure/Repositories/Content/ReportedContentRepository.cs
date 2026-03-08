using Domain.Entities.Content.Moderation;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class ReportedContentRepository:GenericRepository<ReportedContent>, IReportedContentRepository
    {
        public ReportedContentRepository(MongoDbContext context):base(context.ReportedContents)
        {
            
        }
    }
}
