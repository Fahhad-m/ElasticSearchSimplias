using SearchAPI.Models;

namespace SearchAPI.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> SearchProductsAsync(string query);
        Task<string> CreateProductsAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        Task<Product> GetProductsAsync(int id);
        Task<List< Product>> GetAllProducts();

    }
}
