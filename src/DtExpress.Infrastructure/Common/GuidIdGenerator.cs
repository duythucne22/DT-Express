using DtExpress.Domain.Common;

namespace DtExpress.Infrastructure.Common;

/// <summary>
/// Production implementation of <see cref="IIdGenerator"/>.
/// Generates unique identifiers via <see cref="Guid.NewGuid"/>.
/// </summary>
public sealed class GuidIdGenerator : IIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}
