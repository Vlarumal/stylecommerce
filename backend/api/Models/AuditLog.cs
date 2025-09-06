using System.ComponentModel.DataAnnotations;

namespace StyleCommerce.Api.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [StringLength(50)]
        public string? UserId { get; set; }

        [StringLength(45)] // IPv4/IPv6 max length
        public string? IpAddress { get; set; }

        [StringLength(255)]
        public string? UserAgent { get; set; }

        [StringLength(1000)]
        public string? AdditionalData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // For compliance tracking
        [StringLength(100)]
        public string? SessionId { get; set; }
    }
}
