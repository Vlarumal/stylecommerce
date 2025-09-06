using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StyleCommerce.Api.Controllers;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using Xunit;

namespace StyleCommerce.Api.Tests
{
    public class OrderControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly Mock<ILogger<OrderController>> _loggerMock;
        private readonly OrderController _controller;

        public OrderControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _orderServiceMock = new Mock<IOrderService>();
            _loggerMock = new Mock<ILogger<OrderController>>();

            _controller = new OrderController(_orderServiceMock.Object, _loggerMock.Object);

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
        public async Task CreateOrder_WithValidRequest_CreatesOrder()
        {
            var userId = "1";
            var request = new CreateOrderRequest { PaymentToken = "valid_token_1234567890" };
            var order = new Order
            {
                OrderId = 1,
                UserId = 1,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 20.99m,
                Status = "Processing",
            };
            var paymentResult = new PaymentResult
            {
                IsSuccess = true,
                TransactionId = "txn_12345",
                Amount = 20.99m,
                PaymentMethod = "Credit Card",
            };
            var response = new CreateOrderResponse { Order = order, PaymentResult = paymentResult };

            _orderServiceMock
                .Setup(x => x.PlaceOrderAsync(1, "valid_token_1234567890"))
                .ReturnsAsync(response);
            SetupControllerContext(userId);

            var result = await _controller.CreateOrder(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResponse = Assert.IsType<CreateOrderResponse>(okResult.Value);
            Assert.Equal(1, returnedResponse.Order.OrderId);
            Assert.Equal(1, returnedResponse.Order.UserId);
            Assert.True(returnedResponse.PaymentResult.IsSuccess);
        }

        [Fact]
        public async Task CreateOrder_WithInvalidUserId_ReturnsUnauthorized()
        {
            var request = new CreateOrderRequest { PaymentToken = "valid_token_1234567890" };
            SetupControllerContext(""); // Invalid user ID

            var result = await _controller.CreateOrder(request);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid user ID", unauthorizedResult.Value);
        }

        [Fact]
        public async Task CreateOrder_WithInvalidUserIdFormat_ReturnsBadRequest()
        {
            var request = new CreateOrderRequest { PaymentToken = "valid_token_1234567890" };
            SetupControllerContext("invalid"); // Invalid user ID format

            var result = await _controller.CreateOrder(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid user ID format", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateOrder_WithMissingPaymentToken_ReturnsBadRequest()
        {
            var userId = "1";
            var request = new CreateOrderRequest { PaymentToken = "" };
            SetupControllerContext(userId);

            var result = await _controller.CreateOrder(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Payment token is required", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateOrder_WithInvalidPaymentTokenFormat_ReturnsBadRequest()
        {
            var userId = "1";
            var request = new CreateOrderRequest { PaymentToken = "short" };
            SetupControllerContext(userId);

            var result = await _controller.CreateOrder(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid payment token format", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateOrder_WithOrderServiceException_ReturnsBadRequest()
        {
            var userId = "1";
            var request = new CreateOrderRequest { PaymentToken = "valid_token_1234567890" };
            var exceptionMessage = "Cannot place order with empty cart";

            _orderServiceMock
                .Setup(x => x.PlaceOrderAsync(1, "valid_token_1234567890"))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));
            SetupControllerContext(userId);

            var result = await _controller.CreateOrder(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(exceptionMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task CreateOrder_WithUnexpectedException_ReturnsInternalServerError()
        {
            var userId = "1";
            var request = new CreateOrderRequest { PaymentToken = "valid_token_1234567890" };

            _orderServiceMock
                .Setup(x => x.PlaceOrderAsync(1, "valid_token_1234567890"))
                .ThrowsAsync(new Exception("Unexpected error"));
            SetupControllerContext(userId);

            var result = await _controller.CreateOrder(request);

            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while creating the order", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetOrderHistory_WithValidUser_ReturnsOrders()
        {
            var userId = "1";
            var orders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    UserId = 1,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = 20.99m,
                    Status = "Processing",
                },
                new Order
                {
                    OrderId = 2,
                    UserId = 1,
                    OrderDate = DateTime.UtcNow.AddHours(-1),
                    TotalAmount = 15.99m,
                    Status = "Shipped",
                },
            };

            _orderServiceMock.Setup(x => x.GetOrderHistoryAsync(1)).ReturnsAsync(orders);
            SetupControllerContext(userId);

            var result = await _controller.GetOrderHistory();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrders = Assert.IsType<List<Order>>(okResult.Value);
            Assert.Equal(2, returnedOrders.Count);
            Assert.Equal(1, returnedOrders[0].UserId);
            Assert.Equal(1, returnedOrders[1].UserId);
        }

        [Fact]
        public async Task GetOrderHistory_WithInvalidUserId_ReturnsUnauthorized()
        {
            SetupControllerContext(""); // Invalid user ID

            var result = await _controller.GetOrderHistory();

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid user ID", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetOrderHistory_WithInvalidUserIdFormat_ReturnsBadRequest()
        {
            SetupControllerContext("invalid"); // Invalid user ID format

            var result = await _controller.GetOrderHistory();

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid user ID format", badRequestResult.Value);
        }

        [Fact]
        public async Task GetOrder_WithValidId_ReturnsOrder()
        {
            var userId = "1";
            var orderId = 1;
            var order = new Order
            {
                OrderId = orderId,
                UserId = 1,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 20.99m,
                Status = "Processing",
            };

            _orderServiceMock.Setup(x => x.GetOrderDetailsAsync(orderId)).ReturnsAsync(order);
            SetupControllerContext(userId);

            var result = await _controller.GetOrder(orderId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrder = Assert.IsType<Order>(okResult.Value);
            Assert.Equal(orderId, returnedOrder.OrderId);
            Assert.Equal(1, returnedOrder.UserId);
        }

        [Fact]
        public async Task GetOrder_WithInvalidId_ReturnsNotFound()
        {
            var userId = "1";
            var orderId = 999; // Non-existent order

            _orderServiceMock
                .Setup(x => x.GetOrderDetailsAsync(orderId))
                .ReturnsAsync(default(Order));
            SetupControllerContext(userId);

            var result = await _controller.GetOrder(orderId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Order with ID {orderId} not found", notFoundResult.Value);
        }

        [Fact]
        public async Task GetOrder_WithInvalidUserId_ReturnsUnauthorized()
        {
            var orderId = 1;
            SetupControllerContext(""); // Invalid user ID

            var result = await _controller.GetOrder(orderId);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid user ID", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetOrder_WithInvalidUserIdFormat_ReturnsBadRequest()
        {
            var orderId = 1;
            SetupControllerContext("invalid"); // Invalid user ID format

            var result = await _controller.GetOrder(orderId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid user ID format", badRequestResult.Value);
        }

        [Fact]
        public async Task GetOrder_WithUnauthorizedAccess_ReturnsForbidden()
        {
            var userId = "2"; // Different user
            var orderId = 1;
            var order = new Order
            {
                OrderId = orderId,
                UserId = 1, // Order belongs to user 1
                OrderDate = DateTime.UtcNow,
                TotalAmount = 20.99m,
                Status = "Processing",
            };

            _orderServiceMock.Setup(x => x.GetOrderDetailsAsync(orderId)).ReturnsAsync(order);
            SetupControllerContext(userId);

            var result = await _controller.GetOrder(orderId);

            var forbiddenResult = Assert.IsType<ForbidResult>(result.Result);
            Assert.Equal(
                "You don't have permission to access this order",
                forbiddenResult.AuthenticationSchemes.First()
            );
        }

        [Fact]
        public async Task UpdateOrderStatus_WithValidRequest_UpdatesStatus()
        {
            var orderId = 1;
            var request = new UpdateOrderStatusRequest { Status = "Processing" };
            var order = new Order
            {
                OrderId = orderId,
                UserId = 1,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 20.99m,
                Status = "Processing",
            };

            _orderServiceMock
                .Setup(x => x.UpdateOrderStatusAsync(orderId, "Processing"))
                .ReturnsAsync(order);

            var result = await _controller.UpdateOrderStatus(orderId, request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrder = Assert.IsType<Order>(okResult.Value);
            Assert.Equal(orderId, returnedOrder.OrderId);
            Assert.Equal("Processing", returnedOrder.Status);
        }

        [Fact]
        public async Task UpdateOrderStatus_WithInvalidId_ReturnsNotFound()
        {
            var orderId = 999; // Non-existent order
            var request = new UpdateOrderStatusRequest { Status = "Processing" };

            _orderServiceMock
                .Setup(x => x.UpdateOrderStatusAsync(orderId, "Processing"))
                .ReturnsAsync(default(Order));

            var result = await _controller.UpdateOrderStatus(orderId, request);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Order with ID {orderId} not found", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateOrderStatus_WithInvalidStatus_ReturnsBadRequest()
        {
            var orderId = 1;
            var request = new UpdateOrderStatusRequest { Status = "InvalidStatus" };
            var exceptionMessage = "Invalid order status: InvalidStatus";

            _orderServiceMock
                .Setup(x => x.UpdateOrderStatusAsync(orderId, "InvalidStatus"))
                .ThrowsAsync(new ArgumentException(exceptionMessage));

            var result = await _controller.UpdateOrderStatus(orderId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(exceptionMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateOrderStatus_WithInvalidTransition_ReturnsBadRequest()
        {
            var orderId = 1;
            var request = new UpdateOrderStatusRequest { Status = "Delivered" };
            var exceptionMessage = "Cannot transition from Pending to Delivered";

            _orderServiceMock
                .Setup(x => x.UpdateOrderStatusAsync(orderId, "Delivered"))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            var result = await _controller.UpdateOrderStatus(orderId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(exceptionMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateOrderStatus_WithUnexpectedException_ReturnsInternalServerError()
        {
            var orderId = 1;
            var request = new UpdateOrderStatusRequest { Status = "Processing" };

            _orderServiceMock
                .Setup(x => x.UpdateOrderStatusAsync(orderId, "Processing"))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.UpdateOrderStatus(orderId, request);

            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal(
                "An error occurred while updating the order status",
                statusCodeResult.Value
            );
        }

        [Fact]
        public async Task GetAvailableOrderStatuses_ReturnsStatuses()
        {
            var statuses = new List<string>
            {
                "Pending",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled",
                "Payment Failed",
            };

            _orderServiceMock.Setup(x => x.GetAvailableOrderStatusesAsync()).ReturnsAsync(statuses);

            var result = await _controller.GetAvailableOrderStatuses();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedStatuses = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(6, returnedStatuses.Count);
            Assert.Contains("Pending", returnedStatuses);
            Assert.Contains("Processing", returnedStatuses);
            Assert.Contains("Shipped", returnedStatuses);
            Assert.Contains("Delivered", returnedStatuses);
            Assert.Contains("Cancelled", returnedStatuses);
            Assert.Contains("Payment Failed", returnedStatuses);
        }

        private void SetupControllerContext(string userId)
        {
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim("UserId", userId) }, "mock")
            );

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user },
            };
        }
    }
}
