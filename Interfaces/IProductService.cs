using SearchAPI.Models;

namespace SearchAPI.Interfaces
{
    /// <summary>
    /// Service layer — orchestrates Product operations.
    /// Writes go to SQL (source of truth) inside a TransactionScope that also
    /// writes an outbox entry, then attempts an immediate ES sync.
    /// </summary>
    public interface IProductService
    {
        Task<Product> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<Product?> GetProductByIdAsync(int id);
        Task<List<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> SearchProductsAsync(string query);
        Task<BulkIndexResult> BulkIndexAllProductsAsync();
    }
}
