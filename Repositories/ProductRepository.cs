using Microsoft.Data.SqlClient;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Repositories
{
    /// <summary>
    /// Pure SQL data access for the Products table. No business logic.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ElasticSettings settings, ILogger<ProductRepository> logger)
        {
            _connectionString = settings.SqlDBConnection;
            _logger = logger;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"INSERT INTO Products (Name, Description, Price, Category)
                                     OUTPUT INSERTED.Id
                                     VALUES (@Name, @Description, @Price, @Category)";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Name", product.Name);
                command.Parameters.AddWithValue("@Description", (object?)product.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Category", (object?)product.Category ?? DBNull.Value);

                var result = await command.ExecuteScalarAsync();
                if (result == null)
                    throw new InvalidOperationException("INSERT did not return a generated Id.");

                product.Id = (int)result;
                return product;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error creating product.");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"UPDATE Products
                                     SET Name = @Name, Description = @Description,
                                         Price = @Price, Category = @Category
                                     WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", product.Id);
                command.Parameters.AddWithValue("@Name", product.Name);
                command.Parameters.AddWithValue("@Description", (object?)product.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Category", (object?)product.Category ?? DBNull.Value);

                int rows = await command.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error updating product {Id}.", product.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("DELETE FROM Products WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);

                int rows = await command.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error deleting product {Id}.", id);
                throw;
            }
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = "SELECT Id, Name, Description, Price, Category FROM Products WHERE Id = @Id";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Product
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Category = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                    };
                }

                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error fetching product {Id}.", id);
                throw;
            }
        }

        public async Task<List<Product>> GetAllAsync()
        {
            try
            {
                var products = new List<Product>();
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = "SELECT Id, Name, Description, Price, Category FROM Products";
                using var command = new SqlCommand(sql, connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    products.Add(new Product
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Category = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                    });
                }

                return products;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error fetching all products.");
                throw;
            }
        }
    }
}