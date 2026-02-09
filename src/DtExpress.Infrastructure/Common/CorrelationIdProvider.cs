using DtExpress.Domain.Common;

namespace DtExpress.Infrastructure.Common;

/// <summary>
/// Implements <see cref="ICorrelationIdProvider"/> using <see cref="AsyncLocal{T}"/>
/// to maintain a correlation ID scoped to the current async execution flow.
/// Thread-safe for concurrent async operations — each async context gets its own value.
/// </summary>
public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string?> _currentCorrelationId = new();

    /// <inheritdoc />
    /// <remarks>
    /// Returns the current correlation ID if set; otherwise generates a new GUID.
    /// The generated value is cached in the <see cref="AsyncLocal{T}"/> so subsequent
    /// calls within the same async flow return the same ID.
    /// </remarks>
    public string GetCorrelationId()
        => _currentCorrelationId.Value ??= Guid.NewGuid().ToString();

    /// <summary>
    /// Sets the correlation ID for the current async flow.
    /// Called by middleware (e.g., CorrelationIdMiddleware) at the start of a request.
    /// </summary>
    /// <param name="correlationId">The correlation ID to associate with this async flow.</param>
    public void SetCorrelationId(string correlationId)
        => _currentCorrelationId.Value = correlationId;
}
