using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Service
{
    /// <summary>
    /// All Elasticsearch operations. Every write uses product.Id as the ES document Id,
    /// making all operations idempotent (safe to retry without creating duplicates).
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

        public async Task EnsureIndexCreatedAsync()
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
                var error = createResponse.ServerError?.Error?.Reason
                    ?? createResponse.OriginalException?.Message ?? "Unknown error";
                throw new InvalidOperationException($"Failed to create 'products' index: {error}");
            }

            _logger.LogInformation("Elasticsearch index 'products' created with mappings.");
        }

        public async Task IndexProductAsync(Product product)
        {
            // Uses Index (not IndexDocument) with explicit Id for idempotency.
            // Re-indexing the same product.Id overwrites the document — no duplicates.
            var response = await _client.IndexAsync(product, i => i.Index("products").Id(product.Id));
            if (!response.IsValid)
            {
                var error = response.ServerError?.Error?.Reason
                    ?? response.OriginalException?.Message ?? "Unknown error";
                _logger.LogError("Failed to index product {Id}: {Error}", product.Id, error);
                throw new InvalidOperationException($"Failed to index product {product.Id}: {error}");
            }

            _logger.LogInformation("Product {Id} indexed in Elasticsearch.", product.Id);
        }

        public async Task DeleteProductFromIndexAsync(int id)
        {
            var response = await _client.DeleteAsync<Product>(id, d => d.Index("products"));

            // 404 (NotFound) is acceptable — the document is already gone
            if (!response.IsValid && response.Result != Result.NotFound)
            {
                var error = response.ServerError?.Error?.Reason
                    ?? response.OriginalException?.Message ?? "Unknown error";
                _logger.LogError("Failed to delete product {Id} from ES: {Error}", id, error);
                throw new InvalidOperationException($"Failed to delete product {id} from ES: {error}");
            }

            _logger.LogInformation("Product {Id} deleted from Elasticsearch.", id);
        }

        public async Task<BulkIndexResult> BulkIndexAsync(IEnumerable<Product> products, int batchSize = 50)
        {
            var result = new BulkIndexResult();
            var productList = products.ToList();
            result.TotalRequested = productList.Count;

            for (int i = 0; i < productList.Count; i += batchSize)
            {
                var batch = productList.Skip(i).Take(batchSize).ToList();

                try
                {
                    var response = await _client.BulkAsync(b => b
                        .Index("products")
                        .IndexMany(batch, (descriptor, product) => descriptor.Id(product.Id))
                    );

                    if (response.Errors)
                    {
                        // Log each failed document individually
                        foreach (var item in response.ItemsWithErrors)
                        {
                            result.Failed++;
                            result.FailedDocuments.Add(new BulkIndexError
                            {
                                ProductId = int.TryParse(item.Id, out var pid) ? pid : 0,
                                Error = item.Error?.Reason ?? "Unknown error"
                            });
                            _logger.LogError("Bulk index failed for document {Id}: {Error}",
                                item.Id, item.Error?.Reason);
                        }
                        result.Succeeded += response.Items.Count - response.ItemsWithErrors.Count();
                    }
                    else
                    {
                        result.Succeeded += response.Items.Count;
                    }
                }
                catch (Exception ex)
                {
                    // Entire batch failed (network error, timeout, etc.)
                    _logger.LogError(ex, "Entire batch {Start}-{End} failed during bulk indexing.",
                        i, Math.Min(i + batchSize, productList.Count));

                    foreach (var product in batch)
                    {
                        result.Failed++;
                        result.FailedDocuments.Add(new BulkIndexError
                        {
                            ProductId = product.Id,
                            Error = $"Batch-level failure: {ex.Message}"
                        });
                    }
                    // Continue to next batch — do NOT abort the entire operation
                }
            }

            _logger.LogInformation("Bulk index complete: {Succeeded}/{Total} succeeded, {Failed} failed.",
                result.Succeeded, result.TotalRequested, result.Failed);

            return result;
        }

        public async Task<IEnumerable<Product>> SearchAsync(string query, int from = 0, int size = 10)
        {
            var response = await _client.SearchAsync<Product>(s => s
                .Index("products")
                .From(from)
                .Size(size)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f
                            .Field(p => p.Id)
                            .Field(p => p.Name)
                            .Field(p => p.Description)
                        )
                        .Query(query)
                    )
                )
            );

            if (!response.IsValid)
            {
                var error = response.ServerError?.Error?.Reason
                    ?? response.OriginalException?.Message ?? "Unknown error";
                _logger.LogError("Search failed: {Error}", error);
                throw new InvalidOperationException($"Search failed: {error}");
            }

            return response.Documents;
        }
    }
}
