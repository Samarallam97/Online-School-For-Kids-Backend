using Domain.Entities.Users;


namespace Domain.Interfaces.Repositories.Users;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByChildInviteTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<(IEnumerable<User> Items, long TotalCount)> GetUsersPagedAsync(
         string? search,
         string? role,
         string? status,
         bool excludeAdmins,
         int skip,
         int limit,
         CancellationToken cancellationToken = default);

    Task<List<User>> GetManyByIdsAsync(
        List<string> ids,
        CancellationToken cancellationToken = default);

    Task<bool> HardDeleteAsync(string id, CancellationToken cancellationToken = default);

}
