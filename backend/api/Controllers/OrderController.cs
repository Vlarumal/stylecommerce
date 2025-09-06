using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;

namespace StyleCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<ActionResult<CreateOrderResponse>> CreateOrder(
            [FromBody] CreateOrderRequest request
        )
        {
            _logger.LogInformation("Creating order for current user");

            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user ID");
            }

            if (string.IsNullOrEmpty(request.PaymentToken))
            {
                return BadRequest("Payment token is required");
            }

            if (request.PaymentToken.Length < 10 || request.PaymentToken.Length > 100)
            {
                return BadRequest("Invalid payment token format");
            }

            try
            {
                if (!int.TryParse(userId, out int userIdInt))
                {
                    return BadRequest("Invalid user ID format");
                }
                var response = await _orderService.PlaceOrderAsync(userIdInt, request.PaymentToken);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Failed to create order: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, "An error occurred while creating the order");
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrderHistory()
        {
            _logger.LogInformation("Getting order history for current user");

            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user ID");
            }

            if (!int.TryParse(userId, out int userIdInt))
            {
                return BadRequest("Invalid user ID format");
            }
            var orders = await _orderService.GetOrderHistoryAsync(userIdInt);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            _logger.LogInformation("Getting order details for order ID: {OrderId}", id);

            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid user ID");
            }

            if (!int.TryParse(userId, out int userIdInt))
            {
                return BadRequest("Invalid user ID format");
            }

            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            if (order.UserId != userIdInt)
            {
                return Forbid("You don't have permission to access this order");
            }

            return Ok(order);
        }

        [HttpPut("{id}/status")]
        [Authorize]
        public async Task<ActionResult<Order>> UpdateOrderStatus(
            int id,
            [FromBody] UpdateOrderStatusRequest request
        )
        {
            _logger.LogInformation(
                "Updating order status for order ID: {OrderId} to {Status}",
                id,
                request.Status
            );

            try
            {
                var order = await _orderService.UpdateOrderStatusAsync(id, request.Status);
                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return StatusCode(500, "An error occurred while updating the order status");
            }
        }

        [HttpGet("statuses")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableOrderStatuses()
        {
            _logger.LogInformation("Getting available order statuses");

            var statuses = await _orderService.GetAvailableOrderStatusesAsync();
            return Ok(statuses);
        }

        private string GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            return userIdClaim?.Value ?? string.Empty;
        }
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CreateOrderRequest
    {
        public string PaymentToken { get; set; } = string.Empty;
    }
}
