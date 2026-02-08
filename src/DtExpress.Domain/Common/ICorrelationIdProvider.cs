namespace DtExpress.Domain.Common;

/// <summary>Provides the current request's correlation ID for traceability.</summary>
public interface ICorrelationIdProvider
{
    string GetCorrelationId();
}
