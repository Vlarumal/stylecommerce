using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.DTOs
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal PriceSnapshot { get; set; }
        public DateTime AddedDate { get; set; }
        public ProductDto Product { get; set; } = null!;
    }

    public class CartDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? SessionId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Model3DUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsVerified { get; set; }
        public int VerificationScore { get; set; }
        public int EcoScore { get; set; }
    }

    public static class CartMapper
    {
        public static CartDto ToDto(this Cart cart)
        {
            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                SessionId = cart.SessionId,
                CreatedDate = cart.CreatedDate,
                CartItems = cart.CartItems.Select(ci => ci.ToDto()).ToList(),
            };
        }

        public static CartItemDto ToDto(this CartItem cartItem)
        {
            return new CartItemDto
            {
                Id = cartItem.Id,
                CartId = cartItem.CartId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                PriceSnapshot = cartItem.PriceSnapshot,
                AddedDate = cartItem.AddedDate,
                Product = cartItem.Product?.ToDto() ?? new ProductDto(),
            };
        }

        public static ProductDto ToDto(this Product product)
        {
            if (product == null)
                return new ProductDto();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                Brand = product.Brand,
                Size = product.Size,
                Color = product.Color,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Model3DUrl = product.Model3DUrl,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                IsVerified = product.IsVerified,
                VerificationScore = product.VerificationScore,
                EcoScore = product.EcoScore,
            };
        }
    }
}
