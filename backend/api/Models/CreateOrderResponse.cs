namespace StyleCommerce.Api.Models
{
    public class CreateOrderResponse
    {
        public Order Order { get; set; } = null!;
        public PaymentResult PaymentResult { get; set; } = null!;
    }
}