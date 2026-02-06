using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
}
