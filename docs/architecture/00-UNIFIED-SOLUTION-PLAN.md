# 🏗️ DT-Express Unified Solution Plan — .NET 8

> **Target**: .NET 8 LTS · Single Solution · Interview-Ready  
> **Architecture**: Clean Architecture · SOLID · DDD-Lite  

---

## 📋 Table of Contents

1. [Executive Summary](#-executive-summary)
2. [Architecture Decision Records](#-architecture-decision-records)
3. [Constraints & Rules](#-constraints--rules)
4. [Technology Stack](#-technology-stack)
5. [Solution Structure](#-solution-structure)
6. [Pattern Application Map](#-pattern-application-map)
7. [Cross-Domain Integration](#-cross-domain-integration)
8. [Dependency Flow](#-dependency-flow)
9. [Verification Checklist](#-verification-checklist)
10. [Reference Resources](#-reference-resources)
11. [Related Documents](#-related-documents)

---

## 🎯 Executive Summary

### What This Is

A **single .NET 8 solution** (`DtExpress.sln`) containing 5 core domains of a Transport Management System (TMS), unified under Clean Architecture. Every design pattern is demonstrated in a real business context, not in isolation.

### What This Is NOT

- Not a production-ready TMS (no real carrier APIs, no real DB)
- Not a microservices demo (single solution, in-process communication)
- Not an event sourcing framework (simple domain events, append-only audit)

### The 30-Second Pitch

> "DT-Express is a .NET 8 Clean Architecture showcase.  
> **5 domains** — Routing, Carriers, Tracking, Orders, Audit.  
> **8 patterns** — Strategy, Factory, Decorator, Adapter, Observer, State, CQRS, Interceptor.  
> **Zero external packages** — everything runs with `System.*` and the ASP.NET Core framework.  
> **100% in-memory** — mock carriers, mock maps, mock everything.  
> Every pattern solves a **real business problem**, not a textbook exercise."

---

## 📝 Architecture Decision Records

### ADR-001: Single Solution, Four Projects

**Decision**: One solution with Domain / Application / Infrastructure / Api projects.  
**Rationale**: Simplest structure that enforces Clean Architecture dependency rules via project references. No shared kernel project needed at this scale.  
**Consequence**: All 5 domains share one Domain project (organized by namespace/folder).

### ADR-002: .NET 8 LTS, No External NuGet Packages

**Decision**: Target `net8.0`. No NuGet packages beyond the .NET SDK and ASP.NET Core shared framework.  
**Allowed**:
- `Microsoft.AspNetCore.*` (framework, ships with SDK)
- `Microsoft.NET.Test.Sdk` + `xunit` + `xunit.runner.visualstudio` (test tooling only)
- `Swashbuckle.AspNetCore` (Swagger UI — single allowed tooling package)

**Forbidden**:
- MediatR, AutoMapper, FluentValidation, Polly, Serilog, EF Core, Dapper
- Any community NuGet package in `src/` projects

**Rationale**: Demonstrates that patterns are language features, not library features. Shows understanding of underlying mechanisms that packages abstract away.  
**Consequence**: Hand-built command/query dispatcher, manual mapping, custom validation.

### ADR-003: In-Memory Everything

**Decision**: All persistence, caching, and external service calls are mocked with in-memory implementations using `ConcurrentDictionary<,>` and `List<>`.  
**Rationale**: Eliminates infrastructure setup. Focus stays on patterns and architecture.  
**Consequence**: Data is lost on restart. Thread-safety via `Concurrent*` collections.

### ADR-004: Controller-Based API (Not Minimal API)

**Decision**: Use traditional `[ApiController]` controllers, not Minimal API.  
**Rationale**: More familiar in enterprise contexts, shows DI injection into controllers, supports action filters, better for demonstrating middleware pipeline.  
**Consequence**: Slightly more boilerplate but better interview story.

### ADR-005: Interface Segregation — ≤3 Methods Per Interface

**Decision**: Every interface has at most 3 methods (properties don't count against the limit).  
**Rationale**: Forces ISP compliance. Makes mocking trivial. Keeps contracts focused.  
**Consequence**: Some concepts split across multiple small interfaces.

### ADR-006: No switch/if-else for Pattern Selection

**Decision**: All pattern resolution uses dictionary-based registries populated by DI, or generic type resolution via `IServiceProvider`.  
**Rationale**: Demonstrates Open/Closed Principle — adding a new strategy/adapter/state requires only a new class + DI registration, zero changes to existing code.  
**Consequence**: Slightly more DI wiring code, but the registration is the single extension point.

### ADR-007: Order State Machine — 4 Forward States + 1 Terminal

**Decision**: `Created → Confirmed → Shipped → Delivered` with `Cancelled` as terminal.  
**Rationale**: 4 states is enough to demonstrate the State Pattern without the complexity of reverse logistics. Keeps the demo focused.  
**Consequence**: No return/refund flow in V1.

---

## 🚧 Constraints & Rules

### Hard Constraints

| # | Constraint | Verification |
|---|-----------|-------------|
| C1 | Domain project has **zero** project references | `.csproj` has no `<ProjectReference>` |
| C2 | Domain project has **zero** NuGet references | `.csproj` has no `<PackageReference>` |
| C3 | Application references **only** Domain | Check `.csproj` |
| C4 | Infrastructure references Application + Domain | Check `.csproj` |
| C5 | Api references Infrastructure (transitive to all) | Check `.csproj` |
| C6 | No `if/else` or `switch` chains for pattern dispatch | Code review |
| C7 | Every interface has ≤ 3 methods | Count per interface |
| C8 | All external services are mocked in-memory | No HTTP clients, no DB contexts |
| C9 | Every public class has at least one unit test | Test coverage check |

### SOLID Checklist

| Principle | How It's Enforced | Where to Look |
|-----------|-------------------|---------------|
| **S** — Single Responsibility | Strategy = business logic, Pathfinder = algorithm, Decorator = cross-cutting | Routing domain |
| **O** — Open/Closed | New strategy = new class + DI line, no existing code changes | `ServiceCollectionExtensions.cs` |
| **L** — Liskov Substitution | All `IRouteStrategy` implementations are interchangeable | Strategy tests |
| **I** — Interface Segregation | ≤3 methods per interface, split large contracts | All interfaces |
| **D** — Dependency Inversion | Domain defines interfaces, Infrastructure implements them | Project references |

---

## 🔧 Technology Stack

| Layer | Technology | Justification |
|-------|-----------|---------------|
| Runtime | .NET 8 LTS | Long-term support, latest C# 12 features |
| API Framework | ASP.NET Core 8 (Controllers) | Enterprise standard, DI-native |
| API Docs | Swashbuckle.AspNetCore | Swagger UI — sole external tooling package |
| Persistence | `ConcurrentDictionary<,>` | Thread-safe in-memory store |
| Caching | `ConcurrentDictionary<,>` | Simple key-value cache |
| Logging | `ILogger<T>` (Microsoft.Extensions.Logging) | Built-in framework logging |
| Serialization | `System.Text.Json` | Built-in, high-performance |
| Testing | xUnit + `Microsoft.AspNetCore.Mvc.Testing` | Standard .NET test stack |

### What We Explicitly Do NOT Use

| Category | Excluded | Reason |
|----------|----------|--------|
| ORM | EF Core, Dapper | In-memory stores are sufficient |
| Mediator | MediatR | Hand-built dispatcher shows understanding |
| Mapping | AutoMapper | Manual mapping is more explicit |
| Validation | FluentValidation | Simple guard clauses in domain |
| Resilience | Polly | No real HTTP calls to protect |
| Logging | Serilog, NLog | Built-in `ILogger<T>` is enough |
| Messaging | MassTransit, RabbitMQ | In-process events via observer |

---

## 📁 Solution Structure

```
DtExpress.sln
│
├── src/
│   ├── DtExpress.Domain/              ← Pure C#, ZERO dependencies
│   ├── DtExpress.Application/         ← Orchestration, refs Domain only
│   ├── DtExpress.Infrastructure/      ← Mocks + implementations, refs App + Domain
│   └── DtExpress.Api/                 ← ASP.NET Core, refs Infrastructure (transitive)
│
├── tests/
│   ├── DtExpress.Domain.Tests/
│   ├── DtExpress.Application.Tests/
│   ├── DtExpress.Infrastructure.Tests/
│   └── DtExpress.Api.Tests/
│
└── docs/
    └── architecture/                   ← You are here
```

### Dependency Graph (arrows = "depends on")

```
Api ───────► Infrastructure ───────► Application ───────► Domain
                                                            ▲
                                                            │
                                                     ZERO DEPS
                                                   (pure C# only)
```

> 📐 See [01-PROJECT-STRUCTURE.md](01-PROJECT-STRUCTURE.md) for complete file listing.

---

## 🧩 Pattern Application Map

### Pattern × Domain Matrix

| Pattern | Domain 01: Routing | Domain 02: Carrier | Domain 03: Tracking | Domain 04: Orders | Domain 05: Audit |
|---------|-------------------|-------------------|--------------------|--------------------|-----------------|
| **Strategy** | ✅ Route algorithms | ✅ Carrier selection | — | — | — |
| **Factory** | ✅ Strategy creation | ✅ Adapter resolution | — | — | — |
| **Decorator** | ✅ Cache/Log/Validate | ✅ Cache/Log adapter | — | — | ✅ PII masking |
| **Adapter** | — | ✅ SF/JD normalization | — | — | — |
| **Observer** | — | — | ✅ Event subscription | — | — |
| **State** | — | — | — | ✅ Order lifecycle | — |
| **CQRS** | — | — | — | ✅ Command/Query split | — |
| **Interceptor** | — | — | — | — | ✅ Change capture |

### Pattern Collaboration Flow

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         END-TO-END FLOW                                    │
│                                                                            │
│  Customer creates order (CQRS Command)                                     │
│       │                                                                    │
│       ▼                                                                    │
│  Order enters CREATED state (State Pattern)                                │
│       │                                                                    │
│       ▼ confirm                                                            │
│  Order → CONFIRMED → system dispatches:                                    │
│       │                                                                    │
│       ├──► Routing: Factory creates Strategy → Algorithm calculates route  │
│       │    (Strategy + Factory + Decorator)                                │
│       │                                                                    │
│       ├──► Carrier: Factory resolves Adapter → get quotes → book best      │
│       │    (Adapter + Factory + Strategy for selection)                    │
│       │                                                                    │
│       ├──► Tracking: Subject notifies Observers of new shipment            │
│       │    (Observer Pattern)                                              │
│       │                                                                    │
│       └──► Audit: Interceptor captures all state changes                   │
│            (Interceptor + Decorator for PII masking)                       │
│                                                                            │
│  Order → SHIPPED → DELIVERED (State Pattern transitions)                   │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔗 Cross-Domain Integration

### Integration Map

```
┌──────────────┐     route request     ┌──────────────┐
│   Orders     │ ───────────────────►  │   Routing    │
│  (04-CQRS    │                       │  (01-Strategy│
│   +State)    │  ◄─── route result    │   +Factory)  │
└──────┬───────┘                       └──────────────┘
       │                                      
       │ booking request                      
       ▼                                      
┌──────────────┐     tracking number   ┌──────────────┐
│   Carrier    │ ───────────────────►  │   Tracking   │
│  (02-Adapter │                       │(03-Observer) │
│   +Factory)  │                       │              │
└──────┬───────┘                       └──────────────┘
       │                                      │
       │ audit events                         │ audit events
       ▼                                      ▼
┌─────────────────────────────────────────────────────┐
│                  Audit (05-Interceptor)             │
└─────────────────────────────────────────────────────┘
```

### Cross-Domain Ports (Application Layer)

| Port Interface | Defined In | Implemented In | Called By |
|---------------|-----------|---------------|----------|
| `IRoutingPort` | Application/Ports | Infrastructure | Order dispatch handler |
| `ICarrierPort` | Application/Ports | Infrastructure | Order dispatch handler |
| `ITrackingPort` | Application/Ports | Infrastructure | Order query handler |
| `IAuditPort` | Application/Ports | Infrastructure | All command handlers |

> These ports are **application-layer abstractions** that bridge domains. They are NOT domain interfaces.

---

## 🔀 Dependency Flow

### Layer Dependency Rules

```
┌─────────────────────────────────────────────────────────────────────┐
│  DEPENDENCY RULES (enforced by project references)                  │
├─────────────────────────────────────────────────────────────────────┤
│  Domain:         depends on NOTHING                                 │
│  Application:    depends on Domain                                  │
│  Infrastructure: depends on Application + Domain                    │
│  Api:            depends on Infrastructure (transitive to all)      │
│                                                                     │
│  Tests:          depends on the layer they test                     │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│  FLOW DIRECTION                                                     │
│                                                                     │
│  Interfaces flow DOWN (defined in Domain/Application)               │
│  Implementations flow UP (live in Infrastructure)                   │
│  Data flows INWARD (API → Application → Domain)                     │
│  Dependencies point INWARD (Infrastructure → Application → Domain)  │
└─────────────────────────────────────────────────────────────────────┘
```

### What Lives Where

| Artifact | Layer | Rationale |
|----------|-------|-----------|
| `IRouteStrategy` | Domain | Pure business contract |
| `RouteRequest`, `Route` | Domain | Domain models |
| `ICommandHandler<,>` | Application | Orchestration contract |
| `CreateOrderHandler` | Application | Orchestrates domain + ports |
| `IRoutingPort` | Application | Cross-domain port |
| `FastestRouteStrategy` | Infrastructure | Contains algorithm implementation |
| `AStarPathfinder` | Infrastructure | Pure computation |
| `SfExpressAdapter` | Infrastructure | Mock carrier mapping |
| `InMemoryOrderRepository` | Infrastructure | Mock persistence |
| `RoutingController` | Api | HTTP entry point |
| `ServiceCollectionExtensions` | Infrastructure | DI wiring (composition root) |

---

## ✅ Verification Checklist

### Pre-Implementation Verification

- [ ] All interfaces defined in [02-INTERFACE-CONTRACTS.md](02-INTERFACE-CONTRACTS.md) have ≤3 methods
- [ ] All domain models defined in [03-DOMAIN-MODELS.md](03-DOMAIN-MODELS.md) are immutable records
- [ ] All API endpoints defined in [05-API-ENDPOINTS.md](05-API-ENDPOINTS.md) have request/response shapes
- [ ] DI registration in [04-DI-COMPOSITION.md](04-DI-COMPOSITION.md) has no switch/if-else
- [ ] Test plan in [06-TESTING-STRATEGY.md](06-TESTING-STRATEGY.md) covers all acceptance criteria
- [ ] Implementation tasks in [07-IMPLEMENTATION-TASKS.md](07-IMPLEMENTATION-TASKS.md) are ordered by dependency

### Post-Implementation Verification

- [ ] `dotnet build` succeeds with zero warnings
- [ ] `dotnet test` passes all tests
- [ ] Domain `.csproj` has zero `<ProjectReference>` and zero `<PackageReference>`
- [ ] Swagger UI loads and shows all endpoints grouped by domain
- [ ] No `switch` or multi-branch `if/else` in pattern dispatch code
- [ ] Every interface implementation can be swapped by changing only DI registration

---

## 📚 Reference Resources

### .NET Clean Architecture References

| Resource | URL | Value |
|----------|-----|-------|
| Jason Taylor's Clean Architecture | `github.com/jasontaylordev/CleanArchitecture` | Gold-standard .NET CA template |
| Ardalis Clean Architecture | `github.com/ardalis/CleanArchitecture` | Steve Smith's variant |
| run-aspnetcore-cleanarchitecture | `github.com/aspnetrun/run-aspnetcore-cleanarchitecture` | Practical e-commerce example |

### Chinese Developer Community

| Resource | URL | Value |
|----------|-----|-------|
| OSharp Framework | `gitee.com/nickhudson/OSharp` | .NET rapid dev framework (clean layers) |
| ABP Framework | `abp.io` | Enterprise DDD framework (reference only) |
| .NET China Community (NCC) | `github.com/dotnetcore` | Localized .NET libraries |
| Gitee .NET Projects | `gitee.com/explore/net` | Domestic open-source .NET |

### Open-Source Logistics

| Resource | URL | Value |
|----------|-----|-------|
| Fleetbase | `github.com/fleetbase/fleetbase` | Open-source logistics OS |
| Open Logistics Foundation | `openlogisticsfoundation.org` | Standards and reference architectures |

### Design Pattern References

| Resource | Value |
|----------|-------|
| `refactoring.guru/design-patterns` | Visual pattern catalog |
| `github.com/abishekaditya/DesignPatterns` | C# pattern implementations |
| Gitee DesignPattern collections | Search `设计模式 C# 实战` |

---

## 🔗 Related Documents

### Architecture Documents (This Folder)

| Document | Purpose |
|----------|---------|
| [01-PROJECT-STRUCTURE.md](01-PROJECT-STRUCTURE.md) | Complete file tree with annotations |
| [02-INTERFACE-CONTRACTS.md](02-INTERFACE-CONTRACTS.md) | All interfaces, ISP-verified |
| [03-DOMAIN-MODELS.md](03-DOMAIN-MODELS.md) | Value objects, records, enums |
| [04-DI-COMPOSITION.md](04-DI-COMPOSITION.md) | DI wiring, OCP proof |
| [05-API-ENDPOINTS.md](05-API-ENDPOINTS.md) | Routes, Swagger, request/response |
| [06-TESTING-STRATEGY.md](06-TESTING-STRATEGY.md) | Test plan, mocking, conventions |
| [07-IMPLEMENTATION-TASKS.md](07-IMPLEMENTATION-TASKS.md) | Ordered worker agent task list |

### Domain Specifications (Upstream)

| Document | Domain | Patterns |
|----------|--------|----------|
| [../core-domains/01-DYNAMIC-ROUTING.md](../core-domains/01-DYNAMIC-ROUTING.md) | Routing | Strategy · Factory · Decorator |
| [../core-domains/02-MULTI-CARRIER.md](../core-domains/02-MULTI-CARRIER.md) | Carrier | Adapter · Factory · Strategy · Decorator |
| [../core-domains/03-REALTIME-TRACKING.md](../core-domains/03-REALTIME-TRACKING.md) | Tracking | Observer |
| [../core-domains/04-ORDER-PROCESSING.md](../core-domains/04-ORDER-PROCESSING.md) | Orders | State · CQRS |
| [../core-domains/05-AUDIT-TRACKING.md](../core-domains/05-AUDIT-TRACKING.md) | Audit | Interceptor · Decorator |
