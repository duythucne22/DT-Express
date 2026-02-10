namespace DtExpress.Domain.Common;

/// <summary>Provides the current request's correlation ID for traceability.</summary>
public interface ICorrelationIdProvider
{
    /// <summary>Returns the correlation ID associated with the current execution flow.</summary>
    string GetCorrelationId();

    /// <summary>Sets the correlation ID for the current async flow.</summary>
    void SetCorrelationId(string correlationId);
}
