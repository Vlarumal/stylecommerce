using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StyleCommerce.Api.Data;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;
using Xunit;

namespace StyleCommerce.Api.Tests
{
    public class ProductServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductService _productService;
        private readonly Mock<ILogger<ProductService>> _loggerMock;

        public ProductServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique database name for each test
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ProductService>>();
            _productService = new ProductService(_context, _loggerMock.Object);

            _context.Products.AddRange(
                new Product
                {
                    Id = 1,
                    Name = "Product 1",
                    Price = 10.99m,
                    CategoryId = 1,
                    Model3DUrl = "https://example.com/model1.glb",
                },
                new Product
                {
                    Id = 2,
                    Name = "Product 2",
                    Price = 15.99m,
                    CategoryId = 2,
                    Model3DUrl = "https://example.com/model2.glb",
                },
                new Product
                {
                    Id = 3,
                    Name = "Product 3",
                    Price = 20.99m,
                    CategoryId = 1,
                    Model3DUrl = "https://example.com/model3.glb",
                }
            );
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task GetAllProductsAsync_ReturnsAllProducts()
        {
            var result = await _productService.GetAllProductsAsync();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetProductByIdAsync_ExistingId_ReturnsProduct()
        {
            var result = await _productService.GetProductByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Product 1", result.Name);
        }

        [Fact]
        public async Task GetProductByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _productService.GetProductByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateProductAsync_ValidProduct_ReturnsCreatedProduct()
        {
            var product = new Product
            {
                Name = "New Product",
                Price = 25.99m,
                CategoryId = 3,
                Model3DUrl = "https://example.com/new-model.glb",
            };

            var result = await _productService.CreateProductAsync(product);

            Assert.Equal("New Product", result.Name);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task UpdateProductAsync_ExistingProduct_ReturnsUpdatedProduct()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Updated Product",
                Price = 30.99m,
                CategoryId = 3,
                Model3DUrl = "https://example.com/updated-model.glb",
            };

            var result = await _productService.UpdateProductAsync(product);

            Assert.NotNull(result);
            Assert.Equal("Updated Product", result.Name);
            Assert.Equal(30.99m, result.Price);
        }

        [Fact]
        public async Task UpdateProductAsync_NonExistingProduct_ReturnsNull()
        {
            var product = new Product
            {
                Id = 999,
                Name = "Non-existing Product",
                Price = 30.99m,
                CategoryId = 3,
                Model3DUrl = "https://example.com/non-existing-model.glb",
            };

            var result = await _productService.UpdateProductAsync(product);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteProductAsync_ExistingId_ReturnsTrue()
        {
            var result = await _productService.DeleteProductAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteProductAsync_NonExistingId_ReturnsFalse()
        {
            var result = await _productService.DeleteProductAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task SearchProductsAsync_WithMatchingQuery_ReturnsMatchingProducts()
        {
            var result = await _productService.SearchProductsAsync("Product 1");

            Assert.Single(result);
            Assert.Equal("Product 1", result.First().Name);
        }

        [Fact]
        public async Task GetProductsByCategoryAsync_WithExistingCategory_ReturnsProductsInCategory()
        {
            var result = await _productService.GetProductsByCategoryAsync(1);

            Assert.Equal(2, result.Count());
            Assert.All(result, p => Assert.Equal(1, p.CategoryId));
        }
    }
}
