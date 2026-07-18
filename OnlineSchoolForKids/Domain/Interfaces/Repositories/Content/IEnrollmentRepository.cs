using Domain.Entities.Content;


namespace Domain.Interfaces.Repositories.Content;


public interface IEnrollmentRepository : IGenericRepository<Enrollment>
{
    /// <summary>
    /// Returns the enrollment record if the user is enrolled in the course,
    /// otherwise null. Used to gate access to live sessions and other
    /// course-restricted content.
    /// </summary>
    Task<Enrollment?> GetByUserAndCourseAsync(
        string userId, string courseId, CancellationToken ct = default);
}


