using SearchAPI.Models;

namespace SearchAPI.Interfaces
{
    /// <summary>
    /// Repository layer — pure SQL data access for the Products table.
    /// No business logic, no Elasticsearch awareness.
    /// </summary>
    public interface IProductRepository
    {
        Task<Product> CreateAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<Product?> GetByIdAsync(int id);
        Task<List<Product>> GetAllAsync();
    }
}