using SearchAPI.Interfaces;

namespace SearchAPI.Service
{
    /// <summary>
    /// Background hosted service that runs once on application startup.
    /// It ensures the Elasticsearch index exists, reads all products from
    /// the SQL database, and bulk-indexes them into Elasticsearch so the
    /// two data stores are in sync.
    /// </summary>
    public class ElasticsearchIndexingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ElasticsearchIndexingService> _logger;

        public ElasticsearchIndexingService(IServiceProvider serviceProvider, ILogger<ElasticsearchIndexingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ElasticsearchIndexingService: Starting initial data sync from SQL to Elasticsearch.");

            try
            {
                // Create a scope because the service dependencies are registered as Scoped
                using var scope = _serviceProvider.CreateScope();
                var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                // Step 1: Ensure the ES index exists with correct mappings
                await elasticsearchService.EnsureIndexCreatedAsync();

                // Step 2: Read all products from SQL
                var products = await productService.GetAllProducts();

                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning("ElasticsearchIndexingService: No products found in SQL database. Skipping bulk index.");
                    return;
                }

                // Step 3: Bulk-index all products into Elasticsearch
                await elasticsearchService.BulkIndexAsync(products);

                _logger.LogInformation("ElasticsearchIndexingService: Successfully indexed {Count} products from SQL into Elasticsearch.", products.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ElasticsearchIndexingService: Failed to sync data from SQL to Elasticsearch.");
            }
        }
    }
}