using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public interface IOrderService
    {
        Task<CreateOrderResponse> PlaceOrderAsync(int userId, string paymentToken);
        Task<IEnumerable<Order>> GetOrderHistoryAsync(int userId);
        Task<Order?> GetOrderDetailsAsync(int orderId);
        Task<Order?> UpdateOrderStatusAsync(int orderId, string status);
        Task<IEnumerable<string>> GetAvailableOrderStatusesAsync();
    }
}
