using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;

namespace StyleCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentTokenizationService _paymentTokenizationService;
        private readonly IUserService _userService;

        public PaymentController(
            IPaymentTokenizationService paymentTokenizationService,
            IUserService userService
        )
        {
            _paymentTokenizationService = paymentTokenizationService;
            _userService = userService;
        }

        [HttpPost("tokenize")]
        public async Task<ActionResult<PaymentToken>> CreatePaymentToken(
            [FromBody] CreatePaymentTokenRequest request
        )
        {
            var userExists = await _userService.UserExistsAsync(request.UserId);
            if (!userExists)
            {
                return BadRequest("User does not exist");
            }

            try
            {
                var paymentToken = await _paymentTokenizationService.CreatePaymentTokenAsync(
                    request.UserId,
                    request.CardNumber,
                    request.ExpiryMonth,
                    request.ExpiryYear,
                    request.CardType
                );

                return CreatedAtAction(
                    nameof(GetPaymentToken),
                    new { id = paymentToken.Id },
                    paymentToken
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("token/{token}")]
        public async Task<ActionResult<PaymentToken>> GetPaymentToken(string token)
        {
            var paymentToken = await _paymentTokenizationService.GetPaymentTokenAsync(token);
            if (paymentToken == null)
            {
                return NotFound();
            }
            return Ok(paymentToken);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<PaymentToken>>> GetUserPaymentTokens(int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId)
            {
                return Forbid();
            }

            var paymentTokens = await _paymentTokenizationService.GetUserPaymentTokensAsync(userId);
            return Ok(paymentTokens);
        }

        [HttpDelete("token/{id}")]
        public async Task<ActionResult> DeletePaymentToken(int id)
        {
            var result = await _paymentTokenizationService.DeletePaymentTokenAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpPost("validate")]
        public async Task<ActionResult<ValidatePaymentTokenResponse>> ValidatePaymentToken(
            [FromBody] ValidatePaymentTokenRequest request
        )
        {
            var isValid = await _paymentTokenizationService.ValidatePaymentTokenAsync(
                request.Token
            );
            return Ok(new ValidatePaymentTokenResponse { IsValid = isValid });
        }

        private int GetCurrentUserId()
        {
            // In a real implementation, this would extract the user ID from the JWT token
            // For now, we'll return a default value
            return 1;
        }
    }

    public class CreatePaymentTokenRequest
    {
        public int UserId { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string CardType { get; set; } = string.Empty;
    }

    public class ValidatePaymentTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class ValidatePaymentTokenResponse
    {
        public bool IsValid { get; set; }
    }
}
