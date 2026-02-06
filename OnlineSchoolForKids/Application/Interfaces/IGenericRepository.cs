using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Application.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<T?> GetOneAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<long> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default);
}