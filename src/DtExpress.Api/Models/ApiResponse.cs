namespace DtExpress.Api.Models;

/// <summary>
/// Unified API response envelope for all endpoints.
/// Every response wraps data in this structure for consistent client consumption.
/// </summary>
/// <typeparam name="T">The type of the response payload.</typeparam>
public sealed record ApiResponse<T>
{
    /// <summary>Whether the request succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>The response payload (null on failure).</summary>
    public T? Data { get; init; }

    /// <summary>Error details (null on success).</summary>
    public ApiError? Error { get; init; }

    /// <summary>Request correlation ID for distributed tracing.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Create a successful response with data.</summary>
    public static ApiResponse<T> Ok(T data, string? correlationId = null)
        => new() { Success = true, Data = data, CorrelationId = correlationId };

    /// <summary>Create a failure response with error details.</summary>
    public static ApiResponse<T> Fail(string code, string message, string? correlationId = null)
        => new() { Success = false, Error = new ApiError(code, message), CorrelationId = correlationId };
}

/// <summary>Machine-readable error code and human-readable message.</summary>
public sealed record ApiError(string Code, string Message);
