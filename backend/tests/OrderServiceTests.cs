using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using Xunit;

namespace StyleCommerce.Tests
{
    public class OrderServiceTests : IDisposable
    {
        private readonly Mock<ICartService> _mockCartService;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<PaymentProcessor> _mockPaymentProcessor;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly ApplicationDbContext _context;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique database name for each test
                .Options;

            _context = new ApplicationDbContext(options);

            _mockCartService = new Mock<ICartService>();
            _mockProductService = new Mock<IProductService>();
            _mockPaymentProcessor = new Mock<PaymentProcessor>(
                Mock.Of<IPaymentTokenizationService>(),
                Mock.Of<ILogger<PaymentProcessor>>()
            );
            _mockLogger = new Mock<ILogger<OrderService>>();

            _orderService = new OrderService(
                _context,
                _mockCartService.Object,
                _mockProductService.Object,
                _mockPaymentProcessor.Object,
                _mockLogger.Object
            );
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task PlaceOrderAsync_WithValidCartAndPayment_ShouldCreateOrder()
        {
            var userId = 1;
            var paymentToken = "valid_token";

            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
            };

            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Price = 29.99m,
                StockQuantity = 10,
            };

            var cart = new Cart
            {
                Id = 1,
                UserId = userId,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        CartId = 1,
                        ProductId = 1,
                        Quantity = 2,
                        PriceSnapshot = 29.99m,
                    },
                },
            };

            var paymentResult = new PaymentResult
            {
                IsSuccess = true,
                TransactionId = "txn_12345",
                Amount = 59.98m,
                PaymentMethod = "Credit Card",
            };

            _context.Users.Add(user);
            _context.Products.Add(product);
            _context.SaveChanges();

            _mockCartService.Setup(x => x.GetCartAsync(userId, null)).ReturnsAsync(cart);
            _mockProductService.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);
            _mockPaymentProcessor
                .Setup(x => x.ProcessPaymentWithRetryAsync(paymentToken, 59.98m, 3))
                .ReturnsAsync(paymentResult);

            var result = await _orderService.PlaceOrderAsync(userId, paymentToken);

            Assert.NotNull(result);
            Assert.NotNull(result.Order);
            Assert.Equal(userId, result.Order.UserId);
            Assert.Equal(59.98m, result.Order.TotalAmount);
            Assert.Equal("Processing", result.Order.Status);
            Assert.Single(result.Order.OrderItems);

            _mockPaymentProcessor.Verify(
                x => x.ProcessPaymentWithRetryAsync(paymentToken, 59.98m, 3),
                Times.Once
            );

            _mockCartService.Verify(x => x.ClearCartAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task PlaceOrderAsync_WithEmptyCart_ShouldThrowException()
        {
            var userId = 1;
            var paymentToken = "valid_token";
            var cart = new Cart
            {
                Id = 1,
                UserId = userId,
                CartItems = new List<CartItem>(),
            };

            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            _mockCartService.Setup(x => x.GetCartAsync(userId, null)).ReturnsAsync(cart);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.PlaceOrderAsync(userId, paymentToken)
            );

            Assert.Equal("Cannot place order with empty cart", exception.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_WithInvalidProduct_ShouldThrowException()
        {
            var userId = 1;
            var paymentToken = "valid_token";

            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
            };

            var cart = new Cart
            {
                Id = 1,
                UserId = userId,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        CartId = 1,
                        ProductId = 999, // Non-existent product
                        Quantity = 1,
                        PriceSnapshot = 29.99m,
                    },
                },
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            _mockCartService.Setup(x => x.GetCartAsync(userId, null)).ReturnsAsync(cart);
            _mockProductService.Setup(x => x.GetProductByIdAsync(999)).ReturnsAsync(default(Product));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.PlaceOrderAsync(userId, paymentToken)
            );

            Assert.Equal("Product with ID 999 not found", exception.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_WithInsufficientStock_ShouldThrowException()
        {
            var userId = 1;
            var paymentToken = "valid_token";

            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
            };

            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Price = 29.99m,
                StockQuantity = 1, // Only 1 in stock
            };

            var cart = new Cart
            {
                Id = 1,
                UserId = userId,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        CartId = 1,
                        ProductId = 1,
                        Quantity = 5, // Requesting 5, but only 1 available
                        PriceSnapshot = 29.99m,
                    },
                },
            };

            _context.Users.Add(user);
            _context.Products.Add(product);
            _context.SaveChanges();

            _mockCartService.Setup(x => x.GetCartAsync(userId, null)).ReturnsAsync(cart);
            _mockProductService.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.PlaceOrderAsync(userId, paymentToken)
            );

            Assert.Equal("Insufficient stock for product Test Product", exception.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_WithPaymentFailure_ShouldUpdateOrderStatus()
        {
            var userId = 1;
            var paymentToken = "invalid_token";

            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
            };

            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Price = 29.99m,
                StockQuantity = 10,
            };

            var cart = new Cart
            {
                Id = 1,
                UserId = userId,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        CartId = 1,
                        ProductId = 1,
                        Quantity = 2,
                        PriceSnapshot = 29.99m,
                    },
                },
            };

            var paymentResult = new PaymentResult
            {
                IsSuccess = false,
                Message = "Payment declined",
                Amount = 59.98m,
            };

            _context.Users.Add(user);
            _context.Products.Add(product);
            _context.SaveChanges();

            _mockCartService.Setup(x => x.GetCartAsync(userId, null)).ReturnsAsync(cart);
            _mockProductService.Setup(x => x.GetProductByIdAsync(1)).ReturnsAsync(product);
            _mockPaymentProcessor
                .Setup(x => x.ProcessPaymentWithRetryAsync(paymentToken, 59.98m, 3))
                .ReturnsAsync(paymentResult);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.PlaceOrderAsync(userId, paymentToken)
            );

            Assert.Equal("Payment failed: Payment declined", exception.Message);

            var orders = _context.Orders.ToList();
            Assert.Single(orders);
            Assert.Equal("Payment Failed", orders[0].Status);
        }

        [Fact]
        public async Task GetOrderHistoryAsync_WithExistingOrders_ShouldReturnUserOrders()
        {
            var userId = 1;
            var otherUserId = 2;

            var user1 = new User
            {
                Id = userId,
                Username = "testuser1",
                Email = "test1@example.com",
            };

            var user2 = new User
            {
                Id = otherUserId,
                Username = "testuser2",
                Email = "test2@example.com",
            };

            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Price = 29.99m,
            };

            var order1 = new Order
            {
                OrderId = 1,
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 59.98m,
                Status = "Processing",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        OrderItemId = 1,
                        OrderId = 1,
                        ProductId = 1,
                        Quantity = 2,
                        Price = 29.99m,
                        Product = product,
                    },
                },
            };

            var order2 = new Order
            {
                OrderId = 2,
                UserId = userId,
                OrderDate = DateTime.UtcNow.AddHours(-1),
                TotalAmount = 29.99m,
                Status = "Shipped",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        OrderItemId = 2,
                        OrderId = 2,
                        ProductId = 1,
                        Quantity = 1,
                        Price = 29.99m,
                        Product = product,
                    },
                },
            };

            // Order for another user
            var order3 = new Order
            {
                OrderId = 3,
                UserId = otherUserId,
                OrderDate = DateTime.UtcNow.AddHours(-2),
                TotalAmount = 39.99m,
                Status = "Delivered",
            };

            _context.Users.AddRange(user1, user2);
            _context.Products.Add(product);
            _context.Orders.AddRange(order1, order2, order3);
            _context.SaveChanges();

            var result = await _orderService.GetOrderHistoryAsync(userId);

            Assert.NotNull(result);
            var orders = result.ToList();
            Assert.Equal(2, orders.Count);
            Assert.Contains(orders, o => o.OrderId == 1);
            Assert.Contains(orders, o => o.OrderId == 2);
            Assert.DoesNotContain(orders, o => o.OrderId == 3); // Other user's order
            Assert.All(orders, o => Assert.Equal(userId, o.UserId));
        }

        [Fact]
        public async Task GetOrderDetailsAsync_WithValidOrderId_ShouldReturnOrder()
        {
            var orderId = 1;
            var userId = 1;

            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
            };

            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Price = 29.99m,
            };

            var order = new Order
            {
                OrderId = orderId,
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 59.98m,
                Status = "Processing",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        OrderItemId = 1,
                        OrderId = orderId,
                        ProductId = 1,
                        Quantity = 2,
                        Price = 29.99m,
                        Product = product,
                    },
                },
            };

            _context.Users.Add(user);
            _context.Products.Add(product);
            _context.Orders.Add(order);
            _context.SaveChanges();

            var result = await _orderService.GetOrderDetailsAsync(orderId);

            Assert.NotNull(result);
            Assert.Equal(orderId, result.OrderId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(59.98m, result.TotalAmount);
            Assert.Single(result.OrderItems);
            Assert.Equal(1, result.OrderItems.First().ProductId);
        }

        [Fact]
        public async Task GetOrderDetailsAsync_WithInvalidOrderId_ShouldReturnNull()
        {
            var orderId = 999; // Non-existent order

            var result = await _orderService.GetOrderDetailsAsync(orderId);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithValidTransition_ShouldUpdateStatus()
        {
            var orderId = 1;
            var newStatus = "Processing";

            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
            };

            var order = new Order
            {
                OrderId = orderId,
                UserId = 1,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 59.98m,
                Status = "Pending",
            };

            _context.Users.Add(user);
            _context.Orders.Add(order);
            _context.SaveChanges();

            var result = await _orderService.UpdateOrderStatusAsync(orderId, newStatus);

            Assert.NotNull(result);
            Assert.Equal(newStatus, result.Status);

            var updatedOrder = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
            Assert.NotNull(updatedOrder);
            Assert.Equal(newStatus, updatedOrder.Status);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithInvalidStatus_ShouldThrowException()
        {
            var orderId = 1;
            var invalidStatus = "InvalidStatus";

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _orderService.UpdateOrderStatusAsync(orderId, invalidStatus)
            );

            Assert.Equal($"Invalid order status: {invalidStatus}", exception.Message);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithInvalidTransition_ShouldThrowException()
        {
            var orderId = 1;
            var invalidTransition = "Delivered"; // Can't go from Pending to Delivered directly

            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
            };

            var order = new Order
            {
                OrderId = orderId,
                UserId = 1,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 59.98m,
                Status = "Pending",
            };

            _context.Users.Add(user);
            _context.Orders.Add(order);
            _context.SaveChanges();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.UpdateOrderStatusAsync(orderId, invalidTransition)
            );

            Assert.Equal("Cannot transition from Pending to Delivered", exception.Message);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithNonExistentOrder_ShouldReturnNull()
        {
            var orderId = 999; // Non-existent order
            var status = "Processing";

            var result = await _orderService.UpdateOrderStatusAsync(orderId, status);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAvailableOrderStatusesAsync_ShouldReturnAllStatuses()
        {
            var result = await _orderService.GetAvailableOrderStatusesAsync();

            Assert.NotNull(result);
            var statuses = result.ToList();
            Assert.Contains("Pending", statuses);
            Assert.Contains("Processing", statuses);
            Assert.Contains("Shipped", statuses);
            Assert.Contains("Delivered", statuses);
            Assert.Contains("Cancelled", statuses);
            Assert.Contains("Payment Failed", statuses);
        }
    }
}
