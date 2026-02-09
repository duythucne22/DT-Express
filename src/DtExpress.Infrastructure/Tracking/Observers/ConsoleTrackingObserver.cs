using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;
using Microsoft.Extensions.Logging;

namespace DtExpress.Infrastructure.Tracking.Observers;

/// <summary>
/// Observer Pattern — Observer: logs tracking events to <see cref="ILogger"/>
/// with Chinese status names and structured output.
/// <para>
/// Registered as a default observer for demo/diagnostic purposes.
/// In production this would be replaced by a WebSocket push, SMS gateway, etc.
/// </para>
/// </summary>
public sealed class ConsoleTrackingObserver : ITrackingObserver
{
    private readonly ILogger<ConsoleTrackingObserver> _logger;

    public ConsoleTrackingObserver(ILogger<ConsoleTrackingObserver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Logs the event with Chinese status labels:
    /// Created=已创建, PickedUp=已揽收, InTransit=运输中,
    /// OutForDelivery=派送中, Delivered=已签收, Exception=异常.
    /// </remarks>
    public Task OnTrackingEventAsync(TrackingEvent evt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        var statusLabel = evt.NewStatus.HasValue
            ? ToChineseStatus(evt.NewStatus.Value)
            : "位置更新";

        var locationText = evt.Location is not null
            ? $"({evt.Location.Latitude:F4}°N, {evt.Location.Longitude:F4}°E)"
            : "未知位置";

        _logger.LogInformation(
            "📦 [{TrackingNumber}] {StatusLabel} — {Description} · 位置: {Location} · 时间: {OccurredAt:yyyy-MM-dd HH:mm:ss}",
            evt.TrackingNumber,
            statusLabel,
            evt.Description ?? "(无描述)",
            locationText,
            evt.OccurredAt);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Map <see cref="ShipmentStatus"/> to Chinese display label.
    /// </summary>
    private static string ToChineseStatus(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Created        => "已创建",
        ShipmentStatus.PickedUp       => "已揽收",
        ShipmentStatus.InTransit      => "运输中",
        ShipmentStatus.OutForDelivery => "派送中",
        ShipmentStatus.Delivered      => "已签收",
        ShipmentStatus.Exception      => "异常",
        _                             => status.ToString(),
    };
}
