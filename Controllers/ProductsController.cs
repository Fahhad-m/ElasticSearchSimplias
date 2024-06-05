
using Microsoft.AspNetCore.Mvc;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IElasticsearchService _elasticsearchService;
        public ProductsController(IProductService productService, IElasticsearchService elasticsearchService)
        {
            _productService = productService;
            _elasticsearchService = elasticsearchService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var products = await _productService.SearchProductsAsync(query);
          
            return Ok(products);
        }
        [HttpPost("CreateProducts")]
        public async Task<IActionResult> CreatProducts([FromBody] Models.Product product) 
        {
            var result = await _productService.CreateProductsAsync(product);
            await _elasticsearchService.IndexProductAsync(product);
            return Ok(result);
        }
        [HttpGet("read/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var products = await _productService.GetProductsAsync(id);
            return Ok(products);
        }
        [HttpPut("updateProduct")]
        public async Task<IActionResult> UpdateProduct([FromBody] Product product)
        {
           await _productService.UpdateProductAsync(product);
            await _elasticsearchService.UpdateProductAsync(product);
            return Ok(product);
        }
        [HttpPost("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
             await _productService.DeleteProductAsync(id);
            await _elasticsearchService.DeleteProductAsync(id);
            return Ok();
        }
        
              [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productService.GetAllProducts();
            return Ok(products);
        }
    }
}
