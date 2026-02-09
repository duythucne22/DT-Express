using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Tracking.Enums;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;
using DtExpress.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DtExpress.Infrastructure.Tracking.Sources;

/// <summary>
/// Tracking source that replays a predefined, deterministic event sequence.
/// Ideal for demos and integration tests where reproducibility is required.
/// <para>
/// Simulates a full Chinese domestic shipment lifecycle:
/// Created → PickedUp → InTransit (multiple hops) → OutForDelivery → Delivered.
/// Events are published at configurable intervals (default: 1 second between events).
/// </para>
/// </summary>
public sealed class ScriptedTrackingSource : ITrackingSource
{
    private readonly ITrackingSubject _subject;
    private readonly ILogger<ScriptedTrackingSource> _logger;
    private readonly TimeSpan _eventInterval;

    /// <inheritdoc />
    public string Name => "ScriptedTrackingSource";

    public ScriptedTrackingSource(
        ITrackingSubject subject,
        ILogger<ScriptedTrackingSource> logger,
        TimeSpan? eventInterval = null)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventInterval = eventInterval ?? TimeSpan.FromSeconds(1);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Replays the scripted event sequence for a demo tracking number.
    /// Publishes events at fixed intervals until the sequence completes or cancellation.
    /// </remarks>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{Source}] Starting scripted event replay...", Name);

        var events = BuildDemoSequence();

        foreach (var evt in events)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await Task.Delay(_eventInterval, ct);
                await _subject.PublishAsync(evt, ct);

                _logger.LogDebug(
                    "[{Source}] Published {EventType} for {TrackingNumber} — {Description}",
                    Name, evt.EventType, evt.TrackingNumber, evt.Description);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("[{Source}] Scripted replay completed.", Name);
    }

    /// <summary>
    /// Build a deterministic demo sequence simulating a Shanghai → Beijing shipment.
    /// Tracking number: SF0000000001 (a plausible SF Express number).
    /// </summary>
    private static List<TrackingEvent> BuildDemoSequence()
    {
        const string trackingNumber = "SF0000000001";
        var baseTime = DateTimeOffset.UtcNow;

        return
        [
            // Event 1: Package picked up in Shanghai
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.StatusChanged,
                ShipmentStatus.PickedUp,
                new GeoCoordinate(31.2304m, 121.4737m),
                "【上海市】快件已揽收，揽收员：张师傅",
                baseTime),

            // Event 2: Arrived at Shanghai sorting center
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.LocationUpdated,
                null,
                new GeoCoordinate(31.2500m, 121.5000m),
                "【上海市】快件已到达上海转运中心，正在分拣中",
                baseTime.AddMinutes(30)),

            // Event 3: Departed Shanghai, en route to Nanjing
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.StatusChanged,
                ShipmentStatus.InTransit,
                new GeoCoordinate(31.2500m, 121.5000m),
                "【上海市】快件已从上海转运中心发出，下一站：南京",
                baseTime.AddHours(1)),

            // Event 4: Arrived at Nanjing hub
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.LocationUpdated,
                null,
                new GeoCoordinate(32.0603m, 118.7969m),
                "【南京市】快件已到达南京分拨中心",
                baseTime.AddHours(4)),

            // Event 5: Departed Nanjing, en route to Jinan
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.LocationUpdated,
                null,
                new GeoCoordinate(32.0603m, 118.7969m),
                "【南京市】快件已从南京分拨中心发出，下一站：济南",
                baseTime.AddHours(5)),

            // Event 6: Arrived at Jinan node
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.LocationUpdated,
                null,
                new GeoCoordinate(36.6512m, 117.1201m),
                "【济南市】快件已到达济南节点",
                baseTime.AddHours(10)),

            // Event 7: Departed Jinan, en route to Tianjin
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.LocationUpdated,
                null,
                new GeoCoordinate(36.6512m, 117.1201m),
                "【济南市】快件已从济南节点发出，下一站：天津",
                baseTime.AddHours(11)),

            // Event 8: Arrived at Tianjin port
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.LocationUpdated,
                null,
                new GeoCoordinate(39.3434m, 117.3616m),
                "【天津市】快件已到达天津港中转站",
                baseTime.AddHours(15)),

            // Event 9: Arrived at Beijing sorting center
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.LocationUpdated,
                null,
                new GeoCoordinate(39.9042m, 116.4074m),
                "【北京市】快件已到达北京总仓，正在分拣中",
                baseTime.AddHours(17)),

            // Event 10: Out for delivery in Beijing
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.StatusChanged,
                ShipmentStatus.OutForDelivery,
                new GeoCoordinate(39.9142m, 116.3912m),
                "【北京市】快件正在派送中，派送员：李师傅，电话：138****1234",
                baseTime.AddHours(18)),

            // Event 11: Delivered
            new TrackingEvent(
                trackingNumber,
                TrackingEventType.StatusChanged,
                ShipmentStatus.Delivered,
                new GeoCoordinate(39.9200m, 116.3850m),
                "【北京市】快件已签收，签收人：本人签收。如有疑问请联系客服",
                baseTime.AddHours(19)),
        ];
    }
}
