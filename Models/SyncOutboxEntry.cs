namespace SearchAPI.Models
{
    /// <summary>
    /// Represents a pending synchronization operation between SQL and Elasticsearch.
    /// Part of the Outbox pattern: written atomically with the SQL change,
    /// then processed asynchronously by the background service.
    /// </summary>
    public class SyncOutboxEntry
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public string EntityType { get; set; } = "Product";
        public string OperationType { get; set; } = string.Empty; // Index, Update, Delete
        public string? Payload { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}