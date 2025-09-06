using Microsoft.EntityFrameworkCore;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly PaymentProcessor _paymentProcessor;
        private readonly ILogger<OrderService> _logger;

        private static readonly List<string> AvailableOrderStatuses = new()
        {
            "Pending",
            "Processing",
            "Shipped",
            "Delivered",
            "Cancelled",
            "Payment Failed",
        };

        public OrderService(
            ApplicationDbContext context,
            ICartService cartService,
            IProductService productService,
            PaymentProcessor paymentProcessor,
            ILogger<OrderService> logger
        )
        {
            _context = context;
            _cartService = cartService;
            _productService = productService;
            _paymentProcessor = paymentProcessor;
            _logger = logger;
        }

        public async Task<CreateOrderResponse> PlaceOrderAsync(int userId, string paymentToken)
        {
            _logger.LogInformation("Placing order for user ID: {UserId}", userId);

            var cart = await _cartService.GetCartAsync(userId, null);
            if (cart == null || !cart.CartItems.Any())
            {
                throw new InvalidOperationException("Cannot place order with empty cart");
            }

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cart.CartItems)
            {
                var product = await _productService.GetProductByIdAsync(cartItem.ProductId);
                if (product == null)
                {
                    throw new InvalidOperationException(
                        $"Product with ID {cartItem.ProductId} not found"
                    );
                }

                if (product.StockQuantity < cartItem.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {product.Name}"
                    );
                }

                totalAmount += cartItem.PriceSnapshot * cartItem.Quantity;

                orderItems.Add(
                    new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.PriceSnapshot,
                    }
                );
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Status = "Pending",
            };

            order.OrderItems = orderItems;

            _context.Orders.Add(order);

            var paymentResult = await _paymentProcessor.ProcessPaymentWithRetryAsync(
                paymentToken,
                totalAmount
            );

            if (!paymentResult.IsSuccess)
            {
                order.Status = "Payment Failed";
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Payment failed for order ID: {OrderId}, amount: {Amount}, message: {Message}",
                    order.OrderId,
                    totalAmount,
                    paymentResult.Message
                );

                throw new InvalidOperationException($"Payment failed: {paymentResult.Message}");
            }

            order.Status = "Processing";

            foreach (var cartItem in cart.CartItems)
            {
                var product = await _productService.GetProductByIdAsync(cartItem.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= cartItem.Quantity;
                    _context.Products.Update(product);
                }
            }

            await _cartService.ClearCartAsync(userId, null);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Order placed successfully with ID: {OrderId}, payment transaction ID: {TransactionId}",
                order.OrderId,
                paymentResult.TransactionId
            );

            return new CreateOrderResponse { Order = order, PaymentResult = paymentResult };
        }

        public async Task<IEnumerable<Order>> GetOrderHistoryAsync(int userId)
        {
            _logger.LogInformation("Getting order history for user ID: {UserId}", userId);

            return await _context
                .Orders.Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderDetailsAsync(int orderId)
        {
            _logger.LogInformation("Getting order details for order ID: {OrderId}", orderId);

            return await _context
                .Orders.Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> UpdateOrderStatusAsync(int orderId, string status)
        {
            _logger.LogInformation(
                "Updating order status for order ID: {OrderId} to {Status}",
                orderId,
                status
            );

            if (!AvailableOrderStatuses.Contains(status))
            {
                throw new ArgumentException($"Invalid order status: {status}");
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return null;
            }

            if (!IsValidStatusTransition(order.Status, status))
            {
                throw new InvalidOperationException(
                    $"Cannot transition from {order.Status} to {status}"
                );
            }

            order.Status = status;
            order.OrderDate = DateTime.UtcNow; // Update timestamp

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Order status updated successfully for order ID: {OrderId}",
                orderId
            );
            return order;
        }

        public async Task<IEnumerable<string>> GetAvailableOrderStatusesAsync()
        {
            return await Task.FromResult(AvailableOrderStatuses);
        }

        private bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            var validTransitions = new Dictionary<string, List<string>>
            {
                {
                    "Pending",
                    new List<string> { "Processing", "Cancelled" }
                },
                {
                    "Processing",
                    new List<string> { "Shipped", "Cancelled" }
                },
                {
                    "Shipped",
                    new List<string> { "Delivered" }
                },
                { "Delivered", new List<string>() }, // No transitions from Delivered
                { "Cancelled", new List<string>() }, // No transitions from Cancelled
            };

            return validTransitions.ContainsKey(currentStatus)
                && validTransitions[currentStatus].Contains(newStatus);
        }
    }
}
