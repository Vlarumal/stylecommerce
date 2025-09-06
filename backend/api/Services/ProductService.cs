using Microsoft.EntityFrameworkCore;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            _logger.LogInformation("Getting all products");
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            _logger.LogInformation("Getting product by ID: {ProductId}", id);
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _logger.LogInformation("Creating new product: {ProductName}", product.Name);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
            return product;
        }

        public async Task<Product?> UpdateProductAsync(Product product)
        {
            _logger.LogInformation("Updating product with ID: {ProductId}", product.Id);

            var existingProduct = await _context.Products.FindAsync(product.Id);
            if (existingProduct == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for update", product.Id);
                return null;
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Brand = product.Brand;
            existingProduct.Size = product.Size;
            existingProduct.Color = product.Color;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.Model3DUrl = product.Model3DUrl;
            existingProduct.UpdatedAt = DateTime.UtcNow;
            existingProduct.IsVerified = product.IsVerified;
            existingProduct.VerificationScore = product.VerificationScore;
            existingProduct.EcoScore = product.EcoScore;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Product with ID {ProductId} updated successfully", product.Id);
            return existingProduct;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                return false;
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product with ID {ProductId} deleted successfully", id);
            return true;
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
        {
            _logger.LogInformation("Searching products with query: {SearchQuery}", query);

            return await _context
                .Products.Where(p => p.Name.Contains(query) || p.Description.Contains(query))
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            _logger.LogInformation("Getting products by category ID: {CategoryId}", categoryId);

            return await _context.Products.Where(p => p.CategoryId == categoryId).ToListAsync();
        }
    }
}
