using DtExpress.Domain.Common;

namespace DtExpress.Api.Middleware;

/// <summary>
/// Middleware that reads or generates a correlation ID for each request.
/// Sets the correlation ID on the CorrelationIdProvider (AsyncLocal-backed)
/// and includes it in the response header for client tracing.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdProvider correlationIdProvider)
    {
        // Read from request header, or generate a new one
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Set on the AsyncLocal-backed provider so all downstream services see it
        correlationIdProvider.SetCorrelationId(correlationId);

        // Add to response headers for client tracing
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
