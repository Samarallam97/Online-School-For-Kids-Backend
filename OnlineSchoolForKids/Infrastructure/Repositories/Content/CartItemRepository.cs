using Domain.Entities.Content;
using Domain.Interfaces.Repositories.Content;
using StackExchange.Redis;
using System.Text.Json;

public class CartItemRepository : ICartItemRepository
{
    private readonly IDatabase _redisDatabase;
    private const string CART_KEY_PREFIX = "cart:";
    private const string USER_CART_KEY = "user:cart:";

    public CartItemRepository(IConnectionMultiplexer redis)
    {
        _redisDatabase = redis.GetDatabase();
    }

    #region Helper Methods

    private string GetCartItemKey(string cartItemId)
        => $"{CART_KEY_PREFIX}{cartItemId}";

    private string GetUserCartKey(string userId)
        => $"{USER_CART_KEY}{userId}";

    private string GetUserCourseKey(string userId, string courseId)
        => $"{USER_CART_KEY}{userId}:course:{courseId}";

    #endregion

    /// <summary>
    /// Create cart item
    /// </summary>
    public async Task<CartItem> CreateAsync(
        CartItem cartItem,
        CancellationToken cancellationToken = default)
    {
        // Generate ID if not exists
        if (string.IsNullOrEmpty(cartItem.Id))
        {
            cartItem.Id = Guid.NewGuid().ToString();
        }

        cartItem.CreatedAt = DateTime.UtcNow;
        cartItem.UpdatedAt = DateTime.UtcNow;
        cartItem.IsDeleted = false;

        var cartItemKey = GetCartItemKey(cartItem.Id);
        var userCartKey = GetUserCartKey(cartItem.UserId);
        var userCourseKey = GetUserCourseKey(cartItem.UserId, cartItem.CourseId);

        // Serialize
        var json = JsonSerializer.Serialize(cartItem);

        // Batch operations for atomicity
        var batch = _redisDatabase.CreateBatch();

        var task1 = batch.StringSetAsync(cartItemKey, json);
        var task2 = batch.SetAddAsync(userCartKey, cartItem.Id);
        var task3 = batch.StringSetAsync(userCourseKey, cartItem.Id);

        batch.Execute();
        await Task.WhenAll(task1, task2, task3);

        return cartItem;
    }

    /// <summary>
    /// Get cart item by ID
    /// </summary>
    public async Task<CartItem?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var key = GetCartItemKey(id);
        var json = await _redisDatabase.StringGetAsync(key);

        if (json.IsNullOrEmpty)
            return null;

        var cartItem = JsonSerializer.Deserialize<CartItem>(json.ToString()!);

        // Check soft delete
        if (cartItem?.IsDeleted == true)
            return null;

        return cartItem;
    }


    public async Task<bool> ExistsInCartAsync(
        string userId,
        string courseId,
        CancellationToken cancellationToken = default)
    {
        var userCourseKey = GetUserCourseKey(userId, courseId);
        var cartItemId = await _redisDatabase.StringGetAsync(userCourseKey);

        if (cartItemId.IsNullOrEmpty)
            return false;

        // Verify cart item exists and not deleted
        var cartItem = await GetByIdAsync(cartItemId!, cancellationToken);
        return cartItem != null;
    }


    public async Task<IEnumerable<CartItem>> GetUserCartItemsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var userCartKey = GetUserCartKey(userId);
        var cartItemIds = await _redisDatabase.SetMembersAsync(userCartKey);  //Returns all items(cartItemIds) inside the set.       

        if (cartItemIds.Length == 0)
            return Enumerable.Empty<CartItem>();

        var cartItems = new List<CartItem>();

        foreach (var id in cartItemIds)
        {
            var cartItem = await GetByIdAsync(id!, cancellationToken);
            if (cartItem != null)
            {
                cartItems.Add(cartItem);
            }
        }

        return cartItems.OrderByDescending(c => c.CreatedAt);
    }

    /// <summary>
    /// Get cart item by user and course
    /// </summary>
    public async Task<CartItem?> GetByUserAndCourseAsync(
        string userId,
        string courseId,
        CancellationToken cancellationToken = default)
    {
        var userCourseKey = GetUserCourseKey(userId, courseId);
        var cartItemId = await _redisDatabase.StringGetAsync(userCourseKey);

        if (cartItemId.IsNullOrEmpty)
            return null;

        return await GetByIdAsync(cartItemId!, cancellationToken);
    }

    /// <summary>
    /// Delete cart item by user and course
    /// </summary>
    public async Task<bool> DeleteByUserAndCourseAsync(
        string userId,
        string courseId,
        CancellationToken cancellationToken = default)
    {
        var cartItem = await GetByUserAndCourseAsync(userId, courseId, cancellationToken);

        if (cartItem == null)
            return false;

        return await DeleteAsync(cartItem.Id, cancellationToken);
    }

    /// <summary>
    /// Delete cart item by ID (soft delete)
    /// </summary>
    public async Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var cartItem = await GetByIdAsync(id, cancellationToken);

        if (cartItem == null)
            return false;

        // Soft delete
        cartItem.IsDeleted = true;
        cartItem.UpdatedAt = DateTime.UtcNow;

        var cartItemKey = GetCartItemKey(id);
        var json = JsonSerializer.Serialize(cartItem);

        // Update cart item as deleted
        await _redisDatabase.StringSetAsync(cartItemKey, json);

        // Remove from user's cart set
        var userCartKey = GetUserCartKey(cartItem.UserId);
        await _redisDatabase.SetRemoveAsync(userCartKey, id);

        // Remove user-course mapping
        var userCourseKey = GetUserCourseKey(cartItem.UserId, cartItem.CourseId);
        await _redisDatabase.KeyDeleteAsync(userCourseKey);

        return true;
    }

    /// <summary>
    /// Clear all cart items for user
    /// </summary>
    public async Task<int> ClearUserCartAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var cartItems = await GetUserCartItemsAsync(userId, cancellationToken);
        var deletedCount = 0;

        foreach (var item in cartItems)
        {
            if (await DeleteAsync(item.Id, cancellationToken))
            {
                deletedCount++;
            }
        }

        return deletedCount;
    }

    /// <summary>
    /// Get total cart value
    /// </summary>
    public async Task<decimal> GetTotalValueAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var cartItems = await GetUserCartItemsAsync(userId, cancellationToken);
        return cartItems.Sum(c => c.Price);
    }

    /// <summary>
    /// Get cart item count
    /// </summary>
    public async Task<int> GetItemCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var userCartKey = GetUserCartKey(userId);
        var count = await _redisDatabase.SetLengthAsync(userCartKey);
        return (int)count;
    }
}

