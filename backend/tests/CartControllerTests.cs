using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StyleCommerce.Api.Controllers;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.DTOs;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using Xunit;

namespace StyleCommerce.Api.Tests
{
    public class CartControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ICartService> _cartServiceMock;
        private readonly Mock<IProductService> _productServiceMock;
        private readonly Mock<ILogger<CartController>> _loggerMock;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _cartServiceMock = new Mock<ICartService>();
            _productServiceMock = new Mock<IProductService>();
            _loggerMock = new Mock<ILogger<CartController>>();

            _controller = new CartController(
                _cartServiceMock.Object,
                _productServiceMock.Object,
                _loggerMock.Object
            );

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _context.Users.AddRange(
                new User
                {
                    Id = 1,
                    Username = "testuser1",
                    Email = "test1@example.com",
                },
                new User
                {
                    Id = 2,
                    Username = "testuser2",
                    Email = "test2@example.com",
                }
            );

            _context.Products.AddRange(
                new Product
                {
                    Id = 1,
                    Name = "Product 1",
                    Price = 10.99m,
                    CategoryId = 1,
                    StockQuantity = 10,
                },
                new Product
                {
                    Id = 2,
                    Name = "Product 2",
                    Price = 15.99m,
                    CategoryId = 2,
                    StockQuantity = 5,
                }
            );

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task GetCart_WithValidUser_ReturnsCart()
        {
            var userId = 1;
            var cart = new Cart
            {
                Id = 1,
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        CartId = 1,
                        ProductId = 1,
                        Quantity = 2,
                        PriceSnapshot = 10.99m,
                        AddedDate = DateTime.UtcNow,
                        Product = new Product
                        {
                            Id = 1,
                            Name = "Product 1",
                            Price = 10.99m,
                            CategoryId = 1,
                            StockQuantity = 10,
                        },
                    },
                },
            };

            _cartServiceMock.Setup(x => x.GetCartAsync(userId, null)).ReturnsAsync(cart);

            SetupControllerContext(userId);

            var result = await _controller.GetCart();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCart = Assert.IsType<CartDto>(okResult.Value);
            Assert.Equal(userId, returnedCart.UserId);
            Assert.Single(returnedCart.CartItems);
            Assert.Equal(1, returnedCart.CartItems.First().ProductId);
            Assert.Equal(2, returnedCart.CartItems.First().Quantity);
        }

        [Fact]
        public async Task GetCart_WithValidUserButNoCart_ReturnsEmptyCart()
        {
            var userId = 1;
            _cartServiceMock.Setup(x => x.GetCartAsync(userId, null)).ReturnsAsync(default(Cart));

            SetupControllerContext(userId);

            var result = await _controller.GetCart();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCart = Assert.IsType<CartDto>(okResult.Value);
            Assert.Equal(userId, returnedCart.UserId);
            Assert.Empty(returnedCart.CartItems);
        }

        [Fact]
        public async Task GetCart_WithInvalidUserId_ReturnsCart()
        {
            SetupControllerContext(-1); // Invalid user ID

            var result = await _controller.GetCart();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCart = Assert.IsType<CartDto>(okResult.Value);
            Assert.Null(returnedCart.UserId);
            Assert.Empty(returnedCart.CartItems);
        }

        [Fact]
        public async Task AddToCart_WithValidItem_AddsItemToCart()
        {
            var userId = 1;
            var request = new AddToCartRequest { ProductId = 1, Quantity = 2 };
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            var cartItem = new CartItem
            {
                Id = 1,
                CartId = 1,
                ProductId = 1,
                Quantity = 2,
                PriceSnapshot = 10.99m,
                AddedDate = DateTime.UtcNow,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);
            _cartServiceMock
                .Setup(x => x.AddItemToCartAsync(userId, null, 1, 2))
                .ReturnsAsync(cartItem);

            SetupControllerContext(userId);

            var result = await _controller.AddToCart(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCartItem = Assert.IsType<CartItemDto>(okResult.Value);
            Assert.Equal(1, returnedCartItem.ProductId);
            Assert.Equal(2, returnedCartItem.Quantity);
            Assert.Equal(10.99m, returnedCartItem.PriceSnapshot);
        }

        [Fact]
        public async Task AddToCart_WithInvalidProduct_ReturnsBadRequest()
        {
            var userId = 1;
            var request = new AddToCartRequest { ProductId = 999, Quantity = 1 }; // Non-existent product

            _productServiceMock
                .Setup(x => x.GetProductByIdAsync(999))
                .ReturnsAsync(default(Product));

            SetupControllerContext(userId);

            var result = await _controller.AddToCart(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid request", badRequestResult.Value);
        }

        [Fact]
        public async Task AddToCart_WithInvalidQuantity_ReturnsBadRequest()
        {
            var userId = 1;
            var request = new AddToCartRequest { ProductId = 1, Quantity = 0 };

            SetupControllerContext(userId);

            var result = await _controller.AddToCart(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Quantity must be greater than zero", badRequestResult.Value);
        }

        [Fact]
        public async Task AddToCart_WithInvalidUserId_ReturnsBadRequest()
        {
            var request = new AddToCartRequest { ProductId = 1, Quantity = 2 };
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);
            SetupControllerContext(-1); // Invalid user ID

            var result = await _controller.AddToCart(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Cart session not found", badRequestResult.Value);
        }

        [Fact]
        public async Task AddToCart_WithCartServiceFailure_ReturnsBadRequest()
        {
            var userId = 1;
            var request = new AddToCartRequest { ProductId = 1, Quantity = 2 };
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);
            _cartServiceMock
                .Setup(x => x.AddItemToCartAsync(userId, null, 1, 2))
                .ReturnsAsync(default(CartItem)); // Service failure

            SetupControllerContext(userId);

            var result = await _controller.AddToCart(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(
                "Failed to add item to cart. Check product availability.",
                badRequestResult.Value
            );
        }

        [Fact]
        public async Task UpdateItemQuantity_WithValidItem_UpdatesQuantity()
        {
            var userId = 1;
            var request = new UpdateQuantityRequest { ProductId = 1, Quantity = 5 };
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            var cartItem = new CartItem
            {
                Id = 1,
                CartId = 1,
                ProductId = 1,
                Quantity = 5,
                PriceSnapshot = 10.99m,
                AddedDate = DateTime.UtcNow,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);
            _cartServiceMock
                .Setup(x => x.UpdateItemQuantityAsync(userId, null, 1, 5))
                .ReturnsAsync(cartItem);

            SetupControllerContext(userId);

            var result = await _controller.UpdateItemQuantity(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCartItem = Assert.IsType<CartItemDto>(okResult.Value);
            Assert.Equal(1, returnedCartItem.ProductId);
            Assert.Equal(5, returnedCartItem.Quantity);
            Assert.Equal(10.99m, returnedCartItem.PriceSnapshot);
        }

        [Fact]
        public async Task UpdateItemQuantity_WithInvalidProduct_ReturnsBadRequest()
        {
            var userId = 1;
            var request = new UpdateQuantityRequest { ProductId = 999, Quantity = 1 };

            _productServiceMock
                .Setup(x => x.GetProductByIdAsync(999))
                .ReturnsAsync(default(Product));

            SetupControllerContext(userId);

            var result = await _controller.UpdateItemQuantity(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid request", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateItemQuantity_WithInvalidQuantity_ReturnsBadRequest()
        {
            var userId = 1;
            var request = new UpdateQuantityRequest { ProductId = 1, Quantity = 0 };

            SetupControllerContext(userId);

            var result = await _controller.UpdateItemQuantity(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Quantity must be greater than zero", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateItemQuantity_WithInvalidUserId_ReturnsBadRequest()
        {
            var request = new UpdateQuantityRequest { ProductId = 1, Quantity = 2 };
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);
            SetupControllerContext(-1); // Invalid user ID

            var result = await _controller.UpdateItemQuantity(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Cart session not found", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateItemQuantity_WithCartServiceFailure_ReturnsBadRequest()
        {
            var userId = 1;
            var request = new UpdateQuantityRequest { ProductId = 1, Quantity = 5 };
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);
            _cartServiceMock
                .Setup(x => x.UpdateItemQuantityAsync(userId, null, 1, 5))
                .ReturnsAsync(default(CartItem)); // Service failure

            SetupControllerContext(userId);

            var result = await _controller.UpdateItemQuantity(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(
                "Failed to update item quantity. Check if item exists in cart and product availability.",
                badRequestResult.Value
            );
        }

        [Fact]
        public async Task RemoveFromCart_WithValidItem_RemovesItem()
        {
            var userId = 1;
            var productId = 1;
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(productId)).ReturnsAsync(product);
            _cartServiceMock.Setup(x => x.RemoveItemFromCartAsync(userId, null, productId));

            SetupControllerContext(userId);

            var result = await _controller.RemoveItemFromCart(productId);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task RemoveFromCart_WithInvalidProduct_ReturnsBadRequest()
        {
            var userId = 1;
            var productId = 999; // Non-existent product

            _productServiceMock
                .Setup(x => x.GetProductByIdAsync(productId))
                .ReturnsAsync(default(Product));

            SetupControllerContext(userId);

            var result = await _controller.RemoveItemFromCart(productId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request", badRequestResult.Value);
        }

        [Fact]
        public async Task RemoveFromCart_WithInvalidUserId_ReturnsBadRequest()
        {
            var productId = 1;
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(productId)).ReturnsAsync(product);
            SetupControllerContext(-1); // Invalid user ID

            var result = await _controller.RemoveItemFromCart(productId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Cart session not found", badRequestResult.Value);
        }

        [Fact]
        public async Task RemoveFromCart_WithCartServiceFailure_ReturnsNotFound()
        {
            var userId = 1;
            var productId = 1;
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };

            _productServiceMock.Setup(x => x.GetProductByIdAsync(productId)).ReturnsAsync(product);
            _cartServiceMock
                .Setup(x => x.RemoveItemFromCartAsync(userId, null, productId))
                .Throws(new KeyNotFoundException("Cart not found for user ID: 1, session ID: "));

            SetupControllerContext(userId);

            var result = await _controller.RemoveItemFromCart(productId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Cart item not found", notFoundResult.Value);
        }

        private void SetupControllerContext(int userId)
        {
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim("UserId", userId.ToString()) }, "mock")
            );

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user },
            };
        }
    }
}
