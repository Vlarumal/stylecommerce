using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using Xunit;

namespace StyleCommerce.Api.Tests
{
    public class CartServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly Mock<IProductService> _productServiceMock;
        private readonly Mock<ILogger<CartService>> _loggerMock;

        public CartServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique database name for each test
                .Options;

            _context = new ApplicationDbContext(options);
            _productServiceMock = new Mock<IProductService>();
            _loggerMock = new Mock<ILogger<CartService>>();
            _cartService = new CartService(
                _context,
                _productServiceMock.Object,
                _loggerMock.Object
            );

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
        public async Task GetCartAsync_ExistingUserWithCart_ReturnsCart()
        {
            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.CartItems.Add(
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 1,
                    Quantity = 2,
                    PriceSnapshot = 10.99m,
                }
            );
            _context.SaveChanges();

            var result = await _cartService.GetCartAsync(1, null);

            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Single(result.CartItems);
        }

        [Fact]
        public async Task GetCartAsync_ExistingUserWithoutCart_ReturnsNull()
        {
            var result = await _cartService.GetCartAsync(1, null);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateCartAsync_ValidUserId_ReturnsNewCart()
        {
            var result = await _cartService.CreateCartAsync(1, null);

            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.True(result.Id > 0);
            Assert.True(result.CreatedDate <= DateTime.UtcNow);
        }

        [Fact]
        public async Task AddItemToCartAsync_ValidItem_AddsItemToCart()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var result = await _cartService.AddItemToCartAsync(1, null, 1, 2);

            Assert.NotNull(result);
            Assert.Equal(1, result.CartId);
            Assert.Equal(1, result.ProductId);
            Assert.Equal(2, result.Quantity);
            Assert.Equal(10.99m, result.PriceSnapshot);

            var cart = await _context
                .Carts.Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == 1);
            Assert.NotNull(cart);
            Assert.Single(cart.CartItems);
        }

        [Fact]
        public async Task AddItemToCartAsync_ExistingItem_UpdatesQuantity()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.CartItems.Add(
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 1,
                    Quantity = 2,
                    PriceSnapshot = 10.99m,
                }
            );
            _context.SaveChanges();

            var result = await _cartService.AddItemToCartAsync(1, null, 1, 3);

            Assert.NotNull(result);
            Assert.Equal(1, result.CartId);
            Assert.Equal(1, result.ProductId);
            Assert.Equal(5, result.Quantity); // 1 + 1 + 3
            Assert.Equal(10.99m, result.PriceSnapshot);
        }

        [Fact]
        public async Task AddItemToCartAsync_InvalidProduct_ReturnsNull()
        {
            _productServiceMock
                .Setup(x => x.GetProductByIdAsync(999))
                .ReturnsAsync(default(Product));

            var result = await _cartService.AddItemToCartAsync(1, null, 999, 1);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddItemToCartAsync_InsufficientStock_ReturnsNull()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 5,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var result = await _cartService.AddItemToCartAsync(1, null, 1, 10);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_ValidItem_UpdatesQuantity()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.CartItems.Add(
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 1,
                    Quantity = 2,
                    PriceSnapshot = 10.99m,
                }
            );
            _context.SaveChanges();

            var result = await _cartService.UpdateItemQuantityAsync(1, null, 1, 5);

            Assert.NotNull(result);
            Assert.Equal(1, result.CartId);
            Assert.Equal(1, result.ProductId);
            Assert.Equal(5, result.Quantity);
            Assert.Equal(10.99m, result.PriceSnapshot);
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_InvalidQuantity_ReturnsNull()
        {
            var result = await _cartService.UpdateItemQuantityAsync(1, null, 1, 0);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_InvalidProduct_ReturnsNull()
        {
            _productServiceMock
                .Setup(x => x.GetProductByIdAsync(999))
                .ReturnsAsync(default(Product));

            var result = await _cartService.UpdateItemQuantityAsync(1, null, 999, 1);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_InsufficientStock_ReturnsNull()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 5,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.CartItems.Add(
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 1,
                    Quantity = 2,
                    PriceSnapshot = 10.99m,
                }
            );
            _context.SaveChanges();

            var result = await _cartService.UpdateItemQuantityAsync(1, null, 1, 10);

            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveItemFromCartAsync_ExistingItem_RemovesItem()
        {
            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.CartItems.Add(
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 1,
                    Quantity = 2,
                    PriceSnapshot = 10.99m,
                }
            );
            _context.SaveChanges();

            await _cartService.RemoveItemFromCartAsync(1, null, 1);

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci =>
                ci.CartId == cart.Id && ci.ProductId == 1
            );
            Assert.Null(cartItem);
        }

        [Fact]
        public async Task RemoveItemFromCartAsync_NonExistingItem_ThrowsKeyNotFoundException()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _cartService.RemoveItemFromCartAsync(1, null, 999)
            );
        }

        [Fact]
        public async Task RemoveItemFromCartAsync_ShouldHandleDatabaseErrors()
        {
            // Simulate database error by disposing context first
            _context.Dispose();

            // Since we're testing database error handling, we expect a KeyNotFoundException to be thrown
            // because GetCartAsync returns null when there's a database error
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _cartService.RemoveItemFromCartAsync(1, null, 1)
            );
        }

        [Fact]
        public async Task ClearCartAsync_ExistingCart_ClearsAllItems()
        {
            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.CartItems.AddRange(
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 1,
                    Quantity = 2,
                    PriceSnapshot = 10.99m,
                },
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 2,
                    Quantity = 1,
                    PriceSnapshot = 15.99m,
                }
            );
            _context.SaveChanges();

            var result = await _cartService.ClearCartAsync(1, null);

            Assert.True(result);

            var cartItems = await _context
                .CartItems.Where(ci => ci.CartId == cart.Id)
                .ToListAsync();
            Assert.Empty(cartItems);
        }

        [Fact]
        public async Task ClearCartAsync_NonExistingCart_ReturnsFalse()
        {
            var result = await _cartService.ClearCartAsync(999, null);

            Assert.False(result);
        }

        [Fact]
        public async Task AddItemToCartAsync_NegativeQuantity_ReturnsNull()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var result = await _cartService.AddItemToCartAsync(1, null, 1, -1);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddItemToCartAsync_ZeroQuantity_ReturnsNull()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var result = await _cartService.AddItemToCartAsync(1, null, 1, 0);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddItemToCartAsync_CreatesNewCart_WhenCartDoesNotExist()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var user = await _context.Users.FindAsync(1);
            Assert.NotNull(user);

            var result = await _cartService.AddItemToCartAsync(1, null, 1, 2);

            Assert.NotNull(result);
            Assert.Equal(1, result.ProductId);
            Assert.Equal(2, result.Quantity);
            Assert.Equal(10.99m, result.PriceSnapshot);

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == 1);
            Assert.NotNull(cart);
            Assert.Equal(1, cart.UserId);
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_CartDoesNotExist_ReturnsNull()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var result = await _cartService.UpdateItemQuantityAsync(999, null, 1, 2);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_ItemDoesNotExistInCart_ReturnsNull()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.SaveChanges();

            var result = await _cartService.UpdateItemQuantityAsync(1, null, 1, 2);

            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveItemFromCartAsync_CartDoesNotExist_ThrowsKeyNotFoundException()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _cartService.RemoveItemFromCartAsync(999, null, 1)
            );
        }

        [Fact]
        public async Task RemoveItemFromCartAsync_ItemDoesNotExistInCart_ThrowsKeyNotFoundException()
        {
            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.SaveChanges();

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _cartService.RemoveItemFromCartAsync(1, null, 999)
            );
        }

        [Fact]
        public async Task ClearCartAsync_EmptyCart_ReturnsTrue()
        {
            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.SaveChanges();

            var result = await _cartService.ClearCartAsync(1, null);

            Assert.True(result);

            var cartItems = await _context
                .CartItems.Where(ci => ci.CartId == cart.Id)
                .ToListAsync();
            Assert.Empty(cartItems);
        }

        [Fact]
        public async Task AddItemToCartAsync_UpdatesPriceSnapshot_WhenProductPriceChanges()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.CartItems.Add(
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 1,
                    Quantity = 2,
                    PriceSnapshot = 9.99m, // Old price
                }
            );
            _context.SaveChanges();

            var result = await _cartService.AddItemToCartAsync(1, null, 1, 3);

            Assert.NotNull(result);
            Assert.Equal(1, result.CartId);
            Assert.Equal(1, result.ProductId);
            Assert.Equal(5, result.Quantity); // 1 + 1 + 3
            Assert.Equal(10.99m, result.PriceSnapshot); // Updated to current price
        }

        [Fact]
        public async Task AddItemToCartAsync_UpdatesAddedDate_WhenItemExists()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 10.99m,
                CategoryId = 1,
                StockQuantity = 10,
            };
            _productServiceMock.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var oldDate = DateTime.UtcNow.AddDays(-1);
            var cart = new Cart { UserId = 1, CreatedDate = DateTime.UtcNow };
            _context.Carts.Add(cart);
            _context.CartItems.Add(
                new CartItem
                {
                    CartId = cart.Id,
                    ProductId = 1,
                    Quantity = 2,
                    PriceSnapshot = 10.99m,
                    AddedDate = oldDate,
                }
            );
            _context.SaveChanges();

            var result = await _cartService.AddItemToCartAsync(1, null, 1, 3);

            Assert.NotNull(result);
            Assert.True(result.AddedDate > oldDate); // Updated to current date
        }
    }
}
