using Microsoft.AspNetCore.Mvc;
using SearchAPI.Interfaces;

namespace SearchAPI.Controllers
{
    /// <summary>
    /// Provides the /api/search endpoint that queries the Elasticsearch
    /// products index across Id, Name, and Description fields.
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

        /// <summary>
        /// Searches products in Elasticsearch by Id, Name, and Description.
        /// </summary>
        /// <param name="query">The search term.</param>
        /// <returns>Matching products from Elasticsearch.</returns>
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query cannot be empty.");

                var products = await _productService.SearchProductsAsync(query);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in /api/search.");
                return StatusCode(500, "An error occurred while searching products.");
            }
        }
    }
}