namespace DtExpress.Domain.Common;

/// <summary>Abstraction over system time for testability.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
