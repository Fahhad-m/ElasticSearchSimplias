using SearchAPI.Models;

namespace SearchAPI.Interfaces
{
    public interface IElasticsearchService
    {
        Task IndexProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
    }
}