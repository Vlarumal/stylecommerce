using System.ComponentModel.DataAnnotations;

namespace StyleCommerce.Api.Models
{
    public class PaymentToken
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

        // Store only the last 4 digits for reference
        [StringLength(4)]
        public string? LastFourDigits { get; set; }

        // Card type (Visa, Mastercard, etc.)
        [StringLength(20)]
        public string? CardType { get; set; }

        public int? ExpiryMonth { get; set; }
        public int? ExpiryYear { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // For security, tokens expire after a certain period
        public DateTime ExpiresAt { get; set; }

        // For audit logging
        public bool IsActive { get; set; } = true;
    }
}
