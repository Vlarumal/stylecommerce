using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public class PaymentTokenizationService : IPaymentTokenizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentTokenizationService> _logger;
        private readonly IAuditLoggingService _auditLoggingService;
        private readonly StripeSettings _stripeSettings;

        public PaymentTokenizationService(
            ApplicationDbContext context,
            ILogger<PaymentTokenizationService> logger,
            IAuditLoggingService auditLoggingService,
            IOptions<StripeSettings> stripeSettings
        )
        {
            _context = context;
            _logger = logger;
            _auditLoggingService = auditLoggingService;
            _stripeSettings = stripeSettings.Value;
        }

        public async Task<PaymentToken> CreatePaymentTokenAsync(
            int userId,
            string cardNumber,
            int expiryMonth,
            int expiryYear,
            string cardType
        )
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 16)
            {
                throw new ArgumentException("Invalid card number");
            }

            if (expiryMonth < 1 || expiryMonth > 12)
            {
                throw new ArgumentException("Invalid expiry month");
            }

            if (expiryYear < DateTime.UtcNow.Year)
            {
                throw new ArgumentException("Card has expired");
            }

            // Create a secure token (in a real implementation, this would integrate with a payment provider)
            var token = GenerateSecureToken();

            // Store only the last 4 digits for reference
            var lastFourDigits = cardNumber.Substring(cardNumber.Length - 4);

            var paymentToken = new PaymentToken
            {
                Token = token,
                UserId = userId,
                LastFourDigits = lastFourDigits,
                CardType = cardType,
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                IsActive = true,
            };

            _context.PaymentTokens.Add(paymentToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Payment token created for user {UserId} with token ID {TokenId}",
                userId,
                paymentToken.Id
            );

            // Log audit action for PCI-DSS compliance
            await _auditLoggingService.LogActionAsync(
                "CREATE_PAYMENT_TOKEN",
                "PaymentToken",
                paymentToken.Id,
                userId.ToString(),
                $"Payment token created for user {userId}, card type {cardType}, expires {expiryMonth}/{expiryYear}"
            );

            return paymentToken;
        }

        public async Task<PaymentToken?> GetPaymentTokenAsync(string token)
        {
            return await _context
                .PaymentTokens.Where(pt =>
                    pt.Token == token && pt.IsActive && pt.ExpiresAt > DateTime.UtcNow
                )
                .FirstOrDefaultAsync();
        }

        public async Task<PaymentToken?> GetPaymentTokenByIdAsync(int id)
        {
            return await _context
                .PaymentTokens.Where(pt =>
                    pt.Id == id && pt.IsActive && pt.ExpiresAt > DateTime.UtcNow
                )
                .FirstOrDefaultAsync();
        }

        public async Task<bool> DeletePaymentTokenAsync(int id)
        {
            var token = await _context.PaymentTokens.FindAsync(id);
            if (token == null)
            {
                return false;
            }

            // For PCI-DSS compliance, we mark as inactive rather than hard delete
            token.IsActive = false;
            token.UpdatedAt = DateTime.UtcNow;

            _context.PaymentTokens.Update(token);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment token deleted with ID: {TokenId}", id);

            // Log audit action for PCI-DSS compliance
            await _auditLoggingService.LogActionAsync(
                "DELETE_PAYMENT_TOKEN",
                "PaymentToken",
                token.Id,
                token.UserId.ToString(),
                $"Payment token deleted for user {token.UserId}"
            );

            return true;
        }

        public async Task<IEnumerable<PaymentToken>> GetUserPaymentTokensAsync(int userId)
        {
            return await _context
                .PaymentTokens.Where(pt =>
                    pt.UserId == userId && pt.IsActive && pt.ExpiresAt > DateTime.UtcNow
                )
                .ToListAsync();
        }

        public async Task<bool> ValidatePaymentTokenAsync(string token)
        {
            var paymentToken = await GetPaymentTokenAsync(token);
            return paymentToken != null;
        }

        public async Task CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context
                .PaymentTokens.Where(pt => pt.ExpiresAt < DateTime.UtcNow || !pt.IsActive)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                foreach (var token in expiredTokens)
                {
                    token.IsActive = false;
                }

                _context.PaymentTokens.UpdateRange(expiredTokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Cleaned up {Count} expired payment tokens",
                    expiredTokens.Count
                );

                // Log audit action for PCI-DSS compliance
                await _auditLoggingService.LogActionAsync(
                    "CLEANUP_EXPIRED_TOKENS",
                    "PaymentToken",
                    null,
                    null,
                    $"Cleaned up {expiredTokens.Count} expired payment tokens"
                );
            }
        }

        public async Task<PaymentResult> ProcessPaymentAsync(string paymentToken, decimal amount)
        {
            try
            {
                // Create a payment intent with Stripe
                var paymentIntentService = new PaymentIntentService();
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Convert to cents
                    Currency = "usd",
                    PaymentMethod = paymentToken,
                    ConfirmationMethod = "manual",
                    Confirm = true,
                    ReturnUrl = "https://localhost:5173/checkout", // Adjust as needed
                };

                var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

                if (paymentIntent.Status == "succeeded")
                {
                    _logger.LogInformation(
                        "Payment processed successfully with Stripe, amount: {Amount}, transaction ID: {TransactionId}",
                        amount,
                        paymentIntent.Id
                    );

                    // Log audit action for PCI-DSS compliance
                    await _auditLoggingService.LogActionAsync(
                        "PROCESS_STRIPE_PAYMENT_SUCCESS",
                        "Payment",
                        null,
                        null,
                        $"Payment processed successfully with Stripe, amount: {amount}, transaction ID: {paymentIntent.Id}"
                    );

                    return new PaymentResult
                    {
                        IsSuccess = true,
                        TransactionId = paymentIntent.Id,
                        Message = "Payment processed successfully",
                        Amount = amount,
                        PaymentMethod = "Card",
                    };
                }
                else
                {
                    _logger.LogWarning(
                        "Payment processing failed with Stripe, amount: {Amount}, transaction ID: {TransactionId}, status: {Status}",
                        amount,
                        paymentIntent.Id,
                        paymentIntent.Status
                    );

                    // Log audit action for PCI-DSS compliance
                    await _auditLoggingService.LogActionAsync(
                        "PROCESS_STRIPE_PAYMENT_FAILURE",
                        "Payment",
                        null,
                        null,
                        $"Payment processing failed with Stripe, amount: {amount}, transaction ID: {paymentIntent.Id}, status: {paymentIntent.Status}"
                    );

                    return new PaymentResult
                    {
                        IsSuccess = false,
                        TransactionId = paymentIntent.Id,
                        Message = $"Payment failed with status: {paymentIntent.Status}",
                        Amount = amount,
                        PaymentMethod = "Card",
                    };
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Stripe error occurred while processing payment, amount: {Amount}",
                    amount
                );

                // Log audit action for PCI-DSS compliance
                await _auditLoggingService.LogActionAsync(
                    "PROCESS_STRIPE_PAYMENT_ERROR",
                    "Payment",
                    null,
                    null,
                    $"Stripe error occurred while processing payment, amount: {amount}, error: {ex.Message}"
                );

                return new PaymentResult
                {
                    IsSuccess = false,
                    Message = $"Payment failed: {ex.Message}",
                    Amount = amount,
                    PaymentMethod = "Card",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error occurred while processing payment, amount: {Amount}",
                    amount
                );

                // Log audit action for PCI-DSS compliance
                await _auditLoggingService.LogActionAsync(
                    "PROCESS_STRIPE_PAYMENT_EXCEPTION",
                    "Payment",
                    null,
                    null,
                    $"Unexpected error occurred while processing payment, amount: {amount}, error: {ex.Message}"
                );

                return new PaymentResult
                {
                    IsSuccess = false,
                    Message =
                        "An unexpected error occurred while processing your payment. Please try again.",
                    Amount = amount,
                    PaymentMethod = "Card",
                };
            }
        }

        public async Task<PaymentResult> Process3DSecurePaymentAsync(
            string paymentToken,
            decimal amount,
            string returnUrl
        )
        {
            try
            {
                // Create a payment intent with Stripe that requires 3D Secure authentication
                var paymentIntentService = new PaymentIntentService();
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Convert to cents
                    Currency = "usd",
                    PaymentMethod = paymentToken,
                    ConfirmationMethod = "manual",
                    Confirm = true,
                    ReturnUrl = returnUrl,
                    CaptureMethod = "manual", // For 3D Secure, we might want to capture manually
                };

                var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

                // Check if the payment requires 3D Secure authentication
                if (
                    paymentIntent.Status == "requires_action"
                    || paymentIntent.Status == "requires_source_action"
                )
                {
                    _logger.LogInformation(
                        "3D Secure authentication required for payment, amount: {Amount}, transaction ID: {TransactionId}",
                        amount,
                        paymentIntent.Id
                    );

                    // Log audit action for PCI-DSS compliance
                    await _auditLoggingService.LogActionAsync(
                        "REQUIRE_STRIPE_3D_SECURE",
                        "Payment",
                        null,
                        null,
                        $"3D Secure authentication required for payment, amount: {amount}, transaction ID: {paymentIntent.Id}"
                    );

                    return new PaymentResult
                    {
                        IsSuccess = false,
                        TransactionId = paymentIntent.Id,
                        Message = "3D Secure authentication required",
                        Amount = amount,
                        PaymentMethod = "Card",
                        Requires3DSecure = true,
                        RedirectUrl = paymentIntent.NextAction?.RedirectToUrl?.Url ?? returnUrl,
                    };
                }
                else if (paymentIntent.Status == "succeeded")
                {
                    _logger.LogInformation(
                        "3D Secure payment processed successfully, amount: {Amount}, transaction ID: {TransactionId}",
                        amount,
                        paymentIntent.Id
                    );

                    // Log audit action for PCI-DSS compliance
                    await _auditLoggingService.LogActionAsync(
                        "PROCESS_STRIPE_3D_SECURE_PAYMENT_SUCCESS",
                        "Payment",
                        null,
                        null,
                        $"3D Secure payment processed successfully, amount: {amount}, transaction ID: {paymentIntent.Id}"
                    );

                    return new PaymentResult
                    {
                        IsSuccess = true,
                        TransactionId = paymentIntent.Id,
                        Message = "Payment processed successfully",
                        Amount = amount,
                        PaymentMethod = "Card",
                    };
                }
                else
                {
                    _logger.LogWarning(
                        "3D Secure payment processing failed, amount: {Amount}, transaction ID: {TransactionId}, status: {Status}",
                        amount,
                        paymentIntent.Id,
                        paymentIntent.Status
                    );

                    // Log audit action for PCI-DSS compliance
                    await _auditLoggingService.LogActionAsync(
                        "PROCESS_STRIPE_3D_SECURE_PAYMENT_FAILURE",
                        "Payment",
                        null,
                        null,
                        $"3D Secure payment processing failed, amount: {amount}, transaction ID: {paymentIntent.Id}, status: {paymentIntent.Status}"
                    );

                    return new PaymentResult
                    {
                        IsSuccess = false,
                        TransactionId = paymentIntent.Id,
                        Message = $"Payment failed with status: {paymentIntent.Status}",
                        Amount = amount,
                        PaymentMethod = "Card",
                    };
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Stripe error occurred while processing 3D Secure payment, amount: {Amount}",
                    amount
                );

                // Log audit action for PCI-DSS compliance
                await _auditLoggingService.LogActionAsync(
                    "PROCESS_STRIPE_3D_SECURE_PAYMENT_ERROR",
                    "Payment",
                    null,
                    null,
                    $"Stripe error occurred while processing 3D Secure payment, amount: {amount}, error: {ex.Message}"
                );

                return new PaymentResult
                {
                    IsSuccess = false,
                    Message = $"Payment failed: {ex.Message}",
                    Amount = amount,
                    PaymentMethod = "Card",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error occurred while processing 3D Secure payment, amount: {Amount}",
                    amount
                );

                // Log audit action for PCI-DSS compliance
                await _auditLoggingService.LogActionAsync(
                    "PROCESS_STRIPE_3D_SECURE_PAYMENT_EXCEPTION",
                    "Payment",
                    null,
                    null,
                    $"Unexpected error occurred while processing 3D Secure payment, amount: {amount}, error: {ex.Message}"
                );

                return new PaymentResult
                {
                    IsSuccess = false,
                    Message =
                        "An unexpected error occurred while processing your payment. Please try again.",
                    Amount = amount,
                    PaymentMethod = "Card",
                };
            }
        }

        private string GenerateTransactionId()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[16];
            rng.GetBytes(bytes);
            return "txn_"
                + Convert
                    .ToBase64String(bytes)
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "")
                    .Substring(0, 20);
        }

        private string GenerateSecureToken()
        {
            // In a real implementation, this would use a cryptographically secure random generator
            // and integrate with a payment provider's tokenization service
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert
                .ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 32);
        }
    }
}
