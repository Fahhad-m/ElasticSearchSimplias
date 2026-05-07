using SearchAPI.Models;

namespace SearchAPI.Interfaces
{
    /// <summary>
    /// Manages Elasticsearch operations for the Products index.
    /// </summary>
    public interface IElasticsearchService
    {
        /// <summary>Ensures the "products" index exists with the correct field mappings.</summary>
        Task EnsureIndexCreatedAsync();

        /// <summary>Indexes a single product document.</summary>
        Task IndexProductAsync(Product product);

        /// <summary>Bulk-indexes a collection of products (used for initial data sync from SQL).</summary>
        Task BulkIndexAsync(IEnumerable<Product> products);

        /// <summary>Updates an existing product document.</summary>
        Task UpdateProductAsync(Product product);

        /// <summary>Deletes a product document by its ID.</summary>
        Task DeleteProductAsync(int id);
    }
}