using Microsoft.AspNetCore.Mvc;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Controllers
{
    /// <summary>
    /// RESTful Product CRUD API.
    /// The controller is thin — it validates input, delegates to ProductService,
    /// and maps the result to an HTTP response with the ApiResponse envelope.
    /// All ES sync is handled inside the service layer (outbox pattern).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>POST api/products — Create a new product.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            if (product == null)
                return BadRequest(ApiResponse<Product>.Fail("Request body is required."));
            if (string.IsNullOrWhiteSpace(product.Name))
                return BadRequest(ApiResponse<Product>.Fail("Product name is required."));

            try
            {
                var created = await _productService.CreateProductAsync(product);
                return CreatedAtAction(nameof(GetById), new { id = created.Id },
                    ApiResponse<Product>.Ok(created, "Product created."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product.");
                return StatusCode(500, ApiResponse<Product>.Fail("Failed to create product."));
            }
        }

        /// <summary>GET api/products/{id} — Get a single product by ID.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse<Product>.Fail("ID must be a positive integer."));

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                    return NotFound(ApiResponse<Product>.Fail($"Product {id} not found."));

                return Ok(ApiResponse<Product>.Ok(product));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product {Id}.", id);
                return StatusCode(500, ApiResponse<Product>.Fail("Failed to fetch product."));
            }
        }

        /// <summary>GET api/products — Get all products.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(ApiResponse<List<Product>>.Ok(products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all products.");
                return StatusCode(500, ApiResponse<List<Product>>.Fail("Failed to fetch products."));
            }
        }

        /// <summary>PUT api/products/{id} — Update an existing product.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product product)
        {
            if (product == null)
                return BadRequest(ApiResponse<Product>.Fail("Request body is required."));
            if (id <= 0 || product.Id != id)
                return BadRequest(ApiResponse<Product>.Fail("Route ID must match product ID."));

            try
            {
                var updated = await _productService.UpdateProductAsync(product);
                if (!updated)
                    return NotFound(ApiResponse<Product>.Fail($"Product {id} not found."));

                return Ok(ApiResponse<Product>.Ok(product, "Product updated."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {Id}.", id);
                return StatusCode(500, ApiResponse<Product>.Fail("Failed to update product."));
            }
        }

        /// <summary>DELETE api/products/{id} — Delete a product.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse<string>.Fail("ID must be a positive integer."));

            try
            {
                var deleted = await _productService.DeleteProductAsync(id);
                if (!deleted)
                    return NotFound(ApiResponse<string>.Fail($"Product {id} not found."));

                return Ok(ApiResponse<string>.Ok($"Product {id} deleted.", "Product deleted."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {Id}.", id);
                return StatusCode(500, ApiResponse<string>.Fail("Failed to delete product."));
            }
        }

        /// <summary>POST api/products/bulk-index — Reads all products from SQL and bulk-indexes into ES.</summary>
        [HttpPost("bulk-index")]
        public async Task<IActionResult> BulkIndex()
        {
            try
            {
                var result = await _productService.BulkIndexAllProductsAsync();
                var message = $"Bulk index: {result.Succeeded} succeeded, {result.Failed} failed out of {result.TotalRequested}.";
                return Ok(ApiResponse<BulkIndexResult>.Ok(result, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk indexing.");
                return StatusCode(500, ApiResponse<BulkIndexResult>.Fail("Bulk indexing failed."));
            }
        }
    }
}
