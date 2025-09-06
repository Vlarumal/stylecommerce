using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleCommerce.Api.DTOs;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;

namespace StyleCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            IProductService productService,
            ILogger<CartController> logger
        )
        {
            _cartService = cartService;
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            _logger.LogInformation("Getting cart for current user");

            var userId = GetUserIdFromToken();
            string? sessionId = null;

            if (userId <= 0)
            {
                sessionId = Request.Cookies["CartSessionId"];
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = Guid.NewGuid().ToString();
                    Response.Cookies.Append(
                        "CartSessionId",
                        sessionId,
                        new Microsoft.AspNetCore.Http.CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                            HttpOnly = true,
                            Secure = true,
                            IsEssential = true,
                            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                        }
                    );
                }
            }

            var cart = await _cartService.GetCartAsync(userId > 0 ? userId : null, sessionId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId > 0 ? userId : null,
                    SessionId = sessionId,
                    CreatedDate = DateTime.UtcNow,
                    CartItems = new List<CartItem>(),
                };
            }

            if (userId > 0 && cart.UserId.HasValue && cart.UserId.Value != userId)
            {
                return Forbid();
            }

            if (userId > 0 && !cart.UserId.HasValue)
            {
                cart.UserId = userId;
                await _cartService.UpdateCartOwnershipAsync(cart.Id, userId);
            }

            var cartDto = cart.ToDto();
            return Ok(cartDto);
        }

        [HttpPost("add")]
        [AllowAnonymous]
        public async Task<ActionResult<CartItemDto>> AddToCart([FromBody] AddToCartRequest request)
        {
            _logger.LogInformation(
                "Adding item to cart. Product ID: {ProductId}, Quantity: {Quantity}",
                request.ProductId,
                request.Quantity
            );

            if (request.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero");
            }

            var product = await _productService.GetProductByIdAsync(request.ProductId);
            if (product == null)
            {
                return BadRequest("Invalid request");
            }

            var userId = GetUserIdFromToken();
            string? sessionId = null;

            if (userId <= 0)
            {
                sessionId = Request.Cookies["CartSessionId"];
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest("Cart session not found");
                }
                Response.Cookies.Append(
                    "CartSessionId",
                    sessionId,
                    new Microsoft.AspNetCore.Http.CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                        HttpOnly = true,
                        Secure = true,
                        IsEssential = true,
                        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    }
                );
            }

            var cart = await _cartService.GetCartAsync(userId > 0 ? userId : null, sessionId);
            if (cart != null)
            {
                if (userId > 0)
                {
                    if (cart.UserId.HasValue && cart.UserId.Value != userId)
                    {
                        return Forbid();
                    }
                    if (!cart.UserId.HasValue)
                    {
                        await _cartService.UpdateCartOwnershipAsync(cart.Id, userId);
                    }
                }
                else
                {
                    if (cart.SessionId != sessionId)
                    {
                        return Forbid();
                    }
                }
            }

            var cartItem = await _cartService.AddItemToCartAsync(
                userId > 0 ? userId : null,
                sessionId,
                request.ProductId,
                request.Quantity
            );
            if (cartItem == null)
            {
                return BadRequest("Failed to add item to cart. Check product availability.");
            }

            var cartItemDto = cartItem.ToDto();
            return Ok(cartItemDto);
        }

        [HttpPut("update")]
        [AllowAnonymous]
        public async Task<ActionResult<CartItemDto>> UpdateItemQuantity(
            [FromBody] UpdateQuantityRequest request
        )
        {
            _logger.LogInformation(
                "Updating item quantity in cart. Product ID: {ProductId}, Quantity: {Quantity}",
                request.ProductId,
                request.Quantity
            );

            if (request.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero");
            }

            var product = await _productService.GetProductByIdAsync(request.ProductId);
            if (product == null)
            {
                return BadRequest("Invalid request");
            }

            var userId = GetUserIdFromToken();
            string? sessionId = null;

            if (userId <= 0)
            {
                sessionId = Request.Cookies["CartSessionId"];
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest("Cart session not found");
                }
                Response.Cookies.Append(
                    "CartSessionId",
                    sessionId,
                    new Microsoft.AspNetCore.Http.CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                        HttpOnly = true,
                        Secure = true,
                        IsEssential = true,
                        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    }
                );
            }

            var cart = await _cartService.GetCartAsync(userId > 0 ? userId : null, sessionId);
            if (cart != null)
            {
                if (userId > 0)
                {
                    if (cart.UserId.HasValue && cart.UserId.Value != userId)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    if (cart.SessionId != sessionId)
                    {
                        return Forbid();
                    }
                }
            }

            var cartItem = await _cartService.UpdateItemQuantityAsync(
                userId > 0 ? userId : null,
                sessionId,
                request.ProductId,
                request.Quantity
            );
            if (cartItem == null)
            {
                return BadRequest(
                    "Failed to update item quantity. Check if item exists in cart and product availability."
                );
            }

            var cartItemDto = cartItem.ToDto();
            return Ok(cartItemDto);
        }

        [HttpDelete("remove")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveItemFromCart([FromQuery] int productId)
        {
            _logger.LogInformation("Removing item from cart. Product ID: {ProductId}", productId);

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                return BadRequest("Invalid request");
            }

            var userId = GetUserIdFromToken();
            string? sessionId = null;

            if (userId <= 0)
            {
                sessionId = Request.Cookies["CartSessionId"];
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest("Cart session not found");
                }
                Response.Cookies.Append(
                    "CartSessionId",
                    sessionId,
                    new Microsoft.AspNetCore.Http.CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                        HttpOnly = true,
                        Secure = true,
                        IsEssential = true,
                        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    }
                );
            }

            var cart = await _cartService.GetCartAsync(userId > 0 ? userId : null, sessionId);
            if (cart != null)
            {
                if (userId > 0)
                {
                    if (cart.UserId.HasValue && cart.UserId.Value != userId)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    if (cart.SessionId != sessionId)
                    {
                        return Forbid();
                    }
                }
            }

            try
            {
                await _cartService.RemoveItemFromCartAsync(
                    userId > 0 ? userId : null,
                    sessionId,
                    productId
                );
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Cart item not found for removal. Product ID: {ProductId}",
                    productId
                );
                return NotFound("Cart item not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("merge")]
        [Authorize]
        public async Task<IActionResult> MergeCarts([FromQuery] string sessionId)
        {
            var userId = GetUserIdFromToken();
            if (userId <= 0)
            {
                return Unauthorized("Invalid user credentials");
            }

            var sessionCart = await _cartService.GetCartAsync(null, sessionId);
            if (sessionCart == null || sessionCart.UserId.HasValue)
            {
                return BadRequest("Invalid guest session");
            }

            await _cartService.MergeCartsAsync(sessionId, userId);

            var newSessionId = Guid.NewGuid().ToString();
            Response.Cookies.Append(
                "CartSessionId",
                newSessionId,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                    HttpOnly = true,
                    Secure = true,
                    IsEssential = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                }
            );

            return Ok("Cart merged successfully. New session ID generated.");
        }

        private int GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return -1;
        }

        [HttpPost("admin/clear")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminClearCart([FromQuery] int userId)
        {
            var result = await _cartService.ClearCartAsync(userId, null);
            if (!result)
            {
                return BadRequest("Failed to clear cart");
            }
            return NoContent();
        }
    }

    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
