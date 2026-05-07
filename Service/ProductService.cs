using SearchAPI.Interfaces;
using SearchAPI.Models;
using System.Text.Json;
using System.Transactions;

namespace SearchAPI.Service
{
    /// <summary>
    /// Orchestrates Product operations using the Transactional Outbox pattern.
    ///
    /// Write flow:
    ///   1. Open TransactionScope
    ///   2. Write to SQL (source of truth)
    ///   3. Write outbox entry (same transaction — atomic with step 2)
    ///   4. Commit transaction
    ///   5. Attempt immediate ES sync (best-effort, outside transaction)
    ///   6. If ES sync succeeds → mark outbox Completed
    ///      If ES sync fails → outbox stays Pending → background service retries
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ISyncOutboxRepository _outboxRepository;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            ISyncOutboxRepository outboxRepository,
            IElasticsearchService elasticsearchService,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _outboxRepository = outboxRepository;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            Product created;
            int outboxId;

            // Atomic: SQL INSERT + outbox entry in one transaction
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                created = await _productRepository.CreateAsync(product);

                outboxId = await _outboxRepository.AddAsync(new SyncOutboxEntry
                {
                    EntityId = created.Id,
                    OperationType = "Index",
                    Payload = JsonSerializer.Serialize(created)
                });

                scope.Complete();
            }

            // Best-effort immediate ES sync (outside transaction, non-blocking)
            await TryImmediateEsSyncAsync(outboxId, () => _elasticsearchService.IndexProductAsync(created),
                "index", created.Id);

            return created;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            bool updated;
            int outboxId;

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                updated = await _productRepository.UpdateAsync(product);
                if (!updated)
                    return false;

                outboxId = await _outboxRepository.AddAsync(new SyncOutboxEntry
                {
                    EntityId = product.Id,
                    OperationType = "Index", // Index = full overwrite, idempotent
                    Payload = JsonSerializer.Serialize(product)
                });

                scope.Complete();
            }

            await TryImmediateEsSyncAsync(outboxId, () => _elasticsearchService.IndexProductAsync(product),
                "update", product.Id);

            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            bool deleted;
            int outboxId;

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                deleted = await _productRepository.DeleteAsync(id);
                if (!deleted)
                    return false;

                outboxId = await _outboxRepository.AddAsync(new SyncOutboxEntry
                {
                    EntityId = id,
                    OperationType = "Delete"
                });

                scope.Complete();
            }

            await TryImmediateEsSyncAsync(outboxId, () => _elasticsearchService.DeleteProductFromIndexAsync(id),
                "delete", id);

            return true;
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetByIdAsync(id);
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
        {
            return await _elasticsearchService.SearchAsync(query);
        }

        public async Task<BulkIndexResult> BulkIndexAllProductsAsync()
        {
            var products = await _productRepository.GetAllAsync();
            if (products.Count == 0)
                return new BulkIndexResult();

            return await _elasticsearchService.BulkIndexAsync(products);
        }

        /// <summary>
        /// Attempts immediate ES sync after the SQL transaction commits.
        /// If it succeeds, marks the outbox entry as Completed so the background
        /// service won't re-process it. If it fails, logs a warning and leaves
        /// the outbox entry as Pending for the background service to retry.
        /// </summary>
        private async Task TryImmediateEsSyncAsync(int outboxId, Func<Task> esOperation, string opName, int entityId)
        {
            try
            {
                await esOperation();
                await _outboxRepository.MarkAsCompletedAsync(outboxId);
                _logger.LogInformation("Immediate ES {Op} succeeded for product {Id}.", opName, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Immediate ES {Op} failed for product {Id}. Outbox entry {OutboxId} will be retried by background service.",
                    opName, entityId, outboxId);
            }
        }
    }
}

