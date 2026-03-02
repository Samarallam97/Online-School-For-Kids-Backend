using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;

namespace Infrastructure.Repositories.Content
{
    public class SectionRepository:GenericRepository<Section>,ISectionRepository
    {
        public SectionRepository(MongoDbContext context) : base(context.Sections)
        {

        }
    }
}
