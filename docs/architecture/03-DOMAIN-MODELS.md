# 📊 03-DOMAIN-MODELS — Value Objects, Records, Enums

> **Layer**: Domain (all types below live in `DtExpress.Domain`)  
> **Rule**: All models are immutable `record` types (C# 12)  
> **Rule**: Value objects enforce invariants in constructors  
> **Rule**: Enums are simple — no string-based enum hacks

---

## 📋 Table of Contents

1. [Value Objects (Shared)](#value-objects-shared)
2. [Routing Models (01)](#routing-models-01)
3. [Carrier Models (02)](#carrier-models-02)
4. [Tracking Models (03)](#tracking-models-03)
5. [Order Models (04)](#order-models-04)
6. [Audit Models (05)](#audit-models-05)
7. [Enum Catalog](#enum-catalog)
8. [Usage Matrix](#usage-matrix)

---

## Value Objects (Shared)

> Namespace: `DtExpress.Domain.ValueObjects`  
> All value objects: `sealed record`, equality by value, invariants in constructor.

### `Address`
```csharp
public sealed record Address
{
    public string Street { get; init; }
    public string City { get; init; }
    public string Province { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }

    public Address(string street, string city, string province, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City required");
        if (string.IsNullOrWhiteSpace(postalCode)) throw new ArgumentException("PostalCode required");
        Street = street;
        City = city;
        Province = province;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>Short display: "City, Province PostalCode"</summary>
    public string ToShortString() => $"{City}, {Province} {PostalCode}";
}
```

### `GeoCoordinate`
```csharp
public sealed record GeoCoordinate
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }

    public GeoCoordinate(decimal latitude, decimal longitude)
    {
        if (latitude < -90 || latitude > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude < -180 || longitude > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Haversine distance in kilometers (simplified).</summary>
    public decimal DistanceToKm(GeoCoordinate other)
    {
        // Simplified flat-earth approximation for demo
        var dLat = (other.Latitude - Latitude) * 111.32m;
        var dLng = (other.Longitude - Longitude) * 111.32m * (decimal)Math.Cos((double)Latitude * Math.PI / 180);
        return (decimal)Math.Sqrt((double)(dLat * dLat + dLng * dLng));
    }
}
```

### `Money`
```csharp
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required");
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money CNY(decimal amount) => new(amount, "CNY");
    public static Money USD(decimal amount) => new(amount, "USD");

    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new InvalidOperationException("Currency mismatch");
        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
```

### `Weight`
```csharp
public sealed record Weight
{
    public decimal Value { get; init; }
    public WeightUnit Unit { get; init; }

    public Weight(decimal value, WeightUnit unit)
    {
        if (value <= 0) throw new ArgumentException("Weight must be positive");
        Value = value;
        Unit = unit;
    }

    public static Weight Kilograms(decimal kg) => new(kg, WeightUnit.Kg);
    public static Weight Grams(decimal g) => new(g, WeightUnit.G);

    public decimal ToKilograms() => Unit switch
    {
        WeightUnit.Kg => Value,
        WeightUnit.G => Value / 1000m,
        WeightUnit.Lb => Value * 0.453592m,
        _ => throw new InvalidOperationException($"Unknown unit: {Unit}")
    };
}
```

### `Dimension`
```csharp
public sealed record Dimension(decimal LengthCm, decimal WidthCm, decimal HeightCm)
{
    public decimal VolumeCm3 => LengthCm * WidthCm * HeightCm;

    /// <summary>Volumetric weight (standard factor: 5000 for air, 6000 for ground).</summary>
    public Weight VolumetricWeight(int factor = 5000)
        => Weight.Kilograms(VolumeCm3 / factor);
}
```

### `ContactInfo`
```csharp
public sealed record ContactInfo(string Name, string Phone, string? Email = null)
{
    // Invariants
    public ContactInfo(string name, string phone, string? email = null) : this(name, phone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required");
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone required");
        Name = name;
        Phone = phone;
        Email = email;
    }
}
```

### `TrackingNumber`
```csharp
public sealed record TrackingNumber
{
    public string Value { get; init; }

    public TrackingNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Tracking number required");
        if (value.Length < 6) throw new ArgumentException("Tracking number too short");
        Value = value;
    }

    public override string ToString() => Value;
    public static implicit operator string(TrackingNumber tn) => tn.Value;
}
```

---

## Routing Models (01)

> Namespace: `DtExpress.Domain.Routing.Models`

### `RouteRequest`
```csharp
public sealed record RouteRequest(
    GeoCoordinate Origin,
    GeoCoordinate Destination,
    Weight PackageWeight,
    ServiceLevel ServiceLevel);
```

### `Route`
```csharp
public sealed record Route(
    string StrategyUsed,
    IReadOnlyList<string> WaypointNodeIds,
    decimal DistanceKm,
    TimeSpan EstimatedDuration,
    Money EstimatedCost);
```

### `Graph`
```csharp
public sealed class Graph
{
    public IReadOnlyDictionary<string, GraphNode> Nodes { get; }
    public IReadOnlyList<GraphEdge> Edges { get; }

    public Graph(IReadOnlyDictionary<string, GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    /// <summary>Get outgoing edges from a node.</summary>
    public IEnumerable<GraphEdge> GetEdgesFrom(string nodeId)
        => Edges.Where(e => e.FromNodeId == nodeId);
}
```

### `GraphNode`
```csharp
public sealed record GraphNode(string Id, string Name, GeoCoordinate Coordinate);
```

### `GraphEdge`
```csharp
public sealed record GraphEdge(
    string FromNodeId,
    string ToNodeId,
    decimal DistanceKm,
    TimeSpan Duration,
    Money Cost);
```

### `PathResult`
```csharp
public sealed record PathResult(
    IReadOnlyList<string> NodeIds,
    decimal TotalDistanceKm,
    TimeSpan TotalDuration,
    Money TotalCost);
```

---

## Carrier Models (02)

> Namespace: `DtExpress.Domain.Carrier.Models`

### `QuoteRequest`
```csharp
public sealed record QuoteRequest(
    Address Origin,
    Address Destination,
    Weight Weight,
    ServiceLevel ServiceLevel);
```

### `CarrierQuote`
```csharp
public sealed record CarrierQuote(
    string CarrierCode,
    Money Price,
    int EstimatedDays,
    ServiceLevel ServiceLevel);
```

### `BookingRequest`
```csharp
public sealed record BookingRequest(
    string CarrierCode,
    Address Origin,
    Address Destination,
    Weight Weight,
    ContactInfo Sender,
    ContactInfo Recipient,
    ServiceLevel ServiceLevel);
```

### `BookingResult`
```csharp
public sealed record BookingResult(
    string CarrierCode,
    string TrackingNumber,
    DateTimeOffset BookedAt);
```

### `CarrierTrackingInfo`
```csharp
public sealed record CarrierTrackingInfo(
    string TrackingNumber,
    ShipmentStatus Status,
    string? CurrentLocation,
    DateTimeOffset UpdatedAt);
```

---

## Tracking Models (03)

> Namespace: `DtExpress.Domain.Tracking.Models`

### `TrackingEvent`
```csharp
public sealed record TrackingEvent(
    string TrackingNumber,
    TrackingEventType EventType,
    ShipmentStatus? NewStatus,
    GeoCoordinate? Location,
    string? Description,
    DateTimeOffset OccurredAt);
```

### `TrackingSnapshot`
```csharp
public sealed record TrackingSnapshot(
    string TrackingNumber,
    ShipmentStatus CurrentStatus,
    GeoCoordinate? LastLocation,
    DateTimeOffset UpdatedAt);
```

---

## Order Models (04)

> Namespace: `DtExpress.Domain.Orders.Models`

### `Order` (Aggregate Root — mutable class, not record)
```csharp
public sealed class Order
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; }
    public ContactInfo Customer { get; private set; }
    public Address Origin { get; private set; }
    public Address Destination { get; private set; }
    public IReadOnlyList<OrderItem> Items { get; private set; }
    public ServiceLevel ServiceLevel { get; private set; }

    // State pattern: current state
    public IOrderState CurrentState { get; private set; }
    public OrderStatus Status => CurrentState.Status;

    // Tracking info (set after shipping)
    public string? TrackingNumber { get; private set; }
    public string? CarrierCode { get; private set; }

    // Domain events (collected, published after save)
    private readonly List<OrderDomainEvent> _domainEvents = new();
    public IReadOnlyList<OrderDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Timestamps
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Order(
        Guid id, string orderNumber,
        ContactInfo customer, Address origin, Address destination,
        IReadOnlyList<OrderItem> items, ServiceLevel serviceLevel,
        IOrderState initialState, DateTimeOffset createdAt)
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

    /// <summary>Apply an action. Delegates to current state. Emits domain event.</summary>
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
```

### `OrderItem`
```csharp
public sealed record OrderItem(
    string Description,
    int Quantity,
    Weight Weight,
    Dimension? Dimensions);
```

### `OrderDetail` (Read Model)
```csharp
public sealed record OrderDetail(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string Origin,
    string Destination,
    OrderStatus Status,
    ServiceLevel ServiceLevel,
    string? TrackingNumber,
    string? CarrierCode,
    IReadOnlyList<OrderItem> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

### `OrderSummary` (Read Model)
```csharp
public sealed record OrderSummary(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    OrderStatus Status,
    ServiceLevel ServiceLevel,
    DateTimeOffset CreatedAt);
```

### `OrderFilter`
```csharp
public sealed record OrderFilter(
    OrderStatus? Status = null,
    ServiceLevel? ServiceLevel = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
```

### `OrderDomainEvent`
```csharp
public sealed record OrderDomainEvent(
    Guid OrderId,
    OrderStatus PreviousStatus,
    OrderStatus NewStatus,
    OrderAction Action,
    DateTimeOffset OccurredAt);
```

---

## Audit Models (05)

> Namespace: `DtExpress.Domain.Audit.Models`

### `AuditRecord`
```csharp
public sealed record AuditRecord(
    string Id,
    string EntityType,
    string EntityId,
    AuditAction Action,
    AuditCategory Category,
    string Actor,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string? Description,
    Dictionary<string, object?>? Payload);
```

### `AuditContext`
```csharp
public sealed record AuditContext(
    string EntityType,
    string EntityId,
    AuditAction Action,
    AuditCategory Category,
    string? Description = null,
    Dictionary<string, object?>? Before = null,
    Dictionary<string, object?>? After = null);
```

---

## Enum Catalog

### Shared Enums

```csharp
// DtExpress.Domain.Routing.Enums
public enum ServiceLevel { Express, Standard, Economy }
public enum RouteOptimization { Fastest, Cheapest, Balanced }

// DtExpress.Domain.Carrier.Enums
public enum ShipmentStatus
{
    Created,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    Exception
}

// DtExpress.Domain.Tracking.Enums
public enum TrackingEventType { StatusChanged, LocationUpdated }

// DtExpress.Domain.Orders.Enums
public enum OrderStatus { Created, Confirmed, Shipped, Delivered, Cancelled }
public enum OrderAction { Confirm, Ship, Deliver, Cancel }

// DtExpress.Domain.Audit.Enums
public enum AuditAction { Created, Updated, Deleted, StateChanged, BusinessAction }
public enum AuditCategory { DataChange, StateTransition, ExternalCall, BusinessDecision }

// DtExpress.Domain.ValueObjects
public enum WeightUnit { Kg, G, Lb }
```

---

## Usage Matrix

| Model | Used By Domain | Used By Layer |
|-------|---------------|---------------|
| `Address` | Carrier, Orders | Domain, Application |
| `GeoCoordinate` | Routing, Tracking | Domain, Infrastructure |
| `Money` | Routing, Carrier | Domain, Application |
| `Weight` | Routing, Carrier, Orders | Domain, Application |
| `ContactInfo` | Carrier, Orders | Domain, Application |
| `TrackingNumber` | Carrier, Tracking, Orders | Domain, Application |
| `ServiceLevel` | All 5 domains | All layers |
| `ShipmentStatus` | Carrier, Tracking | Domain, Infrastructure |
| `OrderStatus` | Orders, Audit | Domain, Application, Infrastructure |
| `AuditRecord` | Audit (consumed by all) | All layers |

### Cross-Domain Enum Sharing

> `ServiceLevel` and `ShipmentStatus` are shared across domains. They live in the domain layer where they are most naturally owned:
> - `ServiceLevel` → `DtExpress.Domain.Routing.Enums` (used by Routing, Carrier, Orders)
> - `ShipmentStatus` → `DtExpress.Domain.Carrier.Enums` (used by Carrier, Tracking)
> 
> This is acceptable because all domains are in the same `DtExpress.Domain` project. In a microservice split, these would move to a shared kernel.

---

## Domain Exception

```csharp
namespace DtExpress.Domain.Common;

/// <summary>Base exception for domain rule violations.</summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
```

### Derived Exceptions

```csharp
public class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(OrderStatus from, OrderAction action)
        : base("INVALID_TRANSITION", $"Cannot {action} from {from} state") { }
}

public class CarrierNotFoundException : DomainException
{
    public CarrierNotFoundException(string carrierCode)
        : base("CARRIER_NOT_FOUND", $"No adapter registered for carrier: {carrierCode}") { }
}

public class StrategyNotFoundException : DomainException
{
    public StrategyNotFoundException(string strategyName)
        : base("STRATEGY_NOT_FOUND", $"No strategy registered with name: {strategyName}") { }
}
```
