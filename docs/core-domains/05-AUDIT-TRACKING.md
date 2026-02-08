# 🔍 05-AUDIT-TRACKING — Multi-Pattern Design Spec

> **Domain**: Audit Tracking — cross-cutting logging + traceability across all domains  
> **Patterns**: Interceptor · Dependency Inversion · (Optional) Decorator (PII masking) · (Optional) Event Stream (audit/event-store mindset)  
> **Goal**: Learning-sized, interview-ready design that still reflects real work experience (compliance/forensics), not production-ready ops engineering  
> **Dependencies**: [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md) · [02-MULTI-CARRIER.md](02-MULTI-CARRIER.md) · [03-REALTIME-TRACKING.md](03-REALTIME-TRACKING.md) · [04-ORDER-PROCESSING.md](04-ORDER-PROCESSING.md)

---

## 📋 Table of Contents

1. [Domain Overview](#-domain-overview)
2. [Business Context](#-business-context)
3. [Architecture Layers](#-architecture-layers)
4. [Feature Specification](#-feature-specification)
5. [Design Pattern Application](#-design-pattern-application)
6. [Interface Contracts](#-interface-contracts)
7. [Audit Events (Minimal Catalog)](#-audit-events-minimal-catalog)
8. [Data Models](#-data-models)
9. [Integration Points](#-integration-points)
10. [Acceptance Criteria](#-acceptance-criteria)
11. [Project Structure](#-project-structure)
12. [Interview Guide](#-interview-guide)
13. [Study Resources](#-study-resources)
14. [Related Documents](#-related-documents)

---

## 🎯 Domain Overview

### Elevator Pitch

> “Audit Tracking is a **cross-cutting module** that records *who did what, when, and why* across the TMS. It uses the **Interceptor Pattern** to capture changes without polluting business code, applies **DIP** so storage is replaceable, and optionally uses a **Decorator** to enforce PII masking consistently. The output is an append-only audit stream that can power a timeline view, investigations, and compliance evidence.”

### Purpose

- Capture **entity changes** (create/update/delete) across domains
- Capture **business actions** (dispatch decision, carrier booking, refunds, cancellations)
- Provide **timeline** queries for customer support and incident investigation
- Enforce **immutability** (append-only) + basic retention policies (conceptual)

### Scope

| In Scope | Out of Scope (explicitly) |
|----------|----------------------------|
| Interceptors for DB save + request pipeline | Full SIEM / log aggregation platform design |
| Minimal audit stream + query service | Production deployment, HA, DR, multi-region |
| PII masking concept (decorator) | ML-based PII discovery, advanced cryptography |
| Correlation IDs + basic traceability | Full compliance program coverage and legal docs |

### Key Insight

> Audit is not “a table”.
>
> Audit is a **system capability**:
> intercept → enrich context → mask → append → query by correlation.

---

## 💼 Business Context

### Why teams actually build this

- **Support**: “Why was this order cancelled?”
- **Operations**: “Which carrier booking failed and who retried it?”
- **Security**: “Who accessed/changed PII?”
- **Compliance**: “Prove a set of actions happened, immutably.”

### Typical questions the audit system must answer

- Show the order’s lifecycle timeline (create → dispatch → delivered → return/refund)
- Show what changed (before/after fields) and who made the change
- Show all actions under one correlation ID for an API request

### Business Rules (minimal)

| Rule ID | Rule | Why |
|---------|------|-----|
| BR-AU-001 | All changes are captured via interceptors, not scattered logging | consistency + SRP |
| BR-AU-002 | Audit records are append-only | trustworthiness |
| BR-AU-003 | Every record contains user + timestamp + correlation ID | forensic usefulness |
| BR-AU-004 | PII is masked in stored audit payloads (policy-based) | reduce data risk |
| BR-AU-005 | Audit queries are read-only and never mutate domain state | separation |

---

## 🏗 Architecture Layers

### Separation Principle

```
┌──────────────────────────────────────────────────────────────┐
│                       CAPTURE LAYER                           │
│  Interceptors + hooks:                                        │
│  • DB Save interceptor (entity changes)                       │
│  • Request interceptor (API calls)                            │
│  • Domain-event hook (business actions)                        │
└──────────────────────────────┬───────────────────────────────┘
                               │ produces audit records
┌──────────────────────────────▼───────────────────────────────┐
│                      CORE AUDIT LAYER                          │
│  • Models: AuditRecord, AuditContext                           │
│  • Policies: masking rules, category, retention class          │
│  • Interfaces: IAuditSink, IAuditClock, ICorrelationIdProvider │
└──────────────────────────────┬───────────────────────────────┘
                               │ depends on abstractions
┌──────────────────────────────▼───────────────────────────────┐
│                    INFRASTRUCTURE LAYER                        │
│  • Storage adapter (SQL / file / in-memory)                    │
│  • Optional decorator: PII masking on write                     │
│  • Simple query adapter (by entityId/correlation/time)         │
└──────────────────────────────┬───────────────────────────────┘
                               │
┌──────────────────────────────▼───────────────────────────────┐
│                         DEMO LAYER                             │
│  • sample flows: dispatch order, carrier booking, refund       │
│  • print timeline by correlationId                             │
└──────────────────────────────────────────────────────────────┘
```

### Why this layering matters

- “Capture” stays close to frameworks (EF Core / web pipeline) and is replaceable.
- “Core audit” stays framework-free and testable.

---

## 📝 Feature Specification

| # | Feature | Priority | Pattern(s) | Interview Value |
|---|---------|----------|------------|-----------------|
| **F1** | Entity-change auditing via interceptor | High | Interceptor | explains cross-cutting cleanly |
| **F2** | Context enrichment (user, correlation, timestamp) | High | DIP | shows operational thinking |
| **F3** | PII masking policy (optional decorator) | High | Decorator | “enterprise hygiene” without bloat |
| **F4** | Timeline query by entity/correlation | High | CQRS-style reads | demonstrates support workflows |
| **F5** | Domain-action audit (dispatch, booking, refund) | Medium | Event stream mindset | shows real-world coverage |

---

## 🎨 Design Pattern Application

### Interceptor Pattern (核心)

Intercept at boundaries so domain code stays clean:

- **DB Save**: capture entity changes (`Added/Modified/Deleted`)
- **Request pipeline**: capture endpoint + status + duration (minimal)
- **Domain action hook**: capture business decisions/events (e.g., carrier chosen)

Compact mental model:

```
Business code (clean) → framework boundary → interceptor captures → audit sink appends
```

### Dependency Inversion (storage + time + correlation)

- Core depends on `IAuditSink`, not on SQL/Elastic/etc.
- Core depends on `IClock` and `ICorrelationIdProvider` for deterministic tests.

### Optional Decorator (PII masking)

Instead of “remember to mask everywhere”, put masking in a wrapper:

- `MaskingAuditSinkDecorator : IAuditSink` wraps a real sink
- Applies policy rules to `AuditRecord.Payload` before writing

---

## 📜 Interface Contracts

Conceptual C# contracts (learning edition — not full implementation).

### Audit writing

```csharp
public interface IAuditSink
{
    Task AppendAsync(AuditRecord record, CancellationToken ct = default);
}

public interface ICorrelationIdProvider
{
    string GetCorrelationId();
}

public interface IAuditClock
{
    DateTimeOffset UtcNow { get; }
}
```

### Capture hooks

```csharp
public interface IEntityChangeInterceptor
{
    IEnumerable<AuditRecord> CaptureEntityChanges(object dbContext);
}

public interface IRequestAuditInterceptor
{
    AuditRecord CaptureRequest(RequestAuditInput input);
}

public interface IDomainActionAuditor
{
    AuditRecord CaptureDomainAction(DomainActionInput input);
}
```

### Read-only audit queries

```csharp
public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditRecord>> GetTimelineByEntityAsync(
        string entityType,
        string entityId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<AuditRecord>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken ct = default);
}
```

---

## 📣 Audit Events (Minimal Catalog)

Keep the catalog small; prefer “high signal” events.

| Category | Event | Example Source |
|----------|-------|----------------|
| Data change | `EntityChanged` | EF Core Save interceptor |
| Request | `ApiRequestCompleted` | middleware / request interceptor |
| Business action | `OrderDispatched` | 04 command handler / domain event hook |
| Business action | `CarrierBooked` | 02 booking result |
| Business action | `RefundProcessed` | 04 refund command |
| Tracking | `TrackingStatusChanged` | 03 event consumption hook |

Notes:

- We don’t aim for “log everything”. We aim for **answering investigations**.
- When in doubt: log *state transitions* and *external interactions*.

---

## 🧱 Data Models

### Core record

```csharp
public sealed record AuditRecord(
    string Id,
    string Category,
    string EventType,
    string CorrelationId,
    string ActorId,
    DateTimeOffset OccurredAt,
    string? EntityType,
    string? EntityId,
    object Payload);
```

### Payload examples (conceptual)

- `EntityChangedPayload`:
  - operation: `Added|Modified|Deleted`
  - before/after: dictionary of changed fields
- `ApiRequestCompletedPayload`:
  - method, path, statusCode, durationMs
- `OrderDispatchedPayload`:
  - orderId, routeId, chosenCarrierCode, trackingNumber

### PII masking rules (minimal)

- Phone → `138****5678`
- Email → `u***@example.com`
- Address → keep city + mask details

---

## 🔌 Integration Points

### With domain docs

- 01 routing: log “strategy selected”, constraints used, route result ID
- 02 multi-carrier: log quote summary + booking result (no raw carrier payloads)
- 03 tracking: log status changes (for timeline)
- 04 order processing: log state transitions + refunds/returns

### Cross-cutting concerns

- Correlation ID propagation: request → commands → audit records
- Policy enforcement: masking + retention classification

---

## ✅ Acceptance Criteria

### Functional

- Entity create/update/delete produces an audit record automatically (interceptor capture).
- Records include actor + timestamp + correlation ID.
- PII masking policy is applied consistently (if masking decorator is enabled).
- Timeline query returns events in chronological order for:
  - a specific order (entityType+entityId)
  - a request correlation ID

### Non-functional (learning edition targets)

- Audit capture does not require changing business logic in 01–04.
- Audit writing is replaceable via `IAuditSink`.
- Query operations are read-only.

### Testing Checklist

- Unit: masking policy transforms phone/email/address fields.
- Unit: enrichment fills actor/correlation/timestamp.
- Integration: Save interceptor captures modified entity fields (before/after).
- Integration: timeline query returns ordered results for an order ID.

---

## 🗂 Project Structure

- `src/AuditTracking.Core/`
  - models, policies, contracts (IAuditSink, IAuditQueryService)
- `src/AuditTracking.Capture/`
  - interceptors / hooks (EF save, request pipeline, domain action hook)
- `src/AuditTracking.Infrastructure/`
  - sink implementations (e.g., SQL / in-memory), masking decorator
- `src/AuditTracking.Demo/`
  - scenarios + output (timeline by correlation ID)

---

## 🧠 Interview Guide

- Why Interceptor instead of adding logging into every command handler?
- What makes an audit record “trustworthy” (immutability, correlation, actor)?
- Where do you apply PII masking, and why a decorator is a good fit?
- What’s the difference between an “audit stream” and full event sourcing?
- How do you keep audit useful without logging everything?

---

## 📚 Study Resources

- Patterns:
  - [../design-patterns/INTERCEPTOR-PATTERN.md](../design-patterns/INTERCEPTOR-PATTERN.md)
  - [../design-patterns/DECORATOR-PATTERN.md](../design-patterns/DECORATOR-PATTERN.md)
- Data models:
  - [../data-models/ORDER-AGGREGATE.md](../data-models/ORDER-AGGREGATE.md)
  - [../data-models/VALUE-OBJECTS.md](../data-models/VALUE-OBJECTS.md)

---

## 🔗 Related Documents

- System:
  - [../00-INDEX.md](../00-INDEX.md)
  - [../01-SYSTEM-VISION.md](../01-SYSTEM-VISION.md)
- Core domains:
  - [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md)
  - [02-MULTI-CARRIER.md](02-MULTI-CARRIER.md)
  - [03-REALTIME-TRACKING.md](03-REALTIME-TRACKING.md)
  - [04-ORDER-PROCESSING.md](04-ORDER-PROCESSING.md)
