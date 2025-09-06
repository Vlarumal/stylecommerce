using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public interface IPaymentTokenizationService
    {
        Task<PaymentToken> CreatePaymentTokenAsync(
            int userId,
            string cardNumber,
            int expiryMonth,
            int expiryYear,
            string cardType
        );
        Task<PaymentToken?> GetPaymentTokenAsync(string token);
        Task<PaymentToken?> GetPaymentTokenByIdAsync(int id);
        Task<bool> DeletePaymentTokenAsync(int id);
        Task<IEnumerable<PaymentToken>> GetUserPaymentTokensAsync(int userId);
        Task<bool> ValidatePaymentTokenAsync(string token);
        Task<PaymentResult> ProcessPaymentAsync(string paymentToken, decimal amount);
        Task<PaymentResult> Process3DSecurePaymentAsync(
            string paymentToken,
            decimal amount,
            string returnUrl
        );
        Task CleanupExpiredTokensAsync();
    }
}
