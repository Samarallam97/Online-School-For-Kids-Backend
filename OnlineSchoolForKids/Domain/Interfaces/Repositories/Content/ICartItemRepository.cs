using Domain.Entities.Content;

namespace Domain.Interfaces.Repositories.Content
{
        public interface ICartItemRepository
        {
            Task<CartItem> CreateAsync(CartItem cartItem, CancellationToken cancellationToken = default);
            Task<CartItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

            Task<bool> ExistsInCartAsync(string userId, string courseId, CancellationToken cancellationToken = default);

            /// <summary>
            /// Get all cart items for a specific user
            /// </summary>
            Task<IEnumerable<CartItem>> GetUserCartItemsAsync(string userId, CancellationToken cancellationToken = default);

            Task<CartItem?> GetByUserAndCourseAsync(string userId, string courseId, CancellationToken cancellationToken = default);

            Task<bool> DeleteByUserAndCourseAsync(string userId, string courseId, CancellationToken cancellationToken = default);
            Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

            Task<int> ClearUserCartAsync(string userId, CancellationToken cancellationToken = default);

            /// <summary>
            /// Get total cart value for user
            /// </summary>
            Task<decimal> GetTotalValueAsync(string userId, CancellationToken cancellationToken = default);

            /// <summary>
            /// Get cart item count for user
            /// </summary>
            Task<int> GetItemCountAsync(string userId, CancellationToken cancellationToken = default);
        }
    }
