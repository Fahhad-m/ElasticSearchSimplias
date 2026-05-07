using Microsoft.Data.SqlClient;
using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Models;
using System.Data;

namespace SearchAPI.Service
{
    /// <summary>
    /// Implements SQL database operations and Elasticsearch search for Products.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<ProductService> _logger;
        private readonly ElasticSettings _elasticSettings;

        public ProductService(IElasticClient elasticClient, ILogger<ProductService> logger, ElasticSettings elasticSettings)
        {
            _elasticClient = elasticClient;
            _logger = logger;
            _elasticSettings = elasticSettings;
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    throw new ArgumentException("Search query cannot be empty.");

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
                    _logger.LogError("Elasticsearch search failed: {Error}", response.ServerError?.Error?.Reason ?? response.OriginalException?.Message);
                    throw new Exception("Search query failed: " + (response.ServerError?.Error?.Reason ?? response.OriginalException?.Message));
                }

                return response.Documents;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching products in Elasticsearch.");
                throw;
            }
        }

        public async Task<Product> CreateProductsAsync(Product product)
        {
            try
            {
                using (var connection = new SqlConnection(_elasticSettings.SqlDBConnection))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand(
                        "INSERT INTO Products (Name, Description, Price, Category) OUTPUT INSERTED.Id VALUES (@Name, @Description, @Price, @Category)",
                        connection);
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Description", product.Description);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@Category", product.Category);

                    var result = await command.ExecuteScalarAsync();
                    if (result == null)
                        throw new Exception("Failed to insert product into database — no ID returned.");

                    product.Id = (int)result;
                }

                return product;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while creating product.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the product.");
                throw;
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("Product ID must be a positive integer.");

                using (var connection = new SqlConnection(_elasticSettings.SqlDBConnection))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("DELETE FROM Products WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                        _logger.LogWarning("Delete: No product found with Id {Id}.", id);
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while deleting product with Id {Id}.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting product with Id {Id}.", id);
                throw;
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            try
            {
                if (product.Id <= 0)
                    throw new ArgumentException("Product ID must be a positive integer.");

                using (var connection = new SqlConnection(_elasticSettings.SqlDBConnection))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand(
                        "UPDATE Products SET Name = @Name, Description = @Description, Price = @Price, Category = @Category WHERE Id = @Id",
                        connection);
                    command.Parameters.AddWithValue("@Id", product.Id);
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Description", product.Description);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@Category", product.Category);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                        _logger.LogWarning("Update: No product found with Id {Id}.", product.Id);
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while updating product with Id {Id}.", product.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating product with Id {Id}.", product.Id);
                throw;
            }
        }

        public async Task<Product> GetProductsAsync(int id)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("Product ID must be a positive integer.");

                Product product = null;
                using (var connection = new SqlConnection(_elasticSettings.SqlDBConnection))
                {
                    var query = "SELECT Id, Name, Description, Price, Category FROM Products WHERE Id = @Id";
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
                                Price = reader.GetDecimal(3),
                                Category = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                            };
                        }
                    }
                }

                return product;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching product with Id {Id}.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching product with Id {Id}.", id);
                throw;
            }
        }

        public async Task<List<Product>> GetAllProducts()
        {
            try
            {
                List<Product> products = new List<Product>();
                using (var connection = new SqlConnection(_elasticSettings.SqlDBConnection))
                {
                    var query = "SELECT Id, Name, Description, Price, Category FROM Products";
                    var command = new SqlCommand(query, connection);
                    command.CommandType = CommandType.Text;

                    using (SqlDataAdapter sda = new SqlDataAdapter(command))
                    {
                        using (DataTable dt = new DataTable())
                        {
                            sda.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                products = (from DataRow row in dt.Rows
                                            select new Product
                                            {
                                                Id = Convert.ToInt32(row["Id"]),
                                                Name = row["Name"]?.ToString() ?? string.Empty,
                                                Description = row["Description"]?.ToString() ?? string.Empty,
                                                Price = Convert.ToDecimal(row["Price"]),
                                                Category = row["Category"]?.ToString() ?? string.Empty
                                            }).ToList();
                            }
                        }
                    }

                    return products;
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching all products.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all products.");
                throw;
            }
        }
    }
}

