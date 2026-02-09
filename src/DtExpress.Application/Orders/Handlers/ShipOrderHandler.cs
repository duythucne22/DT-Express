using DtExpress.Application.Common;
using DtExpress.Application.Orders.Commands;
using DtExpress.Application.Ports;
using DtExpress.Domain.Audit.Enums;
using DtExpress.Domain.Audit.Models;
using DtExpress.Domain.Carrier.Models;
using DtExpress.Domain.Common;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Application.Orders.Handlers;

/// <summary>
/// Handles ShipOrderCommand: calculates route via IRoutingPort,
/// books carrier via ICarrierPort, transitions Confirmed → Shipped,
/// persists, publishes events, and records audit.
/// </summary>
public sealed class ShipOrderHandler : ICommandHandler<ShipOrderCommand, BookingResult>
{
    private readonly IOrderRepository _repository;
    private readonly IRoutingPort _routing;
    private readonly ICarrierPort _carrier;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IAuditPort _audit;
    private readonly IClock _clock;

    public ShipOrderHandler(
        IOrderRepository repository,
        IRoutingPort routing,
        ICarrierPort carrier,
        IDomainEventPublisher eventPublisher,
        IAuditPort audit,
        IClock clock)
    {
        _repository = repository;
        _routing = routing;
        _carrier = carrier;
        _eventPublisher = eventPublisher;
        _audit = audit;
        _clock = clock;
    }

    public async Task<BookingResult> HandleAsync(ShipOrderCommand command, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, ct)
            ?? throw new DomainException("NOT_FOUND", $"Order {command.OrderId} not found.");

        var previousStatus = order.Status;

        // Calculate total weight from items
        var totalWeight = new Weight(
            order.Items.Sum(i => i.Weight.ToKilograms() * i.Quantity),
            WeightUnit.Kg);

        // 1. Route calculation via cross-domain port
        var routeRequest = new RouteRequest(
            Origin: new GeoCoordinate(31.2304m, 121.4737m),    // Simplified: use default coords
            Destination: new GeoCoordinate(39.9042m, 116.4074m),
            PackageWeight: totalWeight,
            ServiceLevel: order.ServiceLevel);

        await _routing.CalculateRouteAsync(routeRequest, ct);

        // 2. Book best carrier via cross-domain port
        var quoteRequest = new QuoteRequest(
            Origin: order.Origin,
            Destination: order.Destination,
            Weight: totalWeight,
            ServiceLevel: order.ServiceLevel);

        var bookingResult = await _carrier.BookBestAsync(quoteRequest, ct);

        // 3. Transition state: Confirmed → Shipped
        order.Apply(OrderAction.Ship, _clock.UtcNow);
        order.SetTrackingInfo(bookingResult.CarrierCode, bookingResult.TrackingNumber);

        await _repository.SaveAsync(order, ct);

        // Publish domain events
        foreach (var evt in order.DomainEvents)
        {
            await _eventPublisher.PublishAsync(evt, ct);
        }
        order.ClearDomainEvents();

        // Audit — state transition
        await _audit.RecordAsync(new AuditContext(
            EntityType: "Order",
            EntityId: command.OrderId.ToString(),
            Action: AuditAction.StateChanged,
            Category: AuditCategory.StateTransition,
            Description: $"State: {previousStatus} → {order.Status}"), ct);

        // Audit — carrier booking
        await _audit.RecordAsync(new AuditContext(
            EntityType: "Carrier",
            EntityId: bookingResult.TrackingNumber,
            Action: AuditAction.BusinessAction,
            Category: AuditCategory.ExternalCall,
            Description: $"Booked {bookingResult.CarrierCode}: {bookingResult.TrackingNumber}"), ct);

        return bookingResult;
    }
}
