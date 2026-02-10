using DtExpress.Api.Models;
using DtExpress.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DtExpress.Api.Filters;

/// <summary>
/// Global exception filter that maps domain exceptions to proper HTTP responses.
/// Centralizes error handling so controllers stay clean.
/// <para>
/// Mapping: DomainException → 400, CarrierNotFoundException → 404,
/// ArgumentException → 400, unhandled → 500.
/// </para>
/// </summary>
public sealed class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ICorrelationIdProvider _correlationId;
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(
        ICorrelationIdProvider correlationId,
        ILogger<GlobalExceptionFilter> logger)
    {
        _correlationId = correlationId;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var correlationId = _correlationId.GetCorrelationId();
        var exception = context.Exception;

        var (statusCode, code, message) = exception switch
        {
            CarrierNotFoundException ex =>
                (StatusCodes.Status404NotFound, ex.Code, ex.Message),

            InvalidStateTransitionException ex =>
                (StatusCodes.Status400BadRequest, ex.Code, ex.Message),

            StrategyNotFoundException ex =>
                (StatusCodes.Status400BadRequest, ex.Code, ex.Message),

            DomainException ex =>
                (StatusCodes.Status400BadRequest, ex.Code, ex.Message),

            ArgumentException ex =>
                (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ex.Message),

            _ =>
                (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        // Log server errors at Error level, client errors at Warning
        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception [CorrelationId={CorrelationId}]", correlationId);
        else
            _logger.LogWarning("Domain/validation error: {Code} — {Message} [CorrelationId={CorrelationId}]",
                code, message, correlationId);

        var response = ApiResponse<object>.Fail(code, message, correlationId);

        context.Result = new ObjectResult(response) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
