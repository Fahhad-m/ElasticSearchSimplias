using SearchAPI.Models;

namespace SearchAPI.Interfaces
{
    /// <summary>
    /// Defines SQL database operations for Products.
    /// </summary>
    public interface IProductService
    {
        /// <summary>Searches products via Elasticsearch across Id, Name, and Description fields.</summary>
        Task<IEnumerable<Product>> SearchProductsAsync(string query);

        /// <summary>Inserts a new product into SQL and returns it with the auto-generated Id.</summary>
        Task<Product> CreateProductsAsync(Product product);

        /// <summary>Updates an existing product in SQL.</summary>
        Task UpdateProductAsync(Product product);

        /// <summary>Deletes a product from SQL by its Id.</summary>
        Task DeleteProductAsync(int id);

        /// <summary>Retrieves a single product from SQL by its Id.</summary>
        Task<Product> GetProductsAsync(int id);

        /// <summary>Retrieves all products from SQL.</summary>
        Task<List<Product>> GetAllProducts();
    }
}
