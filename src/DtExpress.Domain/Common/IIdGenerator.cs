namespace DtExpress.Domain.Common;

/// <summary>Generates unique identifiers. Abstracted for deterministic testing.</summary>
public interface IIdGenerator
{
    Guid NewId();
}
