using SearchAPI.Models;

namespace SearchAPI.Interfaces
{
    /// <summary>
    /// Repository layer — SQL data access for the SyncOutbox table.
    /// Used by ProductService (to add entries) and the background service (to process them).
    /// </summary>
    public interface ISyncOutboxRepository
    {
        Task<int> AddAsync(SyncOutboxEntry entry);
        Task<List<SyncOutboxEntry>> GetPendingAsync(int batchSize = 50);
        Task MarkAsProcessingAsync(int id);
        Task MarkAsCompletedAsync(int id);
        Task MarkAsFailedAsync(int id, string errorMessage);
        Task MarkForRetryAsync(int id, string errorMessage, DateTime nextRetryAt);
    }
}