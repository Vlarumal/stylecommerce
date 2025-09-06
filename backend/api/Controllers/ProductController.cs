using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleCommerce.Api.Models;
using StyleCommerce.Api.Services;

namespace StyleCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            _logger.LogInformation("Getting all products");
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            _logger.LogInformation("Getting product with ID: {ProductId}", id);
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", id);
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            _logger.LogInformation("Creating new product: {ProductName}", product.Name);
            // Validation is automatically handled by FluentValidation
            var createdProduct = await _productService.CreateProductAsync(product);
            return CreatedAtAction(
                nameof(GetProduct),
                new { id = createdProduct.Id },
                createdProduct
            );
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            _logger.LogInformation("Updating product with ID: {ProductId}", id);
            if (id != product.Id)
            {
                _logger.LogWarning(
                    "Product ID mismatch: {ProductId} != {Product.Id}",
                    id,
                    product.Id
                );
                return BadRequest();
            }

            // Validation is automatically handled by FluentValidation
            var updatedProduct = await _productService.UpdateProductAsync(product);
            if (updatedProduct == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for update", id);
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);
            var result = await _productService.DeleteProductAsync(id);
            if (!result)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts(
            [FromQuery] string query
        )
        {
            _logger.LogInformation("Searching products with query: {SearchQuery}", query);
            var products = await _productService.SearchProductsAsync(query);
            return Ok(products);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            _logger.LogInformation("Getting products by category ID: {CategoryId}", categoryId);
            var products = await _productService.GetProductsByCategoryAsync(categoryId);
            return Ok(products);
        }

        [HttpGet("{id}/model/3d")]
        [AllowAnonymous]
        public async Task<IActionResult> Get3DModel(int id)
        {
            _logger.LogInformation("Getting 3D model for product ID: {ProductId}", id);

            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(product.Model3DUrl))
            {
                return NotFound("3D model not available for this product");
            }

            // In a real implementation, you would serve the actual GLB file from storage
            // For this demo, we'll redirect to the model URL
            return Redirect(product.Model3DUrl);
        }

        [HttpGet("{id}/image/ar-fallback")]
        [AllowAnonymous]
        public async Task<IActionResult> GetARFallbackImage(int id)
        {
            _logger.LogInformation("Getting AR fallback image for product ID: {ProductId}", id);

            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // In a real implementation, this would serve the actual fallback image
            // For now, we'll redirect to the main product image
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                return Redirect(product.ImageUrl);
            }

            // Return a default fallback image
            return Ok(
                new
                {
                    productId = id,
                    message = "AR fallback image - in a real implementation, this would serve the actual fallback image",
                }
            );
        }
    }
}
