using SearchAPI.Models;

namespace SearchAPI.Interfaces
{
    /// <summary>
    /// Manages all Elasticsearch operations. Every write operation is idempotent:
    /// re-running the same operation produces the same result without duplicates.
    /// </summary>
    public interface IElasticsearchService
    {
        /// <summary>Creates the "products" index with proper field mappings if it does not exist.</summary>
        Task EnsureIndexCreatedAsync();

        /// <summary>Indexes (or overwrites) a single product document using product.Id as the ES doc ID.</summary>
        Task IndexProductAsync(Product product);

        /// <summary>Deletes a product document. Returns gracefully if doc does not exist (404).</summary>
        Task DeleteProductFromIndexAsync(int id);

        /// <summary>
        /// Bulk-indexes products in batches. A single failed document does NOT fail the entire batch.
        /// Failed documents are returned in BulkIndexResult.FailedDocuments.
        /// </summary>
        Task<BulkIndexResult> BulkIndexAsync(IEnumerable<Product> products, int batchSize = 50);

        /// <summary>Full-text search across Id, Name, and Description fields.</summary>
        Task<IEnumerable<Product>> SearchAsync(string query, int from = 0, int size = 10);
    }
}