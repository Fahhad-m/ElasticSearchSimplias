using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Service
{

    public class ElasticsearchService : IElasticsearchService
    {
        private readonly IElasticClient _client;

        public ElasticsearchService(IElasticClient client)
        {
            _client = client;
        }

        public async Task IndexProductAsync(Product product)
        {
            await _client.IndexDocumentAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _client.UpdateAsync<Product>(product.Id, u => u.Doc(product));
        }

        public async Task DeleteProductAsync(int id)
        {
            await _client.DeleteAsync<Product>(id);
        }
    }
}
