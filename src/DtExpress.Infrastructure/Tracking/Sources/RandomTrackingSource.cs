using DtExpress.Domain.Carrier.Enums;
using DtExpress.Domain.Tracking.Enums;
using DtExpress.Domain.Tracking.Interfaces;
using DtExpress.Domain.Tracking.Models;
using DtExpress.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DtExpress.Infrastructure.Tracking.Sources;

/// <summary>
/// Tracking source that generates random tracking events at intervals (2-10 seconds).
/// Useful for load testing and simulating a live logistics feed.
/// <para>
/// Each cycle picks a random tracking number from its watch list and emits either
/// a <see cref="TrackingEventType.StatusChanged"/> or <see cref="TrackingEventType.LocationUpdated"/>
/// event with realistic Chinese logistics descriptions.
/// </para>
/// </summary>
public sealed class RandomTrackingSource : ITrackingSource
{
    private readonly ITrackingSubject _subject;
    private readonly ILogger<RandomTrackingSource> _logger;

    /// <summary>Tracking numbers this source watches.</summary>
    private readonly List<string> _trackingNumbers = [];

    /// <inheritdoc />
    public string Name => "RandomTrackingSource";

    public RandomTrackingSource(ITrackingSubject subject, ILogger<RandomTrackingSource> logger)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Add a tracking number to the watch list.
    /// Events will be generated for these numbers when <see cref="StartAsync"/> is running.
    /// </summary>
    public void Watch(string trackingNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        _trackingNumbers.Add(trackingNumber);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Runs in an infinite loop until cancellation. Each iteration:
    /// 1. Waits 2-10 seconds (random)
    /// 2. Picks a random tracking number
    /// 3. Generates a random event (status change or location update)
    /// 4. Publishes to <see cref="ITrackingSubject"/>
    /// </remarks>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_trackingNumbers.Count == 0)
        {
            _logger.LogWarning("[{Source}] No tracking numbers to watch. Exiting.", Name);
            return;
        }

        _logger.LogInformation(
            "[{Source}] Started — watching {Count} tracking numbers",
            Name, _trackingNumbers.Count);

        var random = new Random();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Wait 2-10 seconds between events
                var delayMs = random.Next(2000, 10001);
                await Task.Delay(delayMs, ct);

                var trackingNumber = _trackingNumbers[random.Next(_trackingNumbers.Count)];
                var evt = GenerateRandomEvent(trackingNumber, random);

                await _subject.PublishAsync(evt, ct);

                _logger.LogDebug(
                    "[{Source}] Published {EventType} for {TrackingNumber}",
                    Name, evt.EventType, evt.TrackingNumber);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("[{Source}] Stopped.", Name);
    }

    /// <summary>Generate a random tracking event with Chinese logistics descriptions.</summary>
    private static TrackingEvent GenerateRandomEvent(string trackingNumber, Random random)
    {
        var isStatusChange = random.Next(2) == 0;

        if (isStatusChange)
        {
            var (status, description) = RandomStatusEvent(random);
            return new TrackingEvent(
                TrackingNumber: trackingNumber,
                EventType: TrackingEventType.StatusChanged,
                NewStatus: status,
                Location: RandomChineseLocation(random),
                Description: description,
                OccurredAt: DateTimeOffset.UtcNow);
        }

        return new TrackingEvent(
            TrackingNumber: trackingNumber,
            EventType: TrackingEventType.LocationUpdated,
            NewStatus: null,
            Location: RandomChineseLocation(random),
            Description: RandomLocationDescription(random),
            OccurredAt: DateTimeOffset.UtcNow);
    }

    /// <summary>Pick a random status change with Chinese description.</summary>
    private static (ShipmentStatus Status, string Description) RandomStatusEvent(Random random)
    {
        var events = new (ShipmentStatus, string)[]
        {
            (ShipmentStatus.PickedUp,       "快件已揽收，等待发往下一站"),
            (ShipmentStatus.InTransit,      "快件已到达中转站，正在分拣中"),
            (ShipmentStatus.InTransit,      "快件已从中转站发出，运往下一站"),
            (ShipmentStatus.OutForDelivery, "快件正在派送中，请保持电话畅通"),
            (ShipmentStatus.Delivered,      "快件已签收，签收人：本人签收"),
        };

        return events[random.Next(events.Length)];
    }

    /// <summary>Random Chinese logistics hub coordinate.</summary>
    private static GeoCoordinate RandomChineseLocation(Random random)
    {
        // Bounded within China's logistics corridors
        var lat = 22.5m + (decimal)(random.NextDouble() * 18.0);  // 22.5°N - 40.5°N
        var lng = 104.0m + (decimal)(random.NextDouble() * 18.0); // 104°E - 122°E
        return new GeoCoordinate(Math.Round(lat, 4), Math.Round(lng, 4));
    }

    /// <summary>Random location update description in Chinese.</summary>
    private static string RandomLocationDescription(Random random)
    {
        var descriptions = new[]
        {
            "快件已到达上海转运中心",
            "快件已到达北京分拣中心",
            "快件已到达广州集散中心",
            "快件已到达武汉中转站",
            "快件已到达成都枢纽",
            "快件已扫描，在途中",
            "快件已到达深圳站点",
            "快件已到达杭州集散中心",
        };

        return descriptions[random.Next(descriptions.Length)];
    }
}
