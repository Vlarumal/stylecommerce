using System.ComponentModel.DataAnnotations;

namespace StyleCommerce.Api.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public int CategoryId { get; set; }

        [StringLength(50)]
        public string Brand { get; set; } = string.Empty;

        [StringLength(20)]
        public string Size { get; set; } = string.Empty;

        [StringLength(30)]
        public string Color { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Url]
        public string ImageUrl { get; set; } = string.Empty;

        [Url]
        public string? Model3DUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsVerified { get; set; } = false;

        [Range(0, 100)]
        public int VerificationScore { get; set; } = 0;

        [Range(0, 100)]
        public int EcoScore { get; set; } = 0;
    }
}
