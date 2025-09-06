using Microsoft.EntityFrameworkCore;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductService _productService;
        private readonly ILogger<CartService> _logger;

        public CartService(
            ApplicationDbContext context,
            IProductService productService,
            ILogger<CartService> logger
        )
        {
            _context = context;
            _productService = productService;
            _logger = logger;
        }

        public async Task<Cart?> GetCartAsync(int? userId, string? sessionId)
        {
            if (userId == null && sessionId == null)
            {
                _logger.LogWarning("Both userId and sessionId cannot be null");
                return null;
            }

            try
            {
                var cart = await _context
                    .Carts.Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product) // Eager load products
                    // Removed AsNoTracking() to allow tracking of entities for updates/deletes
                    .FirstOrDefaultAsync(c =>
                        (userId != null && c.UserId == userId)
                        || (sessionId != null && c.SessionId == sessionId)
                    );

                return cart;
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogError(
                    ex,
                    "Database context disposed error when retrieving cart for user ID: {UserId}, session ID: {SessionId}",
                    userId,
                    sessionId
                );
                // Return null for ObjectDisposedException as this indicates a database error
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving cart for user ID: {UserId}, session ID: {SessionId}",
                    userId,
                    sessionId
                );
                throw; // Re-throw the exception to be handled by GlobalExceptionHandlingMiddleware
            }
        }

        public async Task<Cart> CreateCartAsync(int? userId, string? sessionId)
        {
            _logger.LogInformation(
                "Creating new cart for user ID: {UserId}, session ID: {SessionId}",
                userId,
                sessionId
            );

            var cart = new Cart
            {
                UserId = userId,
                SessionId = sessionId,
                CreatedDate = DateTime.UtcNow,
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cart created successfully with ID: {CartId}", cart.Id);
            return cart;
        }

        public async Task<CartItem?> AddItemToCartAsync(
            int? userId,
            string? sessionId,
            int productId,
            int quantity
        )
        {
            _logger.LogInformation(
                "Adding item to cart for user ID: {UserId}, session ID: {SessionId}, product ID: {ProductId}, quantity: {Quantity}",
                userId,
                sessionId,
                productId,
                quantity
            );

            if (quantity <= 0)
            {
                _logger.LogWarning(
                    "Invalid quantity: {Quantity}. Quantity must be greater than zero",
                    quantity
                );
                return null;
            }

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", productId);
                return null;
            }

            if (product.StockQuantity < quantity)
            {
                _logger.LogWarning(
                    "Insufficient stock for product ID {ProductId}. Requested: {Quantity}, Available: {Stock}",
                    productId,
                    quantity,
                    product.StockQuantity
                );
                return null;
            }

            try
            {
                var cart = await GetCartAsync(userId, sessionId);
                if (cart == null)
                {
                    cart = await CreateCartAsync(userId, sessionId);
                }

                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.PriceSnapshot = product.Price;
                    existingItem.AddedDate = DateTime.UtcNow;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        PriceSnapshot = product.Price,
                    };
                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Item added to cart successfully");
                return existingItem
                    ?? _context.CartItems.FirstOrDefault(ci =>
                        ci.CartId == cart.Id && ci.ProductId == productId
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding item to cart for user ID: {UserId}, session ID: {SessionId}, product ID: {ProductId}",
                    userId,
                    sessionId,
                    productId
                );
                throw; // Re-throw the exception to be handled by GlobalExceptionHandlingMiddleware
            }
        }

        public async Task MergeCartsAsync(string sessionId, int userId)
        {
            _logger.LogInformation(
                "Merging guest cart (session: {SessionId}) into user cart (user: {UserId})",
                sessionId,
                userId
            );

            try
            {
                var guestCart = await GetCartAsync(null, sessionId);
                if (guestCart == null || !guestCart.CartItems.Any())
                {
                    _logger.LogInformation("No guest cart found to merge");
                    return;
                }

                var userCart = await GetCartAsync(userId, null);
                if (userCart == null)
                {
                    userCart = await CreateCartAsync(userId, null);
                }

                // Merge items
                foreach (var guestItem in guestCart.CartItems)
                {
                    var existingItem = userCart.CartItems.FirstOrDefault(ci =>
                        ci.ProductId == guestItem.ProductId
                    );
                    if (existingItem != null)
                    {
                        existingItem.Quantity += guestItem.Quantity;
                    }
                    else
                    {
                        userCart.CartItems.Add(
                            new CartItem
                            {
                                ProductId = guestItem.ProductId,
                                Quantity = guestItem.Quantity,
                                PriceSnapshot = guestItem.PriceSnapshot,
                                AddedDate = DateTime.UtcNow,
                            }
                        );
                    }
                }

                _context.Carts.Remove(guestCart);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cart merged successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error merging carts for session ID: {SessionId}, user ID: {UserId}",
                    sessionId,
                    userId
                );
                throw; // Re-throw the exception to be handled by GlobalExceptionHandlingMiddleware
            }
        }

        public async Task<CartItem?> UpdateItemQuantityAsync(
            int? userId,
            string? sessionId,
            int productId,
            int quantity
        )
        {
            _logger.LogInformation(
                "Updating item quantity in cart for user ID: {UserId}, session ID: {SessionId}, product ID: {ProductId}, quantity: {Quantity}",
                userId,
                sessionId,
                productId,
                quantity
            );

            if (quantity <= 0)
            {
                _logger.LogWarning(
                    "Invalid quantity: {Quantity}. Quantity must be greater than zero",
                    quantity
                );
                return null;
            }

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", productId);
                return null;
            }

            if (product.StockQuantity < quantity)
            {
                _logger.LogWarning(
                    "Insufficient stock for product ID {ProductId}. Requested: {Quantity}, Available: {Stock}",
                    productId,
                    quantity,
                    product.StockQuantity
                );
                return null;
            }

            try
            {
                var cart = await GetCartAsync(userId, sessionId);
                if (cart == null)
                {
                    _logger.LogWarning(
                        "Cart not found for user ID: {UserId}, session ID: {SessionId}",
                        userId,
                        sessionId
                    );
                    return null;
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (cartItem == null)
                {
                    _logger.LogWarning(
                        "Item with product ID {ProductId} not found in cart for user ID: {UserId}, session ID: {SessionId}",
                        productId,
                        userId,
                        sessionId
                    );
                    return null;
                }

                cartItem.Quantity = quantity;
                cartItem.PriceSnapshot = product.Price;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Item quantity updated successfully");
                return cartItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating item quantity in cart for user ID: {UserId}, session ID: {SessionId}, product ID: {ProductId}",
                    userId,
                    sessionId,
                    productId
                );
                throw; // Re-throw the exception to be handled by GlobalExceptionHandlingMiddleware
            }
        }

        public async Task RemoveItemFromCartAsync(int? userId, string? sessionId, int productId)
        {
            _logger.LogInformation(
                "Removing item from cart for user ID: {UserId}, session ID: {SessionId}, product ID: {ProductId}",
                userId,
                sessionId,
                productId
            );

            try
            {
                var cart = await GetCartAsync(userId, sessionId);
                if (cart == null)
                {
                    // If cart is null and it's not due to a database error, throw KeyNotFoundException
                    // We need to differentiate between a genuine "not found" and a database error
                    throw new KeyNotFoundException(
                        $"Cart not found for user ID: {userId}, session ID: {sessionId}"
                    );
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (cartItem == null)
                {
                    throw new KeyNotFoundException(
                        $"Item with product ID {productId} not found in cart"
                    );
                }

                cart.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Item removed from cart successfully");
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException))
            {
                _logger.LogError(
                    ex,
                    "Error removing item from cart for user ID: {UserId}, session ID: {SessionId}, product ID: {ProductId}",
                    userId,
                    sessionId,
                    productId
                );
                throw; // Re-throw the exception to be handled by GlobalExceptionHandlingMiddleware
            }
        }

        public async Task<bool> ClearCartAsync(int? userId, string? sessionId)
        {
            _logger.LogInformation(
                "Clearing cart for user ID: {UserId}, session ID: {SessionId}",
                userId,
                sessionId
            );

            try
            {
                var cart = await GetCartAsync(userId, sessionId);
                if (cart == null)
                {
                    _logger.LogWarning(
                        "Cart not found for user ID: {UserId}, session ID: {SessionId}",
                        userId,
                        sessionId
                    );
                    return false;
                }

                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cart cleared successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error clearing cart for user ID: {UserId}, session ID: {SessionId}",
                    userId,
                    sessionId
                );
                throw; // Re-throw the exception to be handled by GlobalExceptionHandlingMiddleware
            }
        }

        public async Task<bool> UpdateCartOwnershipAsync(int cartId, int userId)
        {
            try
            {
                var cart = await _context.Carts.FindAsync(cartId);
                if (cart == null)
                {
                    return false;
                }

                cart.UserId = userId;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating cart ownership for cart ID: {CartId}, user ID: {UserId}",
                    cartId,
                    userId
                );
                throw; // Re-throw the exception to be handled by GlobalExceptionHandlingMiddleware
            }
        }
    }
}
