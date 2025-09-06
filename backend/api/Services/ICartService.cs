using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public interface ICartService
    {
        Task<Cart?> GetCartAsync(int? userId, string? sessionId);
        Task<Cart> CreateCartAsync(int? userId, string? sessionId);
        Task<CartItem?> AddItemToCartAsync(
            int? userId,
            string? sessionId,
            int productId,
            int quantity
        );
        Task<CartItem?> UpdateItemQuantityAsync(
            int? userId,
            string? sessionId,
            int productId,
            int quantity
        );
        Task RemoveItemFromCartAsync(int? userId, string? sessionId, int productId);
        Task<bool> ClearCartAsync(int? userId, string? sessionId);
        Task MergeCartsAsync(string sessionId, int userId);
        Task<bool> UpdateCartOwnershipAsync(int cartId, int userId);
    }
}
