using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Service
{
    /// <summary>
    /// Handles all Elasticsearch CRUD and index-management operations for Products.
    /// </summary>
    public class ElasticsearchService : IElasticsearchService
    {
        private readonly IElasticClient _client;
        private readonly ILogger<ElasticsearchService> _logger;

        public ElasticsearchService(IElasticClient client, ILogger<ElasticsearchService> logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task EnsureIndexCreatedAsync()
        {
            try
            {
                var existsResponse = await _client.Indices.ExistsAsync("products");
                if (existsResponse.Exists)
                {
                    _logger.LogInformation("Elasticsearch index 'products' already exists.");
                    return;
                }

                var createResponse = await _client.Indices.CreateAsync("products", c => c
                    .Map<Product>(m => m
                        .Properties(p => p
                            .Number(n => n.Name(f => f.Id).Type(NumberType.Integer))
                            .Text(t => t.Name(f => f.Name).Analyzer("standard"))
                            .Text(t => t.Name(f => f.Description).Analyzer("standard"))
                            .Number(n => n.Name(f => f.Price).Type(NumberType.Double))
                            .Keyword(k => k.Name(f => f.Category))
                        )
                    )
                );

                if (!createResponse.IsValid)
                {
                    _logger.LogError("Failed to create Elasticsearch index: {Error}",
                        createResponse.ServerError?.Error?.Reason ?? createResponse.OriginalException?.Message);
                    throw new Exception("Failed to create 'products' index in Elasticsearch.");
                }

                _logger.LogInformation("Elasticsearch index 'products' created with mappings.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ensuring Elasticsearch index exists.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task IndexProductAsync(Product product)
        {
            try
            {
                var response = await _client.IndexAsync(product, i => i.Index("products").Id(product.Id));
                if (!response.IsValid)
                {
                    _logger.LogError("Failed to index product {Id} in Elasticsearch: {Error}",
                        product.Id, response.ServerError?.Error?.Reason ?? response.OriginalException?.Message);
                    throw new Exception($"Failed to index product {product.Id} in Elasticsearch.");
                }

                _logger.LogInformation("Product {Id} indexed in Elasticsearch.", product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Elasticsearch error while indexing product {Id}.", product.Id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task BulkIndexAsync(IEnumerable<Product> products)
        {
            try
            {
                var response = await _client.BulkAsync(b => b
                    .Index("products")
                    .IndexMany(products, (descriptor, product) => descriptor.Id(product.Id))
                );

                if (response.Errors)
                {
                    foreach (var item in response.ItemsWithErrors)
                    {
                        _logger.LogError("Failed to index product {Id}: {Error}", item.Id, item.Error?.Reason);
                    }
                    throw new Exception($"Bulk indexing completed with {response.ItemsWithErrors.Count()} errors.");
                }

                _logger.LogInformation("Bulk indexed {Count} products into Elasticsearch.", response.Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Elasticsearch error during bulk indexing.");
                throw;
            }
        }

        /// <inheritdoc />
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Elasticsearch error while updating product {Id}.", product.Id);
                throw;
            }
        }

        /// <inheritdoc />
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Elasticsearch error while deleting product {Id}.", id);
                throw;
            }
        }
    }
}
