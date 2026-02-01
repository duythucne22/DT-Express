# 📚 DT-Express Logistics System - Design Specification Index

## 🗂️ Document Structure

```
docs/
├── 00-INDEX.md                      ← You are here (Navigation Hub)
├── 01-SYSTEM-VISION.md              ← Overall system vision & goals
│
├── core-domains/                    ← 5 Core Domain Specifications
│   ├── 01-DYNAMIC-ROUTING.md        ← Route optimization & algorithms
│   ├── 02-MULTI-CARRIER.md          ← Carrier integration & adapters
│   ├── 03-REALTIME-TRACKING.md      ← GPS & status monitoring
│   ├── 04-ORDER-PROCESSING.md       ← Order lifecycle & CQRS
│   └── 05-AUDIT-TRACKING.md         ← Compliance & change tracking
│
├── design-patterns/                 ← Pattern Study & Reference
│   ├── STRATEGY-PATTERN.md          ← For routing algorithms
│   ├── ADAPTER-PATTERN.md           ← For carrier integration
│   ├── FACTORY-PATTERN.md           ← For adapter creation
│   ├── OBSERVER-PATTERN.md          ← For real-time updates
│   ├── STATE-PATTERN.md             ← For order lifecycle
│   ├── CQRS-PATTERN.md              ← For command/query separation
│   ├── DECORATOR-PATTERN.md         ← For behavioral extensions
│   └── INTERCEPTOR-PATTERN.md       ← For cross-cutting interception
│
├── data-models/                     ← Entity & Aggregate Specs
│   ├── 00-DATA-MODEL-OVERVIEW.md    ← Relationship map & design rules
│   ├── ORDER-AGGREGATE.md           ← Business intent capture
│   ├── SHIPMENT-AGGREGATE.md        ← Physical journey tracking
│   ├── CARRIER-AGGREGATE.md         ← External partner capabilities
│   ├── CUSTOMER-AGGREGATE.md        ← Business relationships
│   ├── WAREHOUSE-AGGREGATE.md       ← Origin points & inventory
│   ├── NETWORK-NODE-AGGREGATE.md    ← Logistics topology
│   ├── SERVICE-LEVEL-AGGREGATE.md   ← Products & SLA definitions
│   └── VALUE-OBJECTS.md             ← Shared immutable concepts
│
└── architecture/                    ← System Architecture Docs
    ├── CLEAN-ARCHITECTURE.md
    ├── LAYER-RESPONSIBILITIES.md
    └── INTEGRATION-MAP.md
```

---

## 🔗 Domain Relationship Map

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CORE DOMAIN CONNECTIONS                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│    ┌──────────────────┐         ┌──────────────────┐                │
│    │  04-ORDER        │────────>│  01-DYNAMIC      │                │
│    │  PROCESSING      │ request │  ROUTING         │                │
│    │  (CQRS+State)    │  route  │  (Strategy)      │                │
│    └────────┬─────────┘         └────────┬─────────┘                │
│             │                            │                          │
│             │ dispatch                   │ assign                   │
│             ▼                            ▼                          │
│    ┌──────────────────┐         ┌──────────────────┐                │
│    │  05-AUDIT        │<────────│  02-MULTI        │                │
│    │  TRACKING        │  log    │  CARRIER         │                │
│    │  (Interceptor)   │  all    │ (Adapter+Factory)│                │
│    └────────▲─────────┘         └────────┬─────────┘                │
│             │                            │                          │
│             │ track                      │ track                    │
│             │ changes                    ▼                          │
│             │                   ┌──────────────────┐                │
│             └───────────────────│  03-REALTIME     │                │
│                     audit       │  TRACKING        │                │
│                     events      │  (Observer)      │                │
│                                 └──────────────────┘                │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 📋 Document Status Tracker

| Document | Domain | Status | Priority | Key Content |
|----------|--------|--------|----------|-------------|
| [01-DYNAMIC-ROUTING](core-domains/01-DYNAMIC-ROUTING.md) | TMS | 🟢 Completed | 🔴 High | Algorithm analysis, Strategy pattern, 高德/百度 API |
| [02-MULTI-CARRIER](core-domains/02-MULTI-CARRIER.md) | CMS | 🟢 Completed | 🔴 High | Adapter/Factory patterns, SF/JD/ZTO integration |
| [03-REALTIME-TRACKING](core-domains/03-REALTIME-TRACKING.md) | TMS | 🟢 Basic | 🔴 High | Observer pattern, SignalR, Geofencing |
| [04-ORDER-PROCESSING](core-domains/04-ORDER-PROCESSING.md) | OMS | 🟢 Basic | 🔴 High | State pattern, CQRS, MediatR |
| [05-AUDIT-TRACKING](core-domains/05-AUDIT-TRACKING.md) | Cross | 🟢 Basic | 🟡 Medium | Interceptor pattern, EF Core |

### Pattern Study Guides Status

| Document | Pattern | Status | Applied In |
|----------|---------|--------|------------|
| [STRATEGY-PATTERN](design-patterns/STRATEGY-PATTERN.md) | Strategy | 🟢 Complete | Dynamic Routing |
| [ADAPTER-PATTERN](design-patterns/ADAPTER-PATTERN.md) | Adapter | 🟢 Complete | Multi-Carrier |
| [FACTORY-PATTERN](design-patterns/FACTORY-PATTERN.md) | Factory | 🟢 Complete | Multi-Carrier |
| [OBSERVER-PATTERN](design-patterns/OBSERVER-PATTERN.md) | Observer | 🟢 Complete | Real-time Tracking |
| [STATE-PATTERN](design-patterns/STATE-PATTERN.md) | State | 🟢 Complete | Order Processing |
| [CQRS-PATTERN](design-patterns/CQRS-PATTERN.md) | CQRS | 🟢 Complete | Order Processing |
| [DECORATOR-PATTERN](design-patterns/DECORATOR-PATTERN.md) | Decorator | 🟢 Complete | Cross-cutting |
| [INTERCEPTOR-PATTERN](design-patterns/INTERCEPTOR-PATTERN.md) | Interceptor | 🟢 Complete | Audit / Cross-cutting |

---

## 🎯 Reading Order (Recommended)

### Core Pattern Understanding
1. **START HERE** → [01-DYNAMIC-ROUTING.md](core-domains/01-DYNAMIC-ROUTING.md)
   - Foundation pattern: Strategy
   - No dependencies
   - Learn algorithm switching

2. **THEN** → [02-MULTI-CARRIER.md](core-domains/02-MULTI-CARRIER.md)
   - Builds on: Route results need carrier assignment
   - Patterns: Adapter + Factory
   - Learn API abstraction

3. **THEN** → [03-REALTIME-TRACKING.md](core-domains/03-REALTIME-TRACKING.md)
   - Builds on: Track shipments from carriers
   - Pattern: Observer
   - Learn push notifications

### Business Logic Layer
4. **THEN** → [04-ORDER-PROCESSING.md](core-domains/04-ORDER-PROCESSING.md)
   - Orchestrates: All above domains
   - Patterns: State + CQRS
   - Learn order lifecycle

### Cross-Cutting Concerns
5. **FINALLY** → [05-AUDIT-TRACKING.md](core-domains/05-AUDIT-TRACKING.md)
   - Applies to: All domains
   - Pattern: Interceptor
   - Learn compliance logging

---

## 🔑 Key Terms Glossary

| Term | Chinese | Definition | Used In |
|------|---------|------------|---------|
| **Strategy Pattern** | 策略模式 | Runtime algorithm switching | Dynamic Routing |
| **Adapter Pattern** | 适配器模式 | Unified interface for different APIs | Multi-Carrier |
| **Factory Pattern** | 工厂模式 | Object creation abstraction | Multi-Carrier |
| **Observer Pattern** | 观察者模式 | Push-based notifications | Real-time Tracking |
| **State Pattern** | 状态模式 | State-specific behavior encapsulation | Order Processing |
| **CQRS** | 命令查询分离 | Separate read/write models | Order Processing |
| **Aggregate Root** | 聚合根 | DDD entity cluster boundary | All domains |
| **Value Object** | 值对象 | Immutable domain concept | All domains |

---

## 📖 Reference Sources

### Chinese Tech Community
- **CSDN**: `物流路由策略模式 实战`, `多承运商 工厂模式`, `SignalR 实时物流监控`
- **Gitee**: `3PL-Carrier-Adapter`, `DDD-CQRS-ES-Sample`, `MicroservicesDemo`

### Enterprise References
- SF Express (顺丰) routing algorithms
- JD Logistics (京东物流) carrier integration
- ZTO Express (中通) order processing
- Cainiao (菜鸟) real-time tracking

---