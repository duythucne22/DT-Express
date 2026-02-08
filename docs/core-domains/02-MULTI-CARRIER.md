# 🚚 02-MULTI-CARRIER — Multi-Pattern Design Spec

> **Domain**: Multi-Carrier Integration — Carrier “CMS slice” inside a TMS  
> **Patterns**: Adapter · Factory · (Optional) Strategy · (Optional) Decorator  
> **Goal**: Interview-ready, enterprise-grade boundaries — small scope, clean layering, SOLID-friendly  
> **Dependencies**: [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md) (upstream may provide route/constraints)

---

## 📋 Table of Contents

1. [Domain Overview](#-domain-overview)
2. [Business Context](#-business-context)
3. [Architecture Layers](#-architecture-layers)
4. [Feature Specification](#-feature-specification)
5. [Design Pattern Application](#-design-pattern-application)
6. [Interface Contracts](#-interface-contracts)
7. [Data Models](#-data-models)
8. [Adapter Layer](#-adapter-layer)
9. [Infrastructure Layer](#-infrastructure-layer)
10. [Acceptance Criteria](#-acceptance-criteria)
11. [Project Structure](#-project-structure)
12. [Interview Guide](#-interview-guide)
13. [Study Resources](#-study-resources)
14. [Related Documents](#-related-documents)

---

## 🎯 Domain Overview

### Elevator Pitch

> “A multi-carrier module that hides heterogeneous carrier contracts behind a unified interface.  
> **Adapter** normalizes JSON/XML-shaped carrier APIs into a single model.  
> **Factory** resolves adapters by carrier code without `switch` sprawl.  
> Optional **Strategy** selects the best quote; optional **Decorator** adds caching/logging.  
> Clean boundaries make it easy to add carriers and maintain the system.”

### Purpose

Given a shipment intent (origin, destination, weight, service level), the system must:

- **Quote**: ask each supported carrier for a rate
- **Book**: create a shipment with the selected carrier
- **Track**: query shipment status using the carrier tracking number

### The Key Insight

> **Adapter is mapping** (carrier contract → domain model).  
> **Selection is policy** (cheapest vs fastest) → keep it separate (Strategy).  
> **Caching/logging is cross-cutting** → keep it separate (Decorator).

### Scope

| In Scope | Out of Scope |
|----------|--------------|
| Exactly **2 mock carriers** with different data shapes (JSON vs XML) | Real carrier integrations, auth/signatures, retries, SLAs |
| Unified interface: quote + book + track | Full lifecycle: returns, settlement, claims |
| Factory registry (DI enumerable → dictionary) | Platform comparisons / onboarding frameworks |
| Optional Strategy for choosing best quote | Resilience engineering (circuit breakers, etc.) |
| Optional Decorator for caching quote results | Real HTTP / persistence requirements |

---

## 💼 Business Context

### User Story

> **As a** shipping operator,  
> **I want to** compare carriers, book one, and track status,  
> **So that** I can fulfill shipments without my code depending on any single carrier’s data format.

### Cross-domain boundaries (TMS system view)

- **Routing** ([01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md)) decides *route plan / constraints* (distance estimate, zones, “avoid tolls”, etc.).
- **Multi-Carrier** (this doc) decides *carrier interaction*: quote → book → track.
- **Realtime Tracking** (03) handles continuous updates / streaming (out of scope here).
- **Order Processing** (04) orchestrates order → shipment creation (out of scope here).

---

## 🏗 Architecture Layers

### The Separation Principle

```
┌────────────────────────────────────────────────────────────────┐
│                      CLEAN ARCHITECTURE                        │
├────────────────────────────────────────────────────────────────┤
│   ┌───────────────────────────────────────────────────────┐    │
│   │  CORE LAYER (pure C#, zero dependencies)              │    │
│   │                                                       │
│   │  • Contracts: ICarrierAdapter, ICarrierAdapterFactory │
│   │  • Optional: ICarrierSelectionStrategy                │
│   │  • Domain models: RateRequest, CarrierQuote, ...      │
│   │  • No JSON/XML details here                           │
│   └───────────────────────┬───────────────────────────────┘    │
│                           │ depends on                         │
│   ┌───────────────────────▼───────────────────────────────┐    │
│   │  ADAPTER LAYER (mapping, carrier-specific)            │    │
│   │                                                       │
│   │  • JsonShipAdapter  (pretends carrier speaks JSON)    │
│   │  • XmlPostAdapter   (pretends carrier speaks XML)     │
│   │  • Mapping DTOs ↔ domain models                       │
│   └───────────────────────┬───────────────────────────────┘    │
│                           │ wrapped by / composed in            │
│   ┌───────────────────────▼───────────────────────────────┐    │
│   │  INFRASTRUCTURE LAYER (cross-cutting, utilities)      │    │
│   │                                                       │
│   │  • Decorators: caching/logging around adapters        │
│   │  • (Optional) stub transport client                   │
│   └───────────────────────┬───────────────────────────────┘    │
│                           │ used by                            │
│   ┌───────────────────────▼───────────────────────────────┐    │
│   │  DEMO LAYER (composition root)                        │    │
│   │                                                       │
│   │  • Factory wiring + demo scenario                     │
│   │  • quote → select → book → track                      │
│   └───────────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────────────┘
```

### Why this layering matters

| Layer | What goes here | What does NOT go here |
|-------|---------------|----------------------|
| **Core** | Interfaces + domain models + selection policy | JSON/XML parsing, HTTP, caching |
| **Adapter** | Carrier DTO mapping | Business branching across carriers |
| **Infrastructure** | Decorators, utilities | Carrier contract details |
| **Demo** | Wiring + scenario | Business logic, mapping logic |

---

## 📝 Feature Specification

Three power features — consistent with 01’s structure.

| # | Feature | Pattern Demonstrated | Interview Value |
|---|---------|---------------------|----------------|
| **F1** | Unified carrier interface with mapping | Adapter | “Integration without coupling” |
| **F2** | Registry-based adapter resolution | Factory | Open/Closed carrier onboarding |
| **F3** | Cross-cutting concerns + selection policy | Decorator + Strategy (optional) | Clean composition, SRP |

### F1 — Adapter Pattern (MUST HAVE)

**Description**: Two mock carriers expose different shapes (JSON vs XML). Adapters map them into the unified domain models.

**Acceptance**:
- [ ] Client code uses only `ICarrierAdapter` methods
- [ ] Carrier JSON/XML DTOs are not visible outside adapter layer
- [ ] Adding a new carrier means adding a new adapter class, not modifying callers

### F2 — Factory Pattern (MUST HAVE)

**Description**: The factory is the single place that resolves adapters by carrier code using a registry (dictionary) built from DI.

**Acceptance**:
- [ ] Factory is registry-based (DI enumerable → dictionary)
- [ ] Resolution is case-insensitive
- [ ] Unsupported carrier code throws a single, explicit exception

### F3 — Optional: Strategy + Decorator (INTERVIEW POLISH)

**Description**:
- Strategy selects “best quote” based on policy (cheapest vs fastest)
- Decorator adds caching/logging around the adapter without touching mapping code

**Acceptance**:
- [ ] Strategy is pluggable without changing adapters
- [ ] Decorator implements the same interface and wraps an inner adapter

### Demo flow: quote → select → book → track

A single end-to-end flow is enough to demonstrate all patterns without bloating scope:

1. Create `RateRequest`
2. Ask **all adapters** for quotes
3. Strategy selects one quote (optional; can default to cheapest)
4. Book with the chosen adapter
5. Track with the chosen adapter

Pseudo-flow:

```csharp
var quotes = await Task.WhenAll(factory.GetAll().Select(a => a.GetRateAsync(request)));
var best = strategy.SelectBest(quotes);
var booking = await factory.Get(best.CarrierCode).BookAsync(new BookingRequest(...));
var tracking = await factory.Get(best.CarrierCode).TrackAsync(booking.TrackingNumber);
```

---

## 🎨 Design Pattern Application

### Pattern collaboration diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        PATTERN COLLABORATION                         │
├─────────────────────────────────────────────────────────────────────┤
│  FACTORY PATTERN (resolves)            ADAPTER PATTERN (maps)        │
│  ─────────────────────────            ───────────────────────────    │
│  ┌──────────────────────────┐         ┌──────────────────────────┐   │
│  │ CarrierAdapterFactory     │────────>│ «interface»              │   │
│  │ + Get(code)               │ returns │ ICarrierAdapter          │   │
│  │ + GetAll()                │         ├──────────────────────────┤   │
│  └──────────────────────────┘         │ + GetRateAsync(...)      │   │
│                                       │ + BookAsync(...)         │   │
│                                       │ + TrackAsync(...)        │   │
│                                       └───────────┬──────────────┘   │
│                                                   │ implements        │
│                                 ┌─────────────────┼────────────────┐  │
│                                 ▼                 ▼                ▼  │
│                        ┌────────────────┐  ┌────────────────┐  (new) │
│                        │ JsonShipAdapter │  │ XmlPostAdapter  │  ...  │
│                        └────────────────┘  └────────────────┘       │
│                                                                       │
│  OPTIONAL STRATEGY (policy)             OPTIONAL DECORATOR (cross-cut)│
│  ─────────────────────────             ───────────────────────────── │
│  ┌──────────────────────────┐          ┌──────────────────────────┐  │
│  │ ICarrierSelectionStrategy │          │ CachingAdapterDecorator  │  │
│  │ + SelectBest(quotes)      │          │ wraps ICarrierAdapter    │  │
│  └──────────────────────────┘          └──────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

### Pattern roles summary

| Pattern | Role here | What it enables |
|---------|----------|-----------------|
| **Adapter** | Normalizes carrier contract differences | Core stays stable while carriers vary |
| **Factory** | Centralizes adapter resolution | No scattered `switch` / branching |
| **Strategy (optional)** | Selection policy among quotes | Swap selection logic at runtime |
| **Decorator (optional)** | Cache/log around adapters | Cross-cutting concerns without pollution |

---

## 📜 Interface Contracts

### Core layer

#### ICarrierAdapter — Unified integration contract

```csharp
public interface ICarrierAdapter
{
    string CarrierCode { get; } // e.g. "JSON" or "XML"

    Task<CarrierQuote> GetRateAsync(RateRequest request, CancellationToken ct = default);
    Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default);
    Task<TrackingInfo> TrackAsync(string trackingNumber, CancellationToken ct = default);
}
```

#### ICarrierAdapterFactory — Registry resolver

```csharp
public interface ICarrierAdapterFactory
{
    ICarrierAdapter Get(string carrierCode);
    IReadOnlyCollection<ICarrierAdapter> GetAll();
}
```

#### Optional: ICarrierSelectionStrategy — Selection policy

```csharp
public interface ICarrierSelectionStrategy
{
    string Name { get; }
    CarrierQuote SelectBest(IEnumerable<CarrierQuote> quotes);
}
```

---

## 📊 Data Models

Keep models intentionally small so the patterns stay visible.

```csharp
public sealed record RateRequest(
    string Origin,
    string Destination,
    decimal WeightKg,
    ServiceLevel ServiceLevel);

public sealed record CarrierQuote(
    string CarrierCode,
    decimal Cost,
    DateTimeOffset EstimatedDelivery);

public sealed record BookingRequest(
    string CarrierCode,
    string ShipmentId,
    string Origin,
    string Destination,
    decimal WeightKg);

public sealed record BookingResult(
    string CarrierCode,
    string TrackingNumber);

public sealed record TrackingInfo(
    string TrackingNumber,
    ShipmentStatus Status,
    DateTimeOffset UpdatedAt);

public enum ServiceLevel { Economy, Standard, Express }
public enum ShipmentStatus { Created, InTransit, Delivered, Exception }
```

---

## 🧩 Adapter Layer

### Why 2 mock carriers (JSON vs XML)

We want the adapter to do *real* normalization work. Two carriers are enough to show the contrast:

- **Carrier A**: JSON-shaped contract
- **Carrier B**: XML-shaped contract

No real HTTP is required for the learning project — represent the “carrier contract” as internal DTOs plus mapping.

### Mock carrier contracts (examples)

**Carrier A: JSON-shaped**

```json
{
  "from": "SGN",
  "to": "HAN",
  "kg": 1.5,
  "service": "Express"
}
```

**Carrier B: XML-shaped**

```xml
<RateRequest>
  <Origin>SGN</Origin>
  <Destination>HAN</Destination>
  <WeightKg>1.5</WeightKg>
  <Service>Express</Service>
</RateRequest>
```

### Mapping rule

> Adapters map `RateRequest` → carrier DTO → carrier response DTO → `CarrierQuote`.

Core stays clean: the rest of the system never sees the JSON/XML shapes.

---

## 🛡 Infrastructure Layer

### Factory: registry-based (no switch)

The factory is the single place that knows “carrier code → adapter instance”.

```csharp
public sealed class CarrierAdapterFactory : ICarrierAdapterFactory
{
    private readonly IReadOnlyDictionary<string, ICarrierAdapter> _map;

    public CarrierAdapterFactory(IEnumerable<ICarrierAdapter> adapters)
    {
        _map = adapters.ToDictionary(a => a.CarrierCode, a => a,
            StringComparer.OrdinalIgnoreCase);
    }

    public ICarrierAdapter Get(string carrierCode)
        => _map.TryGetValue(carrierCode, out var adapter)
            ? adapter
            : throw new CarrierNotSupportedException(carrierCode);

    public IReadOnlyCollection<ICarrierAdapter> GetAll() => _map.Values.ToList();
}
```

Exception is intentionally simple:

```csharp
public sealed class CarrierNotSupportedException : Exception
{
    public CarrierNotSupportedException(string carrierCode)
        : base($"Carrier '{carrierCode}' is not supported.") { }
}
```

### Optional: Decorator (in-memory cache for quotes)

Smallest cross-cutting example that still demonstrates composition:

- Cache only `GetRateAsync`
- TTL: 5 minutes
- Storage: `Dictionary<string, (expires, quote)>`

```csharp
public sealed class CachingCarrierAdapterDecorator : ICarrierAdapter
{
    private readonly ICarrierAdapter _inner;
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, (DateTimeOffset Expires, CarrierQuote Quote)> _cache = new();

    public string CarrierCode => _inner.CarrierCode;

    public CachingCarrierAdapterDecorator(ICarrierAdapter inner) => _inner = inner;

    public async Task<CarrierQuote> GetRateAsync(RateRequest request, CancellationToken ct = default)
    {
        var key = $"{CarrierCode}:{request.Origin}:{request.Destination}:{request.WeightKg}:{request.ServiceLevel}";
        if (_cache.TryGetValue(key, out var entry) && entry.Expires > DateTimeOffset.UtcNow)
            return entry.Quote;

        var quote = await _inner.GetRateAsync(request, ct);
        _cache[key] = (DateTimeOffset.UtcNow.Add(_ttl), quote);
        return quote;
    }

    public Task<BookingResult> BookAsync(BookingRequest request, CancellationToken ct = default)
        => _inner.BookAsync(request, ct);

    public Task<TrackingInfo> TrackAsync(string trackingNumber, CancellationToken ct = default)
        => _inner.TrackAsync(trackingNumber, ct);
}
```

### Optional: Strategy (pick cheapest vs fastest)

Keep it tiny and policy-only:

- `CheapestStrategy`: lowest `Cost`
- `FastestStrategy`: earliest `EstimatedDelivery`

---

## ✅ Acceptance Criteria

- Exactly **2 mock carriers** exist (one “JSON”, one “XML”) and both implement `ICarrierAdapter`.
- Client workflow uses only the unified interface + factory (no carrier-specific branching in calling code).
- Factory is **registry-based** (DI enumerable → dictionary), not `switch`.
- Demo scenario is possible end-to-end: **quote → select → book → track** using mocks.
- Optional patterns (Strategy/Decorator) remain optional and do not bloat the core.

### Testing checklist (what to actually test)

Keep tests aligned with patterns:

- Adapter tests
  - Maps unified request → carrier DTO correctly (JSON vs XML shape)
  - Maps carrier response → `CarrierQuote` correctly
- Factory tests
  - Resolves by code (case-insensitive)
  - Throws for unsupported code
- Strategy tests (optional)
  - Cheapest picks lowest cost
  - Fastest picks earliest delivery
- Decorator tests (optional)
  - Second call returns cached quote within TTL

---

## 🧱 Project Structure

Mirror 01’s “layers” naming, but smaller.

```
src/
  DTExpress.MultiCarrier.Core/
    Contracts/
      ICarrierAdapter.cs
      ICarrierAdapterFactory.cs
      ICarrierSelectionStrategy.cs   (optional)
    Models/
      RateRequest.cs
      CarrierQuote.cs
      BookingRequest.cs
      BookingResult.cs
      TrackingInfo.cs
  DTExpress.MultiCarrier.Adapters/
    JsonShip/
      JsonShipAdapter.cs
      JsonDtos.cs
    XmlPost/
      XmlPostAdapter.cs
      XmlDtos.cs
  DTExpress.MultiCarrier.Infrastructure/
    CarrierAdapterFactory.cs
    CachingCarrierAdapterDecorator.cs (optional)
  DTExpress.MultiCarrier.Demo/
    Program.cs
tests/
  DTExpress.MultiCarrier.Tests/
    AdapterMappingTests.cs
    FactoryTests.cs
    SelectionStrategyTests.cs         (optional)
    CachingDecoratorTests.cs          (optional)
```

---

## 🧠 Interview Guide

### 30–60 second talk-track

> “I modeled multi-carrier integration as a small, testable module with clean boundaries.  
> Adapters normalize different carrier contracts (I used JSON vs XML to make the mapping concrete).  
> A registry-based factory resolves adapters by carrier code so the rest of the system stays open/closed.  
> Selection is an optional Strategy, and caching is an optional Decorator — both added through composition without coupling core workflow to implementation details.”

### Common interview questions (with the “why”)

- **Why Adapter?** Carrier contracts change independently; Adapter prevents contract churn leaking into domain code.
- **Why Factory?** Centralizes creation/resolution and avoids branching duplication.
- **Why not put selection into adapters?** Adapters map data; selection is policy (SRP).
- **How do you add a new carrier?** Implement `ICarrierAdapter`, register it; nothing else changes.

---

## 📚 Study Resources

- Adapter and Factory patterns: see pattern docs under `docs/design-patterns/`.
- Practice: add a third mock carrier with a different shape (CSV-like or weird field names) and verify callers remain unchanged.

---

## 🔗 Related Documents

- [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md)
- [03-REALTIME-TRACKING.md](03-REALTIME-TRACKING.md)
- [04-ORDER-PROCESSING.md](04-ORDER-PROCESSING.md)
- [ADAPTER-PATTERN.md](../design-patterns/ADAPTER-PATTERN.md)
- [FACTORY-PATTERN.md](../design-patterns/FACTORY-PATTERN.md)
- [STRATEGY-PATTERN.md](../design-patterns/STRATEGY-PATTERN.md)
- [DECORATOR-PATTERN.md](../design-patterns/DECORATOR-PATTERN.md)
