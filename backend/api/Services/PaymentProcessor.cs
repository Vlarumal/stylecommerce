using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public class PaymentProcessor
    {
        private readonly IPaymentTokenizationService _paymentTokenizationService;
        private readonly ILogger<PaymentProcessor> _logger;

        public PaymentProcessor(
            IPaymentTokenizationService paymentTokenizationService,
            ILogger<PaymentProcessor> logger
        )
        {
            _paymentTokenizationService = paymentTokenizationService;
            _logger = logger;
        }

        public virtual async Task<PaymentResult> ProcessPaymentWithRetryAsync(
            string paymentToken,
            decimal amount,
            int maxRetries = 3
        )
        {
            PaymentResult? result = null;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;
                _logger.LogInformation(
                    "Processing payment attempt {Attempt} of {MaxRetries} for amount: {Amount}",
                    attempt,
                    maxRetries,
                    amount
                );

                try
                {
                    result = await _paymentTokenizationService.ProcessPaymentAsync(
                        paymentToken,
                        amount
                    );

                    if (result.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Payment processed successfully on attempt {Attempt}, transaction ID: {TransactionId}",
                            attempt,
                            result.TransactionId
                        );
                        return result;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Payment attempt {Attempt} failed: {Message}",
                            attempt,
                            result.Message
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Payment attempt {Attempt} failed with exception",
                        attempt
                    );
                }

                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                    _logger.LogInformation(
                        "Waiting {Delay} seconds before retrying payment",
                        delay.TotalSeconds
                    );
                    await Task.Delay(delay);
                }
            }

            _logger.LogError(
                "All {MaxRetries} payment attempts failed for amount: {Amount}",
                maxRetries,
                amount
            );

            return new PaymentResult
            {
                IsSuccess = false,
                Message = "Payment failed after multiple attempts. Please try again later.",
                Amount = amount,
                PaymentMethod = "Unknown",
            };
        }

        public virtual string GeneratePaymentReceipt(PaymentResult paymentResult, Order order)
        {
            var receipt =
                $@"
Payment Receipt
===============
Transaction ID: {paymentResult.TransactionId}
Date: {paymentResult.ProcessedAt:yyyy-MM-dd HH:mm:ss}
Order ID: {order.OrderId}
Amount: ${paymentResult.Amount:F2}
Payment Method: {paymentResult.PaymentMethod}
Status: {(paymentResult.IsSuccess ? "SUCCESS" : "FAILED")}
Message: {paymentResult.Message}

Thank you for your purchase!
";
            return receipt;
        }

        public virtual async Task<PaymentResult> ProcessPaymentByMethodAsync(
            string paymentToken,
            decimal amount,
            string paymentMethod
        )
        {
            // In a real implementation, this would handle different payment methods
            // For now, we'll just process the payment normally
            _logger.LogInformation(
                "Processing payment for method: {PaymentMethod}, amount: {Amount}",
                paymentMethod,
                amount
            );

            return await _paymentTokenizationService.ProcessPaymentAsync(paymentToken, amount);
        }
    }
}
