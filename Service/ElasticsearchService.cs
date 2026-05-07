using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Service
{
    public class ElasticsearchService : IElasticsearchService
    {
        private readonly IElasticClient _client;
        private readonly ILogger<ElasticsearchService> _logger;

        public ElasticsearchService(IElasticClient client, ILogger<ElasticsearchService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task IndexProductAsync(Product product)
        {
            try
            {
                var response = await _client.IndexDocumentAsync(product);
                if (!response.IsValid)
                {
                    _logger.LogError("Failed to index product {Id} in Elasticsearch: {Error}",
                        product.Id, response.ServerError?.Error?.Reason ?? response.OriginalException?.Message);
                    throw new Exception($"Failed to index product {product.Id} in Elasticsearch.");
                }

                _logger.LogInformation("Product {Id} indexed in Elasticsearch.", product.Id);
            }
            catch (Exception ex) when (ex is not Exception { Message: var m } || !m.StartsWith("Failed to index"))
            {
                _logger.LogError(ex, "Elasticsearch error while indexing product {Id}.", product.Id);
                throw;
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            try
            {
                var response = await _client.UpdateAsync<Product>(product.Id, u => u
                    .Doc(product)
                    .Index("products"));
                if (!response.IsValid)
                {
                    _logger.LogError("Failed to update product {Id} in Elasticsearch: {Error}",
                        product.Id, response.ServerError?.Error?.Reason ?? response.OriginalException?.Message);
                    throw new Exception($"Failed to update product {product.Id} in Elasticsearch.");
                }

                _logger.LogInformation("Product {Id} updated in Elasticsearch.", product.Id);
            }
            catch (Exception ex) when (ex is not Exception { Message: var m } || !m.StartsWith("Failed to update"))
            {
                _logger.LogError(ex, "Elasticsearch error while updating product {Id}.", product.Id);
                throw;
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            try
            {
                var response = await _client.DeleteAsync<Product>(id, d => d.Index("products"));
                if (!response.IsValid)
                {
                    _logger.LogError("Failed to delete product {Id} from Elasticsearch: {Error}",
                        id, response.ServerError?.Error?.Reason ?? response.OriginalException?.Message);
                    throw new Exception($"Failed to delete product {id} from Elasticsearch.");
                }

                _logger.LogInformation("Product {Id} deleted from Elasticsearch.", id);
            }
            catch (Exception ex) when (ex is not Exception { Message: var m } || !m.StartsWith("Failed to delete"))
            {
                _logger.LogError(ex, "Elasticsearch error while deleting product {Id}.", id);
                throw;
            }
        }
    }
}
