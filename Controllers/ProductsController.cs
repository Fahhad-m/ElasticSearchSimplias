using Microsoft.AspNetCore.Mvc;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Controllers
{
    /// <summary>
    /// Manages CRUD operations for Products.
    /// Writes go to both SQL and Elasticsearch to keep data in sync.
    /// Reads come from SQL.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, IElasticsearchService elasticsearchService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        [HttpPost("CreateProducts")]
        public async Task<IActionResult> CreateProducts([FromBody] Product product)
        {
            try
            {
                if (product == null)
                    return BadRequest("Product cannot be null.");

                if (string.IsNullOrWhiteSpace(product.Name))
                    return BadRequest("Product name is required.");

                var createdProduct = await _productService.CreateProductsAsync(product);

                try
                {
                    await _elasticsearchService.IndexProductAsync(createdProduct);
                }
                catch (Exception esEx)
                {
                    _logger.LogError(esEx, "Product {Id} saved to SQL but failed to index in Elasticsearch. ES is out of sync.", createdProduct.Id);
                }

                return Ok(createdProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateProducts.");
                return StatusCode(500, "An error occurred while creating the product.");
            }
        }

        [HttpGet("read/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Product ID must be a positive integer.");

                var product = await _productService.GetProductsAsync(id);
                if (product == null)
                    return NotFound($"Product with Id {id} not found.");

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProduct for Id {Id}.", id);
                return StatusCode(500, "An error occurred while fetching the product.");
            }
        }

        [HttpPut("updateProduct")]
        public async Task<IActionResult> UpdateProduct([FromBody] Product product)
        {
            try
            {
                if (product == null)
                    return BadRequest("Product cannot be null.");

                if (product.Id <= 0)
                    return BadRequest("Product ID must be a positive integer.");

                await _productService.UpdateProductAsync(product);

                try
                {
                    await _elasticsearchService.UpdateProductAsync(product);
                }
                catch (Exception esEx)
                {
                    _logger.LogError(esEx, "Product {Id} updated in SQL but failed to update in Elasticsearch. ES is out of sync.", product.Id);
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateProduct for Id {Id}.", product?.Id);
                return StatusCode(500, "An error occurred while updating the product.");
            }
        }

        [HttpPost("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Product ID must be a positive integer.");

                await _productService.DeleteProductAsync(id);

                try
                {
                    await _elasticsearchService.DeleteProductAsync(id);
                }
                catch (Exception esEx)
                {
                    _logger.LogError(esEx, "Product {Id} deleted from SQL but failed to delete from Elasticsearch. ES is out of sync.", id);
                }

                return Ok($"Product with Id {id} deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteProduct for Id {Id}.", id);
                return StatusCode(500, "An error occurred while deleting the product.");
            }
        }

        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _productService.GetAllProducts();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllProducts.");
                return StatusCode(500, "An error occurred while fetching products.");
            }
        }
    }
}
