using SearchAPI.Interfaces;
using SearchAPI.Models;
using System.Text.Json;

namespace SearchAPI.Service
{
    /// <summary>
    /// Background hosted service with two responsibilities:
    ///
    /// Phase 1 (startup): Ensure the ES index exists, then bulk-sync all SQL data into ES.
    /// Phase 2 (continuous): Poll the SyncOutbox table every 5 seconds and process pending
    ///          entries with exponential backoff retry (10s → 30s → 90s, max 3 retries).
    ///
    /// This is the safety net that guarantees eventual consistency between SQL and ES
    /// even when the immediate sync in ProductService fails.
    /// </summary>
    public class ElasticsearchSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ElasticsearchSyncBackgroundService> _logger;
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

        public ElasticsearchSyncBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ElasticsearchSyncBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ElasticsearchSyncBackgroundService started.");

            await InitializeIndexAsync();
            await PerformInitialSyncAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in outbox processing loop.");
                }

                try
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("ElasticsearchSyncBackgroundService stopped.");
        }

        private async Task InitializeIndexAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var esService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
                await esService.EnsureIndexCreatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure ES index on startup. Will continue — outbox will retry.");
            }
        }

        private async Task PerformInitialSyncAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
                var esService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

                var products = await productRepo.GetAllAsync();
                if (products.Count == 0)
                {
                    _logger.LogInformation("Initial sync: No products in SQL. Skipping.");
                    return;
                }

                var result = await esService.BulkIndexAsync(products);
                _logger.LogInformation("Initial sync complete: {Succeeded}/{Total} succeeded, {Failed} failed.",
                    result.Succeeded, result.TotalRequested, result.Failed);

                foreach (var error in result.FailedDocuments)
                    _logger.LogError("Initial sync failed for product {Id}: {Error}", error.ProductId, error.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initial bulk sync failed. ES may be out of date until outbox catches up.");
            }
        }

        private async Task ProcessOutboxAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<ISyncOutboxRepository>();
            var esService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

            var entries = await outboxRepo.GetPendingAsync(50);
            if (entries.Count == 0)
                return;

            _logger.LogInformation("Processing {Count} outbox entries.", entries.Count);

            foreach (var entry in entries)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await outboxRepo.MarkAsProcessingAsync(entry.Id);

                try
                {
                    await ExecuteOutboxEntryAsync(entry, esService);
                    await outboxRepo.MarkAsCompletedAsync(entry.Id);
                    _logger.LogInformation("Outbox {Id} completed ({Op} for entity {EntityId}).",
                        entry.Id, entry.OperationType, entry.EntityId);
                }
                catch (Exception ex)
                {
                    int nextRetry = entry.RetryCount + 1;
                    if (nextRetry >= entry.MaxRetries)
                    {
                        await outboxRepo.MarkAsFailedAsync(entry.Id, ex.Message);
                        _logger.LogError(ex,
                            "Outbox {Id} permanently FAILED after {Retries} retries ({Op} for entity {EntityId}).",
                            entry.Id, nextRetry, entry.OperationType, entry.EntityId);
                    }
                    else
                    {
                        // Exponential backoff: 10s, 30s, 90s
                        var delay = TimeSpan.FromSeconds(10 * Math.Pow(3, nextRetry - 1));
                        var nextRetryAt = DateTime.UtcNow.Add(delay);
                        await outboxRepo.MarkForRetryAsync(entry.Id, ex.Message, nextRetryAt);
                        _logger.LogWarning(ex,
                            "Outbox {Id} failed (attempt {Retry}/{Max}). Next retry at {NextRetry}.",
                            entry.Id, nextRetry, entry.MaxRetries, nextRetryAt);
                    }
                }
            }
        }

        private static async Task ExecuteOutboxEntryAsync(SyncOutboxEntry entry, IElasticsearchService esService)
        {
            switch (entry.OperationType)
            {
                case "Index":
                    if (string.IsNullOrEmpty(entry.Payload))
                        throw new InvalidOperationException("Outbox entry has no payload for Index operation.");
                    var product = JsonSerializer.Deserialize<Product>(entry.Payload)
                        ?? throw new InvalidOperationException("Failed to deserialize outbox payload.");
                    await esService.IndexProductAsync(product);
                    break;

                case "Delete":
                    await esService.DeleteProductFromIndexAsync(entry.EntityId);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown outbox operation: {entry.OperationType}");
            }
        }
    }
}