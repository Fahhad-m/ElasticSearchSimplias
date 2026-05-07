namespace SearchAPI.Models
{
    /// <summary>
    /// Standardized API response envelope. Every endpoint returns this shape
    /// so clients can rely on a consistent contract.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors };
    }

    /// <summary>
    /// Result of a bulk indexing operation. Failed documents are tracked
    /// individually so the caller knows exactly which products failed and why.
    /// </summary>
    public class BulkIndexResult
    {
        public int TotalRequested { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<BulkIndexError> FailedDocuments { get; set; } = new();
    }

    public class BulkIndexError
    {
        public int ProductId { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}