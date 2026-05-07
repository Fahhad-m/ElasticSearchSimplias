using Microsoft.Data.SqlClient;
using SearchAPI.Interfaces;
using SearchAPI.Models;

namespace SearchAPI.Repositories
{
    /// <summary>
    /// SQL data access for the SyncOutbox table.
    /// </summary>
    public class SyncOutboxRepository : ISyncOutboxRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SyncOutboxRepository> _logger;

        public SyncOutboxRepository(ElasticSettings settings, ILogger<SyncOutboxRepository> logger)
        {
            _connectionString = settings.SqlDBConnection;
            _logger = logger;
        }

        public async Task<int> AddAsync(SyncOutboxEntry entry)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"INSERT INTO SyncOutbox
                        (EntityId, EntityType, OperationType, Payload, Status, RetryCount, MaxRetries, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@EntityId, @EntityType, @OperationType, @Payload, @Status, @RetryCount, @MaxRetries, @CreatedAt)";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@EntityId", entry.EntityId);
                command.Parameters.AddWithValue("@EntityType", entry.EntityType);
                command.Parameters.AddWithValue("@OperationType", entry.OperationType);
                command.Parameters.AddWithValue("@Payload", (object?)entry.Payload ?? DBNull.Value);
                command.Parameters.AddWithValue("@Status", entry.Status);
                command.Parameters.AddWithValue("@RetryCount", entry.RetryCount);
                command.Parameters.AddWithValue("@MaxRetries", entry.MaxRetries);
                command.Parameters.AddWithValue("@CreatedAt", entry.CreatedAt);

                var result = await command.ExecuteScalarAsync();
                return (int)result!;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error adding outbox entry for entity {EntityId}.", entry.EntityId);
                throw;
            }
        }

        public async Task<List<SyncOutboxEntry>> GetPendingAsync(int batchSize = 50)
        {
            var entries = new List<SyncOutboxEntry>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"SELECT TOP (@BatchSize)
                        Id, EntityId, EntityType, OperationType, Payload,
                        Status, RetryCount, MaxRetries, CreatedAt,
                        LastAttemptAt, NextRetryAt, ErrorMessage
                    FROM SyncOutbox
                    WHERE Status = 'Pending' AND (NextRetryAt IS NULL OR NextRetryAt <= @Now)
                    ORDER BY Id ASC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BatchSize", batchSize);
                command.Parameters.AddWithValue("@Now", DateTime.UtcNow);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    entries.Add(new SyncOutboxEntry
                    {
                        Id = reader.GetInt32(0),
                        EntityId = reader.GetInt32(1),
                        EntityType = reader.GetString(2),
                        OperationType = reader.GetString(3),
                        Payload = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Status = reader.GetString(5),
                        RetryCount = reader.GetInt32(6),
                        MaxRetries = reader.GetInt32(7),
                        CreatedAt = reader.GetDateTime(8),
                        LastAttemptAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                        NextRetryAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                        ErrorMessage = reader.IsDBNull(11) ? null : reader.GetString(11)
                    });
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error fetching pending outbox entries.");
                throw;
            }

            return entries;
        }

        public async Task MarkAsProcessingAsync(int id)
        {
            await UpdateStatusAsync(id, "Processing", null);
        }

        public async Task MarkAsCompletedAsync(int id)
        {
            await UpdateStatusAsync(id, "Completed", null);
        }

        public async Task MarkAsFailedAsync(int id, string errorMessage)
        {
            await UpdateStatusAsync(id, "Failed", errorMessage);
        }

        public async Task MarkForRetryAsync(int id, string errorMessage, DateTime nextRetryAt)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"UPDATE SyncOutbox
                    SET Status = 'Pending', RetryCount = RetryCount + 1,
                        LastAttemptAt = @Now, NextRetryAt = @NextRetryAt, ErrorMessage = @Error
                    WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Now", DateTime.UtcNow);
                command.Parameters.AddWithValue("@NextRetryAt", nextRetryAt);
                command.Parameters.AddWithValue("@Error", (object?)errorMessage ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error marking outbox entry {Id} for retry.", id);
            }
        }

        private async Task UpdateStatusAsync(int id, string status, string? errorMessage)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"UPDATE SyncOutbox
                    SET Status = @Status, LastAttemptAt = @Now, ErrorMessage = @Error
                    WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Now", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Error", (object?)errorMessage ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error updating outbox entry {Id} to {Status}.", id, status);
            }
        }
    }
}