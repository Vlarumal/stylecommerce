using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StyleCommerce.Api.Models
{
    public class Cart
    {
        public int Id { get; set; }

        public int? UserId { get; set; } // Nullable for guest carts

        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual User? User { get; set; } // Nullable for guest carts

        [StringLength(100)]
        public string? SessionId { get; set; } // For guest carts

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property for cart items
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
