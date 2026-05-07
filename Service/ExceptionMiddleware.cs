using System.Net;
using System.Text.Json;

/// <summary>
/// Global exception handler — last line of defence.
/// Catches anything that escapes the controller try-catch blocks and returns
/// a consistent JSON error response instead of an HTML stack trace or crash.
/// Guards against double-write (response already started).
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
            await WriteResponseAsync(httpContext, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operation failed: {Message}", ex.Message);
            await WriteResponseAsync(httpContext, HttpStatusCode.InternalServerError, "An internal error occurred.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteResponseAsync(httpContext, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        // Guard: if the response has already started streaming, we cannot modify headers/status
        if (context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            Success = false,
            StatusCode = (int)statusCode,
            Message = message
        });

        await context.Response.WriteAsync(payload);
    }
}