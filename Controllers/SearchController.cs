using Microsoft.AspNetCore.Mvc;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Controllers
{
    /// <summary>
    /// GET /api/search?query=...
    /// Full-text search via Elasticsearch across Id, Name, and Description fields.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(IProductService productService, ILogger<SearchController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(ApiResponse<IEnumerable<Product>>.Fail("Query parameter is required."));

            try
            {
                var products = await _productService.SearchProductsAsync(query);
                return Ok(ApiResponse<IEnumerable<Product>>.Ok(products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed for query '{Query}'.", query);
                return StatusCode(500, ApiResponse<IEnumerable<Product>>.Fail("Search failed."));
            }
        }
    }
}