# 📡 03-REALTIME-TRACKING — Multi-Pattern Design Spec

> **Domain**: Realtime Tracking — shipment status + location updates (TMS slice)
> **Patterns**: Observer · Dependency Inversion · (Optional) Adapter (SignalR) · (Optional) Decorator (rate-limit)
> **Goal**: Learning-sized, interview-ready design that still feels “enterprise-grade” in boundaries (SOLID, interfaces, composable)
> **Dependencies**: [02-MULTI-CARRIER.md](02-MULTI-CARRIER.md) (provides tracking numbers)

---

## 📋 Table of Contents

1. [Domain Overview](#-domain-overview)
2. [Business Context](#-business-context)
3. [Architecture Layers](#-architecture-layers)
4. [Feature Specification](#-feature-specification)
5. [Design Pattern Application](#-design-pattern-application)
6. [Interface Contracts](#-interface-contracts)
7. [Data Models](#-data-models)
8. [Infrastructure Layer](#-infrastructure-layer)
9. [Acceptance Criteria](#-acceptance-criteria)
10. [Project Structure](#-project-structure)
11. [Interview Guide](#-interview-guide)
12. [Study Resources](#-study-resources)
13. [Related Documents](#-related-documents)

---

## 🎯 Domain Overview

### Elevator Pitch

> “A small realtime tracking module where producers (GPS/status sources) publish tracking events, and subscribers (UI, notifications, internal workflows) receive updates via the **Observer Pattern**. The core is pure and testable. A thin adapter can push updates to clients via SignalR without leaking SignalR concerns into domain logic.”

### Purpose

- Provide *current* shipment state (status + last known location)
- Push updates to interested subscribers (customers / operators / other services)
- Keep tracking concerns decoupled from carriers and transport (no carrier-specific webhook details in this learning edition)

### The Key Insight

> Realtime tracking is not “SignalR code”.
>
> Realtime tracking is **event distribution**.
>
> SignalR is just one *delivery adapter* (like “how observers receive updates”), not the business core.

### Scope

| In Scope | Out of Scope (explicitly removed) |
|----------|-----------------------------------|
| Observer-based subscription + notification | Real carrier webhook integrations, signatures, auth |
| Minimal push concept (SignalR optional) | Carrier-specific status normalization matrices |
| Mock event generators (repeatable demos/tests) | Map vendor integrations (Amap/Baidu/Tencent/Google) |
| Simple state projection: “latest status/location” | Enterprise event ordering/dedup/retry/DLQ |
| Legacy compatibility via adapter (polling → events) | Peak-season playbooks (“Double 11”, etc.) |

---

## 💼 Business Context

### User Story

> **As a** customer or operator,
> **I want to** subscribe to a shipment’s live updates,
> **So that** I can see status/location changes without polling.

### Cross-domain boundaries (TMS system view)

- **Multi-Carrier** ([02-MULTI-CARRIER.md](02-MULTI-CARRIER.md)) produces `TrackingNumber` when booking.
- **Realtime Tracking** (this doc) distributes updates for a `TrackingNumber`.
- **Order Processing** (04) consumes status changes to update order state (out of scope to implement here).
- **Audit Tracking** (05) can record the event stream (out of scope to implement here).

### Event Types (minimal set)

| Event | Meaning | Typical Subscribers |
|------|---------|---------------------|
| `TrackingStatusChanged` | Status moved (Created → InTransit → Delivered) | UI, Order Processing, Audit |
| `TrackingLocationUpdated` | Latest geo point changed | UI, Ops dashboards |

### Business Rules (learning edition)

| # | Rule | Why it exists |
|---|------|---------------|
| 1 | Subscription is by `TrackingNumber` | Natural partition key |
| 2 | Publishing an event notifies *only* subscribers of that tracking number | Prevents noisy broadcasts |
| 3 | System can project a “current snapshot” (latest status + latest location) | New subscriber gets immediate state |
| 4 | Location updates can be rate-limited (optional decorator) | Avoid UI spam and unnecessary load |

---

## 🏗 Architecture Layers

### The Separation Principle

```
┌────────────────────────────────────────────────────────────────┐
│                      CLEAN ARCHITECTURE                        │
├────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  CORE LAYER (pure C#, zero dependencies)                  │  │
│  │                                                          │  │
│  │  • Tracking events + domain models                        │  │
│  │  • Observer contracts (subscribe / notify)                │  │
│  │  • State projection (latest snapshot)                     │  │
│  │  • No SignalR, no HTTP, no timers                         │  │
│  └───────────────────────────┬──────────────────────────────┘  │
│                              │ depends on abstractions          │
│  ┌───────────────────────────▼──────────────────────────────┐  │
│  │  INFRASTRUCTURE LAYER (adapters + sources)                │  │
│  │                                                          │  │
│  │  • Optional: SignalR notifier adapter                     │  │
│  │  • Mock sources: random generator / scripted replay       │  │
│  │  • Legacy adapter: polling → events                       │  │
│  └───────────────────────────┬──────────────────────────────┘  │
│                              │ composed in                      │
│  ┌───────────────────────────▼──────────────────────────────┐  │
│  │  DEMO LAYER (composition root)                            │  │
│  │                                                          │  │
│  │  • Wires subject + observers + sources                    │  │
│  │  • Runs demo scenario                                     │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

### Why this layering matters

| Layer | What goes here | What does NOT go here |
|-------|---------------|----------------------|
| **Core** | Events, observer contracts, snapshot logic | SignalR, JSON, timers, external calls |
| **Infrastructure** | SignalR adapter, mock generators, legacy adapters | Business rules and state transitions |
| **Demo** | Wiring, runnable scenarios | Reusable domain logic |

---

## 📝 Feature Specification

| # | Feature | Pattern Demonstrated | Interview Value |
|---|---------|---------------------|----------------|
| **F1** | Subscribe + publish tracking updates | Observer | Core pattern mastery |
| **F2** | Optional realtime push to clients | Adapter + DIP | Boundary discipline |
| **F3** | Mock generator + scripted replay | Testability mindset | Deterministic demos |

### F1 — Observer-based subscription (MUST HAVE)

**Description**: Support subscribing observers to a `TrackingNumber` and notifying them when a new event is published.

**Acceptance**:
- [ ] Subscriber can subscribe/unsubscribe by tracking number
- [ ] Publishing `TrackingStatusChanged` notifies only that tracking number’s subscribers
- [ ] New subscriber can receive current snapshot immediately (latest known state)

### F2 — Optional: SignalR as a delivery adapter (INTERVIEW POLISH)

**Description**: Provide a thin adapter that receives events (as an Observer) and forwards them to connected clients via SignalR.

**Acceptance**:
- [ ] Core does not reference SignalR types
- [ ] SignalR adapter implements `ITrackingObserver`
- [ ] Clients subscribe via hub method `Subscribe(trackingNumber)` (conceptual)

### F3 — Mock update sources (MUST HAVE)

**Description**: Provide mock sources that publish events without real carrier integrations:

- Random generator (good for “wow demo”)
- Scripted replay (good for deterministic tests/interviews)

**Acceptance**:
- [ ] Generator can publish status changes + locations
- [ ] Scripted replay can run the same sequence repeatedly

---

## 🎨 Design Pattern Application

### Pattern collaboration diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                         OBSERVER COLLABORATION                        │
├──────────────────────────────────────────────────────────────────────┤
│  SOURCES (publish)                                                    │
│  ┌─────────────────────┐     ┌──────────────────────┐                 │
│  │ MockTrackingSource  │     │ LegacyPollingAdapter │                 │
│  └──────────┬──────────┘     └──────────┬───────────┘                 │
│             │ publishes                  │ publishes                   │
│             ▼                            ▼                            │
│  ┌──────────────────────────────────────────────────────────────┐     │
│  │ «core» TrackingSubject (per tracking number subscriptions)    │     │
│  │  - Subscribe(trackingNo, observer)                             │     │
│  │  - Unsubscribe(...)                                            │     │
│  │  - Publish(event) → Notify observers                            │     │
│  └───────────────┬───────────────────────────────┬────────────────┘     │
│                  │ notifies                      │ notifies              │
│                  ▼                               ▼                      │
│     ┌────────────────────────┐      ┌─────────────────────────────┐     │
│     │ UI / Dashboard Observer │      │ SignalRNotifier (Adapter)   │     │
│     └────────────────────────┘      └─────────────────────────────┘     │
└──────────────────────────────────────────────────────────────────────┘
```

### Why Observer here?

| Need | How Observer helps |
|------|---------------------|
| Push updates to many consumers | One publish → many notifications |
| Add/remove consumers safely | Subscribe/unsubscribe without changing sources |
| Keep sources unaware of who listens | Strong decoupling (SRP + DIP) |

---

## 📜 Interface Contracts

### Core layer contracts (conceptual C#)

```csharp
public readonly record struct TrackingNumber(string Value);

public interface ITrackingEvent
{
    TrackingNumber TrackingNumber { get; }
    DateTimeOffset OccurredAt { get; }
}

public interface ITrackingObserver
{
    Task OnNextAsync(ITrackingEvent evt, CancellationToken ct = default);
}

public interface ITrackingSubject
{
    IDisposable Subscribe(TrackingNumber trackingNumber, ITrackingObserver observer);
    Task PublishAsync(ITrackingEvent evt, CancellationToken ct = default);

    // Optional quality-of-life: snapshot for new subscribers
    TrackingSnapshot? TryGetSnapshot(TrackingNumber trackingNumber);
}
```

### Optional: SignalR delivery boundary

```csharp
// Pure boundary: core depends on this abstraction, not SignalR types.
public interface IRealtimeClientNotifier
{
    Task NotifyAsync(TrackingNumber trackingNumber, ITrackingEvent evt, CancellationToken ct = default);
}

// Infrastructure implements IRealtimeClientNotifier using SignalR hub context.
```

### Sources (mock + legacy)

```csharp
public interface ITrackingSource
{
    string Name { get; }
    Task StartAsync(CancellationToken ct = default);
}

// Source publishes into ITrackingSubject (dependency inversion).
```

---

## 📊 Data Models

Keep models small so the pattern stays visible.

```csharp
public enum TrackingStatus
{
    Created,
    InTransit,
    Delivered,
    Exception
}

public sealed record GeoPoint(decimal Lat, decimal Lng);

public sealed record TrackingSnapshot(
    TrackingNumber TrackingNumber,
    TrackingStatus? Status,
    GeoPoint? LastKnownLocation,
    DateTimeOffset UpdatedAt);

public sealed record TrackingStatusChanged(
    TrackingNumber TrackingNumber,
    TrackingStatus Status,
    DateTimeOffset OccurredAt) : ITrackingEvent;

public sealed record TrackingLocationUpdated(
    TrackingNumber TrackingNumber,
    GeoPoint Location,
    DateTimeOffset OccurredAt) : ITrackingEvent;
```

---

## 🛡 Infrastructure Layer

### Optional: Rate limiting as a decorator

If location updates are too frequent, you can wrap an observer with a decorator:

- Implements `ITrackingObserver`
- Drops (or coalesces) events that arrive too fast

This demonstrates “cross-cutting” without polluting the subject or sources.

### Optional: SignalR adapter (concept)

- Clients connect to `TrackingHub`
- `Subscribe(trackingNumber)` puts the connection into a group named by tracking number
- `SignalRNotifier` receives events and forwards them to the group

Key boundary rule:

> SignalR types stay in Infrastructure. Core only sees `ITrackingObserver` / `IRealtimeClientNotifier`.

### Mock sources

Two recommended sources for learning + interviews:

1. **Random generator**: emits location updates every N seconds + occasional status changes
2. **Scripted replay**: reads a small in-memory script (list of events) and replays them deterministically

### Legacy compatibility (keeping legacy code)

If you already have a “polling tracker” (legacy), keep it:

- `LegacyPollingAdapter` calls the legacy API/service
- It *translates* legacy results into `ITrackingEvent`
- It publishes into `ITrackingSubject`

This keeps the new Observer design while respecting existing code.

---

## ✅ Acceptance Criteria

- `ITrackingSubject` supports subscribe/unsubscribe and publishes events to observers scoped by `TrackingNumber`.
- New subscriber can receive current snapshot immediately (`TryGetSnapshot` or equivalent behavior).
- Two mock sources exist: **random generator** and **scripted replay**, and both publish events into the subject.
- Optional: SignalR delivery stays isolated in Infrastructure (core remains dependency-free).
- Optional: rate limiting composes as a decorator around any `ITrackingObserver` without changing subject or sources.

### Testing checklist (what to actually test)

- Subject tests
  - Subscribe/unsubscribe correctness
  - Publish notifies only the correct tracking number’s observers
  - Snapshot behavior for new subscribers
- Mock source tests
  - Scripted replay is deterministic
  - Random generator emits both status and location events
- SignalR adapter tests (optional)
  - Core has zero references to SignalR types
  - Adapter forwards events to the correct client group
- Decorator tests (optional)
  - Drops/coalesces noisy location updates without affecting status changes

---

## 🧱 Project Structure

```
src/
  DTExpress.RealtimeTracking.Core/
    Contracts/
      ITrackingEvent.cs
      ITrackingObserver.cs
      ITrackingSubject.cs
      IRealtimeClientNotifier.cs        (optional boundary)
    Models/
      TrackingNumber.cs
      TrackingSnapshot.cs
      TrackingStatus.cs
      TrackingStatusChanged.cs
      TrackingLocationUpdated.cs
    Services/
      TrackingSubject.cs                (in-memory implementation)
      SnapshotProjector.cs              (optional)
  DTExpress.RealtimeTracking.Infrastructure/
    SignalR/
      TrackingHub.cs                    (optional)
      SignalRRealtimeNotifier.cs        (optional)
    Sources/
      RandomTrackingSource.cs
      ScriptedReplaySource.cs
      LegacyPollingAdapter.cs           (optional)
    Decorators/
      RateLimitObserverDecorator.cs     (optional)
  DTExpress.RealtimeTracking.Demo/
    Program.cs

tests/
  DTExpress.RealtimeTracking.Tests/
    SubscriptionTests.cs
    SnapshotTests.cs
    MockSourceTests.cs
    (optional) RateLimitDecoratorTests.cs
```

---

## 🧠 Interview Guide

### 30–60 second talk-track

> “I modeled realtime tracking as event distribution. Producers publish tracking events, and subscribers receive them via the Observer pattern. The core is pure and testable. SignalR is optional and treated as an adapter: it forwards events to connected clients without coupling the domain to SignalR APIs. I also added mock generators and a scripted replay source so the system is demoable and testable without real carrier integrations.”

### Common interview questions (with the “why”)

- **Why Observer?** Many consumers, decoupled producers, and selective subscription by tracking number.
- **Why not bake SignalR into core?** Keeps domain stable; SignalR is replaceable delivery infrastructure.
- **How do you support legacy polling?** Use an adapter that translates polling results into events and publishes them.

---

## 📚 Study Resources

- [OBSERVER-PATTERN.md](../design-patterns/OBSERVER-PATTERN.md)
- Optional reading: Adapter pattern (infrastructure boundary) in [ADAPTER-PATTERN.md](../design-patterns/ADAPTER-PATTERN.md)
- Practice: add a new observer (e.g., “audit logger”) without changing sources or subject

---

## 🔗 Related Documents

- [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md)
- [02-MULTI-CARRIER.md](02-MULTI-CARRIER.md)
- [04-ORDER-PROCESSING.md](04-ORDER-PROCESSING.md)
- [05-AUDIT-TRACKING.md](05-AUDIT-TRACKING.md)
- [OBSERVER-PATTERN.md](../design-patterns/OBSERVER-PATTERN.md)
- [ADAPTER-PATTERN.md](../design-patterns/ADAPTER-PATTERN.md)
