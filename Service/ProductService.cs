using Microsoft.Data.SqlClient;
using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Models;
using System.Data;

namespace SearchAPI.Service
{
    public class ProductService : IProductService
    {
        private readonly IElasticClient _elasticClient;

        private readonly ILogger<ProductService> _logger;
        private readonly string? _connectionString;


        public ProductService(IElasticClient elasticClient, ILogger<ProductService> logger, string? connectionString)
        {
            _elasticClient = elasticClient;
            _logger = logger;
            _connectionString = connectionString;
        }

        //public ProductService(string? connectionString)
        //{
        //    _connectionString = connectionString;
        //}

        public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
        {
           
            var response = await _elasticClient.SearchAsync<Product>(s => s
            .Index("products")
            .From(0)
            .Size(10)
            .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f
                            .Field(p => p.Id)
                            .Field(p => p.Name)
                            .Field(p => p.Description)
                        )
                        .Query(query)
                    )
                )
            );
            if (!response.IsValid)
            {
                _logger.LogError("Search Query Failed Due To", response.OriginalException.Message);
                throw new Exception("Search Query Failed+" + response.OriginalException.Message + " +");

            }

            return response.Documents;
        }

        public async Task<string> CreateProductsAsync(Product product)
        {
            String str = string.Empty;
            // var response = await _elasticClient.IndexDocumentAsync(product);
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO Products (Name, Description, Price, Category) OUTPUT INSERTED.Id VALUES (@Name, @Description, @Price, @Category)", connection);
                command.Parameters.AddWithValue("@Name", product.Name);
                command.Parameters.AddWithValue("@Description", product.Description);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Category", product.Category);

                int value = (int)await command.ExecuteScalarAsync();

                if (value > 0)
                {
                    str = "Product created";
                }
                else { str = "data not inserted in DB"; }
            }
            // return response.IsValid ? "Product created":"";
            return str;
        }

        //public async Task<string> DeleteProductAsync(int id)
        // {
        //     var response = await _elasticClient.DeleteAsync( new DocumentPath<Product>(id));
        //     return response.IsValid ? "Product Deleted" : "";
        // }
        public async Task DeleteProductAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("DELETE FROM Products WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("UPDATE Products SET Name = @Name, Description = @Description, Price = @Price, Category = @Category WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", product.Id);
                command.Parameters.AddWithValue("@Name", product.Name);
                command.Parameters.AddWithValue("@Description", product.Description);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Category", product.Category);

                await command.ExecuteNonQueryAsync();
            }
        }
        public async Task<Product> GetProductsAsync(int id)
        {
            Product product = null;
            //IDbConnection connection = new SqlConnection(_connectionString);
            //propa = connection.Database.
            //var product = await _elasticClient.GetAsync(new DocumentPath<Product>(id));
            //var response = await _elasticClient.GetAsync(new DocumentPath<Product>(id));
            //return response.Source;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT Id, Name, Description, Price FROM Products WHERE Id = @Id";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        product = new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            Price = reader.GetDecimal(3)
                        };
                    }
                }

            }

            return product;
        }
        public async Task<List<Product>> GetAllProducts()
        {
            try
            {
                List<Product> products = new List<Product>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "SELECT Id, Name, Description, Price FROM Products ";
                    var command = new SqlCommand(query, connection);
                    command.CommandType = CommandType.Text;
                    using (SqlDataAdapter sda = new SqlDataAdapter(command))
                    {
                        using (DataTable dt = new DataTable())
                        {
                            try
                            {
                                sda.Fill(dt);
                            }
                            catch (Exception ex)
                            {
                               // throw ex;
                            }
                            if (dt.Rows.Count > 0)
                            {
                                products = (from DataRow row in dt.Rows

                                            select new Product
                                            {
                                                Id = Convert.ToInt32( row["Id"]),
                                                Name = row["Name"].ToString()
                                                ,
                                                Description = row["Description"].ToString(),
                                                Price = Convert.ToInt32(row["Price"])
                                                
                                            }).ToList();

                            }
                        }
                    }
                    
                         
                     return products;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching products from the database.");
                throw;
            }
        }
    }
}

