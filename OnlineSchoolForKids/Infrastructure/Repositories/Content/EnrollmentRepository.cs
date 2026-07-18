using Domain.Entities.Content;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;
namespace Infrastructure.Repositories.Content;

public class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(MongoDbContext context) : base(context.Enrollments)
    {
    }

    public async Task<Enrollment?> GetByUserAndCourseAsync(
        string userId, string courseId, CancellationToken ct = default)
    {
        return await GetOneAsync(
            e => e.UserId == userId && e.CourseId == courseId,
            ct);
    }
}
