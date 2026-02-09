using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Orders.Models;

/// <summary>
/// Aggregate Root: the Order entity.
/// Mutable class (not record) — state transitions mutate internal state.
/// Delegates actions to CurrentState via the State Pattern.
/// Collects domain events for post-save publishing.
/// </summary>
public sealed class Order
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; }
    public ContactInfo Customer { get; private set; }
    public Address Origin { get; private set; }
    public Address Destination { get; private set; }
    public IReadOnlyList<OrderItem> Items { get; private set; }
    public ServiceLevel ServiceLevel { get; private set; }

    // State Pattern: current state determines valid transitions
    public IOrderState CurrentState { get; private set; }
    public OrderStatus Status => CurrentState.Status;

    // Tracking info (set after carrier booking — Confirmed → Shipped)
    public string? TrackingNumber { get; private set; }
    public string? CarrierCode { get; private set; }

    // Domain events (collected during Apply, published after save)
    private readonly List<OrderDomainEvent> _domainEvents = new();
    public IReadOnlyList<OrderDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Timestamps
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Order(
        Guid id,
        string orderNumber,
        ContactInfo customer,
        Address origin,
        Address destination,
        IReadOnlyList<OrderItem> items,
        ServiceLevel serviceLevel,
        IOrderState initialState,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrderNumber = orderNumber;
        Customer = customer;
        Origin = origin;
        Destination = destination;
        Items = items;
        ServiceLevel = serviceLevel;
        CurrentState = initialState;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>
    /// Apply an action to the order. Delegates to CurrentState.Transition().
    /// Records a domain event capturing the state transition.
    /// </summary>
    public void Apply(OrderAction action, DateTimeOffset timestamp)
    {
        var previousStatus = Status;
        CurrentState = CurrentState.Transition(action, this);
        UpdatedAt = timestamp;
        _domainEvents.Add(new OrderDomainEvent(Id, previousStatus, Status, action, timestamp));
    }

    /// <summary>Set tracking info after successful carrier booking.</summary>
    public void SetTrackingInfo(string carrierCode, string trackingNumber)
    {
        CarrierCode = carrierCode;
        TrackingNumber = trackingNumber;
    }

    /// <summary>Clear collected domain events after publishing.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
