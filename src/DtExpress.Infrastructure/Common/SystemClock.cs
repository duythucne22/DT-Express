using DtExpress.Domain.Common;

namespace DtExpress.Infrastructure.Common;

/// <summary>
/// Production implementation of <see cref="IClock"/>.
/// Returns the current UTC time via <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
