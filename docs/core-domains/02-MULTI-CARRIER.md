# 🚚 02-MULTI-CARRIER - Design Specification

> **Domain**: Carrier Management System (CMS)  
> **Primary Patterns**: Adapter Pattern (适配器模式) + Factory Pattern (工厂模式)  
> **Status**: ⬜ Not Started  
> **Dependencies**: [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md) (receives route for carrier assignment)

---

## 📋 Table of Contents

1. [Domain Overview](#domain-overview)
2. [Business Context](#business-context)
3. [Feature Specification](#feature-specification)
4. [Design Pattern Application](#design-pattern-application)
5. [Interface Contracts](#interface-contracts)
6. [Carrier API Mappings](#carrier-api-mappings)
7. [Data Models](#data-models)
8. [Integration Points](#integration-points)
9. [Study Resources](#study-resources)
10. [Acceptance Criteria](#acceptance-criteria)

---

## 🎯 Domain Overview

### Purpose
The Multi-Carrier Integration domain provides a **unified interface** to interact with multiple logistics carriers (SF Express, JD, ZTO, YTO, etc.), hiding the complexity of each carrier's unique API behind a standardized abstraction.

### Scope
| In Scope | Out of Scope |
|----------|--------------|
| Carrier API abstraction | Route calculation (→ 01-DYNAMIC-ROUTING) |
| Unified booking interface | Real-time GPS tracking (→ 03-REALTIME-TRACKING) |
| Rate comparison & selection | Order management (→ 04-ORDER-PROCESSING) |
| Waybill/label generation | Financial settlement (separate module) |
| Status query unification | Driver management |
| Carrier onboarding framework | Fleet management |

### Business Value
- **Reduced Integration Cost**: 70% reduction in new carrier onboarding time
- **Best Rate Selection**: Automatic selection of optimal carrier
- **Unified Experience**: Single API for all carrier operations
- **Vendor Independence**: Easy carrier switching without code changes

---

## 💼 Business Context

### Supported Carriers (China Market)

| Carrier | Chinese Name | API Type | Coverage | Strength |
|---------|--------------|----------|----------|----------|
| SF Express | 顺丰速运 | REST | National | Express, Premium |
| JD Logistics | 京东物流 | REST | National | E-commerce integration |
| ZTO Express | 中通快递 | REST/SOAP | National | Cost-effective |
| YTO Express | 圆通速递 | REST | National | Network coverage |
| STO Express | 申通快递 | REST | National | Economy |
| Yunda Express | 韵达速递 | REST | National | Volume handling |
| Best Express | 百世快递 | REST | National | Cross-border |
| Cainiao | 菜鸟物流 | REST | Global | Platform aggregation |

### Business Rules

| Rule ID | Rule Description | Implementation |
|---------|------------------|----------------|
| BR-CR-001 | Carrier must be active and contracted | Status check before booking |
| BR-CR-002 | Rate quotes valid for 30 minutes | Quote expiration timestamp |
| BR-CR-003 | Hazmat requires certified carrier | Carrier capability filter |
| BR-CR-004 | COD orders require supported carriers | Payment capability filter |
| BR-CR-005 | International needs customs capability | Service type filter |
| BR-CR-006 | Failed booking auto-fallback to next carrier | Retry with fallback logic |

### Use Cases

#### UC-CR-001: Get Rate Quotes
```
Actor: System (triggered by order dispatch)
Precondition: Route is calculated, shipment details known
Flow:
  1. System queries all active carriers for rates
  2. Each carrier adapter translates request to carrier API format
  3. Rates returned and normalized to standard format
  4. System ranks carriers by criteria (cost, time, reliability)
Postcondition: Ranked rate quotes available for selection
```

#### UC-CR-002: Book Shipment
```
Actor: System (auto) or Dispatcher (manual)
Precondition: Carrier selected, shipment ready
Flow:
  1. System sends booking request via unified interface
  2. Adapter translates to carrier-specific API call
  3. Carrier returns tracking number and waybill
  4. System stores booking confirmation
Postcondition: Shipment booked, tracking number assigned
```

#### UC-CR-003: Generate Waybill/Label
```
Actor: Warehouse Staff
Precondition: Shipment booked with carrier
Flow:
  1. Staff requests label for shipment
  2. System calls carrier label API via adapter
  3. Adapter returns standardized label format
  4. System converts to printable PDF
Postcondition: Label ready for printing
```

#### UC-CR-004: Track Shipment Status
```
Actor: Customer or System
Precondition: Shipment has valid tracking number
Flow:
  1. System/Customer requests tracking update
  2. Adapter calls carrier tracking API
  3. Response normalized to standard status codes
  4. Tracking history updated
Postcondition: Current status available
```

---

## 📝 Feature Specification

### Feature Matrix

| Feature ID | Feature Name | Description | Pattern | Priority |
|------------|--------------|-------------|---------|----------|
| CR-F001 | Adapter Registration | Register carrier adapters at startup | Factory | 🔴 High |
| CR-F002 | Rate Inquiry | Get quotes from multiple carriers | Adapter | 🔴 High |
| CR-F003 | Smart Rate Selection | Auto-select best carrier | Strategy | 🔴 High |
| CR-F004 | Shipment Booking | Book via unified interface | Adapter | 🔴 High |
| CR-F005 | Booking Cancellation | Cancel via unified interface | Adapter | 🟡 Medium |
| CR-F006 | Status Tracking | Query status uniformly | Adapter | 🔴 High |
| CR-F007 | Waybill Generation | Generate carrier labels | Factory | 🔴 High |
| CR-F008 | Carrier Health Check | Monitor carrier API availability | Observer | 🟡 Medium |
| CR-F009 | Fallback Handling | Auto-retry with alternate carrier | Chain | 🟡 Medium |
| CR-F010 | Rate Caching | Cache frequent quotes | Cache | 🟢 Low |

### CR-F001: Adapter Registration

**Description**: System registers carrier adapters at startup, each implementing the unified interface.

**Adapter Registration Table**:
| Carrier Code | Adapter Class | API Base URL | Auth Method |
|--------------|---------------|--------------|-------------|
| SF | SFExpressAdapter | api.sf-express.com | API Key + Secret |
| JD | JDLogisticsAdapter | api.jdl.com | OAuth 2.0 |
| ZTO | ZTOExpressAdapter | openapi.zto.com | API Key |
| YTO | YTOExpressAdapter | openapi.yto.net.cn | API Key |
| STO | STOExpressAdapter | openapi.sto.cn | API Key |
| YUNDA | YundaAdapter | openapi.yunda.com | API Key + Sign |
| BEST | BestExpressAdapter | api.800best.com | API Key |

### CR-F002: Rate Inquiry

**Description**: Query multiple carriers for shipping rates simultaneously.

**Input**: ShipmentDetails (origin, destination, weight, dimensions, service type)
**Output**: List<CarrierQuote> sorted by criteria

**Rate Comparison Matrix**:
| Carrier | Express ($/kg) | Standard ($/kg) | Economy ($/kg) | ETA Express | ETA Standard |
|---------|---------------|-----------------|----------------|-------------|--------------|
| SF | ¥22 | ¥15 | ¥10 | 1 day | 2-3 days |
| JD | ¥20 | ¥14 | ¥9 | 1 day | 2-3 days |
| ZTO | ¥12 | ¥8 | ¥5 | 2 days | 3-5 days |
| YTO | ¥11 | ¥7 | ¥4.5 | 2 days | 3-5 days |

### CR-F003: Smart Rate Selection

**Description**: Automatically select optimal carrier based on configurable criteria.

**Selection Strategies**:
| Strategy | Primary Factor | Secondary | Use Case |
|----------|---------------|-----------|----------|
| CheapestRate | Lowest cost | Delivery time | Economy orders |
| FastestDelivery | Shortest ETA | Cost | Express orders |
| BestValue | Score (cost × time) | Reliability | Standard orders |
| HighestReliability | On-time % | Cost | Critical orders |
| PreferredCarrier | Customer preference | Cost | VIP customers |

---

## 🎨 Design Pattern Application

### Adapter Pattern Structure

```
┌─────────────────────────────────────────────────────────────────────┐
│                       ADAPTER PATTERN                               │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐         ┌─────────────────────────┐            │
│  │  CarrierService │────────>│   <<interface>>         │            │
│  │  (Client)       │         │   ICarrierAdapter       │            │
│  │                 │         ├─────────────────────────┤            │
│  │ Uses unified    │         │ + GetRates()            │            │
│  │ interface only  │         │ + BookShipment()        │            │
│  │                 │         │ + CancelShipment()      │            │
│  └─────────────────┘         │ + GetTrackingStatus()   │            │
│                              │ + GenerateLabel()       │            │
│                              └───────────┬─────────────┘            │
│                                          │                          │
│         ┌────────────────────────────────┼──────────────────────┐   │
│         │                    │           │          │           │   │
│         ▼                    ▼           ▼          ▼           ▼   │
│  ┌────────────┐    ┌────────────┐ ┌────────────┐ ┌────────────┐     │
│  │ SFExpress  │    │ JDLogistics│ │ ZTOExpress │ │ YTOExpress │     │
│  │ Adapter    │    │ Adapter    │ │ Adapter    │ │ Adapter    │     │
│  ├────────────┤    ├────────────┤ ├────────────┤ ├────────────┤     │
│  │Translates  │    │Translates  │ │Translates  │ │Translates  │     │
│  │to SF API   │    │to JD API   │ │to ZTO API  │ │to YTO API  │     │
│  │format      │    │format      │ │format      │ │format      │     │
│  └─────┬──────┘    └─────┬──────┘ └─────┬──────┘ └─────┬──────┘     │
│        │                 │              │              │            │
│        ▼                 ▼              ▼              ▼            │
│  ┌────────────┐    ┌────────────┐ ┌────────────┐ ┌────────────┐     │
│  │ SF Express │    │ JD         │ │ ZTO        │ │ YTO        │     │
│  │ REST API   │    │ REST API   │ │ REST API   │ │ REST API   │     │
│  │ (External) │    │ (External) │ │ (External) │ │ (External) │     │
│  └────────────┘    └────────────┘ └────────────┘ └────────────┘     │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Factory Pattern for Adapter Creation

```
┌─────────────────────────────────────────────────────────────────────┐
│                       FACTORY PATTERN                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    CarrierAdapterFactory                    │    │
│  ├─────────────────────────────────────────────────────────────┤    │
│  │  + GetAdapter(carrierCode: string) : ICarrierAdapter        │    │
│  │  + GetAllAdapters() : IEnumerable<ICarrierAdapter>          │    │
│  │  + RegisterAdapter(code: string, adapter: ICarrierAdapter)  │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                  │                                  │
│                                  │ creates                          │
│         ┌────────────────────────┼──────────────────────┐           │
│         │           │            │           │          │           │
│         ▼           ▼            ▼           ▼          ▼           │
│   "SF" → SFAdapter  "JD" → JDAdapter  "ZTO" → ZTOAdapter ...        │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Why These Patterns?

| Pattern | Benefit | Logistics Application |
|---------|---------|----------------------|
| **Adapter** | Unifies incompatible interfaces | Each carrier has unique API |
| **Factory** | Centralizes object creation | Easy carrier addition |
| **Strategy** | Runtime algorithm selection | Rate selection logic |
| **Chain of Responsibility** | Fallback handling | Booking retry logic |

### Design Pattern Ledger

| **Pattern Name** | **Application Location** (Class/Module) | **Core Problem Solved** | **Implementation Summary** | **Reusable Components** |
| :--- | :--- | :--- | :--- | :--- |
| **Adapter Pattern** | `SFExpressAdapter`, `JDLogisticsAdapter` | Unify carrier interfaces with 8 different API specifications | Implement `ICarrierAdapter`, internally map request/response formats | `BaseCarrierAdapter` (abstract base class) |
| **Factory Pattern** | `CarrierAdapterFactory` | Dynamically create and provide concrete adapter instances | Registry pattern, resolve all adapters from DI container | `ICarrierAdapterFactory` interface and default implementation |
| **Strategy Pattern** | `CarrierSelectionService` | Select optimal carrier based on cost, delivery time, etc. | Define `ISelectionStrategy`, encapsulate different selection algorithms | `CheapestRateStrategy`, `FastestDeliveryStrategy` |
| **Chain of Responsibility Pattern** | `BookingWithFallbackService` | Automatically retry fallback carriers when primary carrier booking fails | Call adapters in priority order until success | `FallbackHandler` chain processor |
| **Decorator Pattern** | `CachedCarrierAdapter` | Add caching to rate queries to reduce external API calls | Wrap `ICarrierAdapter`, implement caching logic | `CachingAdapterDecorator` |

---

## 🧠 Pattern Deep Dive Analysis (模式深度分析)

> **Study Focus**: Understand WHY these patterns work together  
> **Goal**: Be able to design similar integrations independently

### Adapter Pattern Mechanics

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ADAPTER PATTERN - THE PROBLEM                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  WITHOUT ADAPTER (每个承运商有不同的接口):                                    │
│  ────────────────────────────────────────                                   │
│                                                                              │
│  SF Express API:                                                             │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │  POST /v2/order/create                                        │          │
│  │  {                                                            │          │
│  │    "partnerID": "SF001",                                      │          │
│  │    "requestID": "REQ123",                                     │          │
│  │    "serviceList": [                                           │          │
│  │      { "serviceCode": "COD", "value": 100 }                   │          │
│  │    ],                                                         │          │
│  │    "cargoDetails": { "name": "商品", "count": 1 }            │          │
│  │  }                                                            │          │
│  └──────────────────────────────────────────────────────────────┘          │
│                                                                              │
│  JD Logistics API:                                                           │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │  POST /api/order/submit                                       │          │
│  │  {                                                            │          │
│  │    "customerCode": "JD_CUST_001",                            │          │
│  │    "orderInfo": {                                             │          │
│  │      "orderId": "ORD123",                                     │          │
│  │      "orderType": 1,                                          │          │
│  │      "senderInfo": { ... },                                   │          │
│  │      "receiverInfo": { ... }                                  │          │
│  │    },                                                         │          │
│  │    "packageInfo": [ { "weight": 1.5, "volume": 0.01 } ]      │          │
│  │  }                                                            │          │
│  └──────────────────────────────────────────────────────────────┘          │
│                                                                              │
│  ZTO Express API:                                                            │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │  POST /openapi/order/create                                   │          │
│  │  {                                                            │          │
│  │    "order_code": "ZTO_123",                                   │          │
│  │    "sender": { "name": "...", "mobile": "..." },             │          │
│  │    "receiver": { "name": "...", "tel": "..." },  // Note: mobile vs tel │
│  │    "goods_type": "document"                                   │          │
│  │  }                                                            │          │
│  └──────────────────────────────────────────────────────────────┘          │
│                                                                              │
│  PROBLEM: 每个承运商的字段名、结构、认证方式都不同!                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Adapter Solution Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ADAPTER PATTERN - THE SOLUTION                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  OUR UNIFIED MODEL:                                                          │
│  ──────────────────                                                         │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  BookingRequest (我们的统一模型)                                 │        │
│  │  {                                                              │        │
│  │    ShipmentId: Guid,                                           │        │
│  │    CarrierCode: "SF" | "JD" | "ZTO",                           │        │
│  │    Sender: ContactInfo,     // Unified contact structure       │        │
│  │    Recipient: ContactInfo,  // Same structure for all         │        │
│  │    Packages: List<PackageInfo>                                 │        │
│  │  }                                                              │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                    │                                         │
│                                    │ CarrierService.BookAsync(request)       │
│                                    ▼                                         │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │               ICarrierAdapterFactory.GetAdapter("SF")           │        │
│  │                            │                                    │        │
│  │                            ▼                                    │        │
│  │                    SFExpressAdapter                             │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                    │                                         │
│                                    │ Adapter TRANSLATES                      │
│                                    ▼                                         │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  SFOrderRequest (顺丰API需要的格式)                             │        │
│  │  {                                                              │        │
│  │    partnerID: MapFromConfig("SF_PARTNER_ID"),                  │        │
│  │    requestID: GenerateUUID(),                                  │        │
│  │    cargoDetails: {                                             │        │
│  │      name: request.Packages[0].Description,                    │        │
│  │      count: request.Packages.Count                             │        │
│  │    },                                                          │        │
│  │    // ... translate all fields                                 │        │
│  │  }                                                              │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                    │                                         │
│                                    │ HTTP POST to SF API                     │
│                                    ▼                                         │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  SFOrderResponse → Adapter TRANSLATES BACK → BookingResult     │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                                                              │
│  BENEFIT: CarrierService doesn't know about SF/JD/ZTO specifics!           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Adapter vs Facade vs Proxy (Pattern Comparison)

| Pattern | Intent | Use When | Logistics Example |
|---------|--------|----------|-------------------|
| **Adapter** | Convert interface A to interface B | Integrating external systems with different contracts | SF API → ICarrierAdapter |
| **Facade** | Simplify complex subsystem | Multiple internal services to coordinate | ShippingFacade wrapping Order+Carrier+Tracking |
| **Proxy** | Control access to object | Caching, logging, access control | CachedCarrierAdapter wrapping real adapter |

### Factory Pattern Deep Dive

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FACTORY PATTERN VARIATIONS                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  SIMPLE FACTORY (最简单):                                                    │
│  ─────────────────────────                                                  │
│  ┌────────────────────────────────────────────────────────────────┐         │
│  │  public class CarrierAdapterFactory                             │        │
│  │  {                                                              │        │
│  │      public ICarrierAdapter GetAdapter(string code)             │        │
│  │      {                                                          │        │
│  │          return code switch                                     │        │
│  │          {                                                      │        │
│  │              "SF" => new SFExpressAdapter(),                    │        │
│  │              "JD" => new JDLogisticsAdapter(),                  │        │
│  │              "ZTO" => new ZTOExpressAdapter(),                  │        │
│  │              _ => throw new CarrierNotSupportedException()      │        │
│  │          };                                                     │        │
│  │      }                                                          │        │
│  │  }                                                              │        │
│  │                                                                 │        │
│  │  ❌ Problem: Must modify factory when adding new carrier        │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                                                              │
│  REGISTRY-BASED FACTORY (推荐):                                             │
│  ─────────────────────────────                                              │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  public class CarrierAdapterFactory : ICarrierAdapterFactory    │        │
│  │  {                                                              │        │
│  │      private readonly Dictionary<string, ICarrierAdapter>       │        │
│  │          _adapters;                                             │        │
│  │                                                                 │        │
│  │      public CarrierAdapterFactory(                              │        │
│  │          IEnumerable<ICarrierAdapter> adapters) // DI注入所有    │        │
│  │      {                                                          │        │
│  │          _adapters = adapters.ToDictionary(a => a.CarrierCode); │        │
│  │      }                                                          │        │
│  │                                                                 │        │
│  │      public ICarrierAdapter GetAdapter(string code)             │        │
│  │          => _adapters.TryGetValue(code, out var adapter)        │        │
│  │              ? adapter                                          │        │
│  │              : throw new CarrierNotSupportedException(code);    │        │
│  │  }                                                              │        │
│  │                                                                 │        │
│  │  ✅ Benefit: New carriers registered in DI, factory unchanged  │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                                                              │
│  DI REGISTRATION:                                                            │
│  ─────────────────                                                          │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  // In Program.cs / Startup.cs                                  │        │
│  │  services.AddTransient<ICarrierAdapter, SFExpressAdapter>();   │        │
│  │  services.AddTransient<ICarrierAdapter, JDLogisticsAdapter>(); │        │
│  │  services.AddTransient<ICarrierAdapter, ZTOExpressAdapter>();  │        │
│  │                                                                 │        │
│  │  // 新增承运商只需要加这一行:                                     │        │
│  │  services.AddTransient<ICarrierAdapter, NewCarrierAdapter>();  │        │
│  │                                                                 │        │
│  │  services.AddSingleton<ICarrierAdapterFactory,                 │        │
│  │      CarrierAdapterFactory>();                                  │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Chain of Responsibility for Fallback

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FALLBACK CHAIN PATTERN                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SCENARIO: Primary carrier (SF) is down, need automatic fallback            │
│  场景: 首选承运商(顺丰)故障，需要自动切换                                      │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      BOOKING REQUEST                                 │   │
│  │                   (Priority: SF → JD → ZTO)                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  TRY 1: SFExpressAdapter.BookAsync()                                │   │
│  │                                                                      │   │
│  │  Result: ❌ CarrierApiException (API timeout)                       │   │
│  │  Action: Log error, continue to next in chain                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  TRY 2: JDLogisticsAdapter.BookAsync()                              │   │
│  │                                                                      │   │
│  │  Result: ❌ BookingFailedException (capacity full)                  │   │
│  │  Action: Log error, continue to next in chain                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  TRY 3: ZTOExpressAdapter.BookAsync()                               │   │
│  │                                                                      │   │
│  │  Result: ✅ BookingResult { TrackingNo: "ZTO1234567890" }           │   │
│  │  Action: Return success, record which carrier was used              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  IMPLEMENTATION PSEUDOCODE:                                                  │
│  ──────────────────────────                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  public async Task<BookingResult> BookWithFallbackAsync(            │   │
│  │      BookingRequest request,                                        │   │
│  │      IEnumerable<string> carrierPriority)                          │   │
│  │  {                                                                   │   │
│  │      var exceptions = new List<Exception>();                        │   │
│  │                                                                      │   │
│  │      foreach (var carrierCode in carrierPriority)                   │   │
│  │      {                                                               │   │
│  │          try                                                        │   │
│  │          {                                                           │   │
│  │              var adapter = _factory.GetAdapter(carrierCode);        │   │
│  │              if (!await adapter.CheckHealthAsync().IsHealthy)       │   │
│  │                  continue; // Skip unhealthy carriers               │   │
│  │                                                                      │   │
│  │              var result = await adapter.BookShipmentAsync(request); │   │
│  │              result.FallbackUsed = carrierCode != carrierPriority.First();│
│  │              return result;                                         │   │
│  │          }                                                           │   │
│  │          catch (Exception ex)                                       │   │
│  │          {                                                           │   │
│  │              _logger.LogWarning(ex, "Carrier {Code} failed",        │   │
│  │                  carrierCode);                                      │   │
│  │              exceptions.Add(ex);                                    │   │
│  │          }                                                           │   │
│  │      }                                                               │   │
│  │                                                                      │   │
│  │      throw new AllCarriersFailedException(exceptions);              │   │
│  │  }                                                                   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## � Enterprise Architecture Comparison (企业架构对比)

> **Study Focus**: How do top Chinese logistics platforms implement multi-carrier integration?  
> **Learning Goal**: Understand why different architectural choices suit different business scales

### How Top Players Implement Carrier Integration

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│              CARRIER INTEGRATION ARCHITECTURE COMPARISON                            │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌────────────────────────────────────────────────────────────────────────────────┐ │
│  │                     CAINIAO LOGISTICS PLATFORM (菜鸟物流平台)                   │ │
│  │                         ~25 million orders/day                                 │ │
│  ├────────────────────────────────────────────────────────────────────────────────┤ │
│  │                                                                                │ │
│  │  ARCHITECTURE: Platform-Level Aggregation                                      │ │
│  │  ──────────────────────────────────────────                                    │ │
│  │                                                                                ││
│  │  ┌─────────────────────────────────────────────────────────────┐              ││
│  │  │                    Cainiao Open Platform                     │              ││
│  │  │              (Unified API Gateway Layer)                     │              ││
│  │  └───────────────────────────┬─────────────────────────────────┘              ││
│  │                              │                                                 ││
│  │         ┌────────────────────┼────────────────────┐                           ││
│  │         │                    │                    │                           ││
│  │         ▼                    ▼                    ▼                           ││
│  │  ┌────────────┐      ┌────────────┐      ┌────────────┐                       ││
│  │  │ 中通 (ZTO) │      │ 圆通 (YTO) │      │ 韵达 (Yunda)│   ... 15+ carriers   ││
│  │  └────────────┘      └────────────┘      └────────────┘                       ││
│  │                                                                                ││
│  │  KEY DECISIONS:                                                                ││
│  │  • Single unified API for ALL carriers (merchants call Cainiao only)          ││
│  │  • Cainiao handles carrier negotiation, SLA enforcement                       ││
│  │  • Dynamic routing based on price/capacity/SLA                                ││
│  │  • Centralized tracking aggregation                                           ││
│  │                                                                                ││
│  │  WHY THIS WORKS AT SCALE:                                                      ││
│  │  ✅ Merchants integrate once, access all carriers                             ││
│  │  ✅ Cainiao has bargaining power for better rates                             ││
│  │  ✅ Centralized monitoring and quality control                                ││
│  │  ❌ Single point of dependency (Cainiao platform)                             ││
│  │  ❌ Less flexibility for carrier-specific features                            ││
│  │                                                                                ││
│  └────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                      │
│  ┌────────────────────────────────────────────────────────────────────────────────┐│
│  │                      JD LOGISTICS (京东物流)                                    ││
│  │                        ~8 million orders/day                                    ││
│  ├────────────────────────────────────────────────────────────────────────────────┤│
│  │                                                                                ││
│  │  ARCHITECTURE: Self-Operated with Selective Partners                          ││
│  │  ─────────────────────────────────────────────                                ││
│  │                                                                                ││
│  │  ┌─────────────────────────────────────────────────────────────┐              ││
│  │  │               JD Order Management System                     │              ││
│  │  └───────────────────────────┬─────────────────────────────────┘              ││
│  │                              │                                                 ││
│  │              ┌───────────────┼────────────────┐                               ││
│  │              │               │                │                               ││
│  │              ▼               ▼                ▼                               ││
│  │       ┌────────────┐ ┌────────────┐   ┌────────────┐                          ││
│  │       │ JD Express │ │JD Cold Chain│  │ 3rd Party │                          ││
│  │       │ (Self-Op)  │ │ (Self-Op)  │   │  Partners │                          ││
│  │       │   80%      │ │   5%       │   │    15%    │                          ││
│  │       └────────────┘ └────────────┘   └────────────┘                          ││
│  │                                              │                                 ││
│  │                           ┌──────────────────┴──────────────────┐             ││
│  │                           │                                     │             ││
│  │                           ▼                                     ▼             ││
│  │                   ┌─────────────┐                       ┌─────────────┐       ││
│  │                   │ ZTO/YTO     │                       │ SF Express  │       ││
│  │                   │ (Overflow)  │                       │ (Express)   │       ││
│  │                   └─────────────┘                       └─────────────┘       ││
│  │                                                                                ││
│  │  KEY DECISIONS:                                                                ││
│  │  • Primarily use own logistics network (controlled quality)                   ││
│  │  • Partner carriers for overflow/special routes                               ││
│  │  • Deep integration for JD carriers, adapter for 3rd party                   ││
│  │                                                                                ││
│  │  WHY THIS WORKS:                                                               ││
│  │  ✅ End-to-end quality control for 80% of shipments                          ││
│  │  ✅ Flexibility to use partners when needed                                   ││
│  │  ✅ Competitive advantage through delivery speed                              ││
│  │  ❌ Higher operational cost than pure platform model                          ││
│  │                                                                                ││
│  └────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                      │
│  ┌────────────────────────────────────────────────────────────────────────────────┐│
│  │                     SF EXPRESS (顺丰速运)                                       ││
│  │                       ~15 million orders/day                                    ││
│  ├────────────────────────────────────────────────────────────────────────────────┤│
│  │                                                                                ││
│  │  ARCHITECTURE: Full Self-Operation with Premium Focus                         ││
│  │  ────────────────────────────────────────────────                             ││
│  │                                                                                ││
│  │  ┌─────────────────────────────────────────────────────────────┐              ││
│  │  │              SF Order Management System                      │              ││
│  │  └───────────────────────────┬─────────────────────────────────┘              ││
│  │                              │                                                 ││
│  │         ┌────────────────────┼────────────────────┐                           ││
│  │         │                    │                    │                           ││
│  │         ▼                    ▼                    ▼                           ││
│  │  ┌────────────┐      ┌────────────┐      ┌────────────┐                       ││
│  │  │ SF Express │      │ SF Cold    │      │ SF Heavy   │                       ││
│  │  │ (时效件)   │      │ (冷链)      │      │ (重货)     │                       ││
│  │  │   70%      │      │   10%      │      │   20%      │                       ││
│  │  └────────────┘      └────────────┘      └────────────┘                       ││
│  │         │                    │                    │                           ││
│  │         └────────────────────┼────────────────────┘                           ││
│  │                              │                                                 ││
│  │                              ▼                                                 ││
│  │  ┌─────────────────────────────────────────────────────────────┐              ││
│  │  │     ALL SF-Owned Infrastructure (planes, trucks, hubs)      │              ││
│  │  └─────────────────────────────────────────────────────────────┘              ││
│  │                                                                                ││
│  │  KEY DECISIONS:                                                                ││
│  │  • 100% self-operated (no third-party carriers)                               ││
│  │  • Premium pricing = Premium quality guarantee                                ││
│  │  • Internal multi-service adapters, NOT multi-carrier adapters               ││
│  │                                                                                ││
│  │  WHY THIS WORKS:                                                               ││
│  │  ✅ Consistent service quality across all shipments                           ││
│  │  ✅ Premium brand positioning                                                  ││
│  │  ✅ No dependency on external carrier reliability                             ││
│  │  ❌ Limited price competitiveness for economy segments                        ││
│  │                                                                                ││
│  └────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Architecture Decision Matrix

**When to use which integration architecture?**

| Factor | Platform Aggregation (菜鸟模式) | Hybrid (京东模式) | Self-Operated (顺丰模式) |
|--------|-------------------------------|------------------|------------------------|
| **Business Scale** | 10M+ orders/day | 1M-10M orders/day | Any (quality-focused) |
| **Integration Cost** | ¥1-2M initial | ¥3-5M initial | ¥10M+ initial |
| **Carrier Count** | 15+ carriers | 5-10 carriers | 1 carrier (self) |
| **Quality Control** | Delegated to platform | Partial control | Full control |
| **Rate Negotiation** | Platform handles | Direct with carriers | Own pricing |
| **Time to Market** | 1-2 months | 3-6 months | 12+ months |
| **Flexibility** | Low (platform dependent) | Medium | High (own roadmap) |
| **Suitable For** | E-commerce platforms | Retail chains | Premium services |

### DT-Express Recommended Approach

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                   RECOMMENDED: HYBRID ADAPTER ARCHITECTURE                           │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  FOR LEARNING: Implement the Hybrid model (similar to JD but smaller scale)         │
│                                                                                      │
│                ┌────────────────────────────────────────────┐                       │
│                │         DT-Express Core System             │                       │
│                │                                            │                       │
│                │  ┌──────────────────────────────────────┐ │                       │
│                │  │     ICarrierAdapterFactory            │ │                       │
│                │  │     (Registry-based, DI-enabled)      │ │                       │
│                │  └───────────────┬──────────────────────┘ │                       │
│                │                  │                         │                       │
│                └──────────────────┼─────────────────────────┘                       │
│                                   │                                                  │
│       ┌───────────────────────────┼───────────────────────────┐                     │
│       │                           │                           │                     │
│       ▼                           ▼                           ▼                     │
│  ┌──────────┐              ┌──────────┐              ┌──────────┐                   │
│  │ Primary  │              │ Economy  │              │ Specialty│                   │
│  │ Carriers │              │ Carriers │              │ Carriers │                   │
│  ├──────────┤              ├──────────┤              ├──────────┤                   │
│  │ SF       │              │ ZTO      │              │ SF Cold  │                   │
│  │ JD       │              │ YTO      │              │ JD COD   │                   │
│  └──────────┘              │ Yunda    │              └──────────┘                   │
│                            └──────────┘                                              │
│                                                                                      │
│  RATIONALE:                                                                          │
│  • Learns enterprise patterns without platform dependency                           │
│  • Direct carrier API integration = deeper understanding                            │
│  • Supports 5-8 carriers (manageable for learning)                                  │
│  • Adapter pattern makes adding carriers straightforward                            │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Adapter vs Facade vs Proxy: Deep Selection Guide (模式选择深度指南)

> **Study Focus**: These three patterns look similar but solve DIFFERENT problems  
> **Learning Goal**: Know exactly which pattern to apply in which situation

### Pattern Selection Decision Flow

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                   STRUCTURAL PATTERN DECISION FLOW                                   │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│                        ┌─────────────────────────┐                                  │
│                        │ Do you need to connect  │                                  │
│                        │ with external system?   │                                  │
│                        └───────────┬─────────────┘                                  │
│                                    │                                                 │
│                   ┌────────────────┼────────────────┐                               │
│                   │                │                │                               │
│                   ▼ YES            │                ▼ NO                            │
│  ┌────────────────────────┐       │   ┌────────────────────────┐                   │
│  │ Is the external API    │       │   │ Do you need to add     │                   │
│  │ incompatible with your │       │   │ behavior (caching,     │                   │
│  │ interface?             │       │   │ logging, auth)?        │                   │
│  └──────────┬─────────────┘       │   └──────────┬─────────────┘                   │
│             │                     │              │                                  │
│    ┌────────┼────────┐            │     ┌────────┼────────┐                        │
│    │ YES    │        │ NO         │     │ YES    │        │ NO                     │
│    ▼        │        ▼            │     ▼        │        ▼                        │
│ ┌──────┐    │    ┌────────┐       │  ┌──────┐    │    ┌────────────┐              │
│ │ADAPT-│    │    │ Direct │       │  │PROXY │    │    │ Do you need│              │
│ │ER    │    │    │ call   │       │  │      │    │    │ to simplify│              │
│ │      │    │    │ works  │       │  │      │    │    │ subsystem? │              │
│ └──────┘    │    └────────┘       │  └──────┘    │    └─────┬──────┘              │
│             │                     │              │          │                       │
│             │                     │              │   ┌──────┼──────┐               │
│             │                     │              │   │ YES  │      │ NO            │
│             │                     │              │   ▼      │      ▼               │
│             │                     │              │ ┌──────┐ │  ┌──────────┐        │
│             │                     │              │ │FACADE│ │  │Other     │        │
│             │                     │              │ │      │ │  │patterns  │        │
│             │                     │              │ └──────┘ │  └──────────┘        │
│             │                     │              │          │                       │
│             │                     │              │          │                       │
│             └─────────────────────┴──────────────┴──────────┘                       │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Detailed Pattern Comparison with Logistics Examples

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                          ADAPTER PATTERN (适配器模式)                                │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  INTENT: Convert interface of class A to interface B that client expects            │
│  意图: 将A类的接口转换为客户端期望的B接口                                            │
│                                                                                      │
│  LOGISTICS USE CASE: SF Express uses different field names than our model           │
│  物流场景: 顺丰API使用的字段名与我们系统模型不同                                       │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  OUR MODEL:                        SF EXPRESS API:                          │   │
│  │  ────────────                      ───────────────                          │   │
│  │  ContactInfo {                     SFContact {                              │   │
│  │    FullName: string       →        contact: string                          │   │
│  │    PhoneNumber: string    →        tel: string                              │   │
│  │    Province: string       →        province: string                         │   │
│  │    City: string           →        city: string                             │   │
│  │    District: string       →        county: string  // Different name!       │   │
│  │    AddressLine: string    →        address: string                          │   │
│  │  }                                 }                                         │   │
│  │                                                                              │   │
│  │  ADAPTER TRANSLATES:                                                        │   │
│  │  ─────────────────────                                                      │   │
│  │  SFContact ToSFContact(ContactInfo contact)                                 │   │
│  │  {                                                                           │   │
│  │      return new SFContact {                                                 │   │
│  │          contact = contact.FullName,      // FullName → contact             │   │
│  │          tel = contact.PhoneNumber,       // PhoneNumber → tel              │   │
│  │          province = contact.Province,                                       │   │
│  │          city = contact.City,                                               │   │
│  │          county = contact.District,       // District → county              │   │
│  │          address = contact.AddressLine                                      │   │
│  │      };                                                                     │   │
│  │  }                                                                           │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  STRUCTURE:                                                                          │
│  ┌────────────┐      ┌────────────────┐      ┌────────────────┐                    │
│  │ Client     │─────▶│ ICarrierAdapter│◀─────│ SFExpressAdapter│                   │
│  │ (Our Code) │      │ (Target)       │      │ (Adapter)       │                   │
│  └────────────┘      └────────────────┘      └───────┬────────┘                    │
│                                                       │                             │
│                                                       ▼                             │
│                                              ┌────────────────┐                    │
│                                              │ SF Express API │                    │
│                                              │ (Adaptee)      │                    │
│                                              └────────────────┘                    │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           FACADE PATTERN (外观模式)                                  │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  INTENT: Simplify complex subsystem by providing unified entry point                │
│  意图: 为复杂子系统提供简化的统一入口                                                 │
│                                                                                      │
│  LOGISTICS USE CASE: Single method to handle complete shipping process              │
│  物流场景: 一个方法处理完整的发货流程                                                 │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  WITHOUT FACADE (Client must coordinate multiple services):                  │   │
│  │  ─────────────────────────────────────────────────────────                  │   │
│  │                                                                              │   │
│  │  // Client code is complex                                                  │   │
│  │  var route = await _routeService.CalculateRouteAsync(origin, dest);         │   │
│  │  var rates = await _carrierService.GetRatesAsync(route, shipment);          │   │
│  │  var bestCarrier = await _selectionService.SelectBestAsync(rates);          │   │
│  │  var booking = await _carrierService.BookAsync(bestCarrier, shipment);      │   │
│  │  var label = await _labelService.GenerateAsync(booking);                    │   │
│  │  await _inventoryService.DeductStockAsync(shipment.Items);                  │   │
│  │  await _notificationService.NotifyCustomerAsync(booking.TrackingNo);        │   │
│  │                                                                              │   │
│  │  WITH FACADE (Single call):                                                 │   │
│  │  ─────────────────────────                                                  │   │
│  │                                                                              │   │
│  │  var result = await _shippingFacade.ProcessShipmentAsync(shipment);         │   │
│  │  // Facade internally coordinates: Route → Rate → Book → Label → Notify    │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  STRUCTURE:                                                                          │
│  ┌────────────┐      ┌────────────────────────────────────┐                        │
│  │ Client     │─────▶│        ShippingFacade              │                        │
│  │            │      │  + ProcessShipmentAsync()          │                        │
│  └────────────┘      └────────────────┬───────────────────┘                        │
│                                        │                                            │
│           ┌────────────────────────────┼────────────────────────────┐              │
│           │              │             │             │              │              │
│           ▼              ▼             ▼             ▼              ▼              │
│     ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│     │ Route    │  │ Carrier  │  │ Label    │  │ Inventory│  │ Notify   │         │
│     │ Service  │  │ Service  │  │ Service  │  │ Service  │  │ Service  │         │
│     └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘         │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────┐
│                            PROXY PATTERN (代理模式)                                  │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  INTENT: Control access to object by adding behavior (caching, auth, logging)       │
│  意图: 通过添加行为(缓存、认证、日志)来控制对象访问                                    │
│                                                                                      │
│  LOGISTICS USE CASE: Cache carrier rates, add logging around API calls              │
│  物流场景: 缓存承运商报价，为API调用添加日志                                          │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  CACHING PROXY EXAMPLE:                                                     │   │
│  │  ─────────────────────────                                                  │   │
│  │                                                                              │   │
│  │  public class CachingCarrierAdapterProxy : ICarrierAdapter                  │   │
│  │  {                                                                           │   │
│  │      private readonly ICarrierAdapter _realAdapter;                         │   │
│  │      private readonly IDistributedCache _cache;                             │   │
│  │                                                                              │   │
│  │      public async Task<CarrierQuote> GetRateAsync(RateRequest request)      │   │
│  │      {                                                                       │   │
│  │          string cacheKey = $"rate:{CarrierCode}:{request.GetHashCode()}";   │   │
│  │                                                                              │   │
│  │          // Try cache first                                                 │   │
│  │          var cached = await _cache.GetStringAsync(cacheKey);                │   │
│  │          if (cached != null)                                                │   │
│  │              return JsonSerializer.Deserialize<CarrierQuote>(cached);       │   │
│  │                                                                              │   │
│  │          // Cache miss - call real adapter                                  │   │
│  │          var quote = await _realAdapter.GetRateAsync(request);              │   │
│  │                                                                              │   │
│  │          // Store in cache for 15 minutes                                   │   │
│  │          await _cache.SetStringAsync(cacheKey,                              │   │
│  │              JsonSerializer.Serialize(quote),                               │   │
│  │              new DistributedCacheEntryOptions {                             │   │
│  │                  AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) │   │
│  │              });                                                            │   │
│  │                                                                              │   │
│  │          return quote;                                                      │   │
│  │      }                                                                       │   │
│  │  }                                                                           │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  STRUCTURE:                                                                          │
│  ┌────────────┐      ┌────────────────────┐      ┌────────────────────┐            │
│  │ Client     │─────▶│ ICarrierAdapter    │◀─────│ CachingProxy       │            │
│  │            │      │ (Subject)          │      │ (Proxy)            │            │
│  └────────────┘      └────────────────────┘      └─────────┬──────────┘            │
│                                                             │                       │
│                                                             │ delegates to          │
│                                                             ▼                       │
│                                              ┌────────────────────┐                 │
│                                              │ SFExpressAdapter   │                 │
│                                              │ (Real Subject)     │                 │
│                                              └────────────────────┘                 │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Summary: When to Use Each Pattern

| Scenario | Pattern | Example in Carrier Integration |
|----------|---------|-------------------------------|
| SF API uses different field names | **Adapter** | SFExpressAdapter translates BookingRequest → SFOrderRequest |
| JD uses OAuth 2.0, we use API Key | **Adapter** | JDAdapter handles OAuth flow internally |
| Need single method for shipping flow | **Facade** | ShippingFacade wraps route + carrier + label |
| Want to cache rate quotes | **Proxy** | CachingCarrierAdapterProxy wraps any adapter |
| Need to log all carrier API calls | **Proxy** | LoggingCarrierAdapterProxy wraps any adapter |
| Circuit breaker for carrier APIs | **Proxy** | CircuitBreakerProxy wraps any adapter |

---

## 💰 Why Rate Caching is CRITICAL for Production (为什么报价缓存至关重要)

> **Study Focus**: Rate APIs cost money and have rate limits  
> **Learning Goal**: Understand real production cost implications

### The Cost of NOT Caching

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    RATE API COST ANALYSIS (报价API成本分析)                          │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  SCENARIO: Mid-sized e-commerce with 50,000 orders/day                              │
│  场景: 中型电商，每天5万订单                                                          │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  WITHOUT CACHING:                                                           │   │
│  │  ────────────────                                                           │   │
│  │                                                                              │   │
│  │  Orders per day:                    50,000                                   │   │
│  │  Rate queries per order:            × 5 carriers                             │   │
│  │  ────────────────────────────────────────                                   │   │
│  │  Total API calls per day:           250,000 calls                           │   │
│  │                                                                              │   │
│  │  TYPICAL CARRIER API PRICING:                                               │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ Carrier    │ Free Tier │ Paid Rate      │ Rate Limit           │        │   │
│  │  ├────────────┼───────────┼────────────────┼──────────────────────┤        │   │
│  │  │ SF Express │ 1000/day  │ ¥0.02/call     │ 1000 QPS             │        │   │
│  │  │ JD         │ 500/day   │ ¥0.03/call     │ 500 QPS              │        │   │
│  │  │ ZTO        │ 2000/day  │ ¥0.01/call     │ 200 QPS              │        │   │
│  │  │ YTO        │ 1000/day  │ ¥0.015/call    │ 300 QPS              │        │   │
│  │  │ Yunda      │ 1500/day  │ ¥0.01/call     │ 250 QPS              │        │   │
│  │  └────────────┴───────────┴────────────────┴──────────────────────┘        │   │
│  │                                                                              │   │
│  │  DAILY COST CALCULATION (无缓存每日成本):                                    │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ SF: (50,000 - 1,000) × ¥0.02  = ¥980                          │        │   │
│  │  │ JD: (50,000 - 500) × ¥0.03    = ¥1,485                        │        │   │
│  │  │ ZTO: (50,000 - 2,000) × ¥0.01 = ¥480                          │        │   │
│  │  │ YTO: (50,000 - 1,000) × ¥0.015 = ¥735                         │        │   │
│  │  │ Yunda: (50,000 - 1,500) × ¥0.01 = ¥485                        │        │   │
│  │  │ ─────────────────────────────────────                          │        │   │
│  │  │ TOTAL DAILY COST: ¥4,165                                       │        │   │
│  │  │ MONTHLY COST: ¥124,950 (~$17,500 USD)                         │        │   │
│  │  │ YEARLY COST: ¥1,499,400 (~$210,000 USD)                       │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  WITH SMART CACHING:                                                        │   │
│  │  ───────────────────                                                        │   │
│  │                                                                              │   │
│  │  OBSERVATION: Many orders have SAME route patterns                          │
│  │  观察: 很多订单有相同的路由模式                                               │   │
│  │                                                                              │   │
│  │  Shanghai → Beijing:      8,000 orders/day (same rate applies)              │   │
│  │  Shenzhen → Guangzhou:    5,000 orders/day (same rate applies)              │   │
│  │  Hangzhou → Shanghai:     4,000 orders/day (same rate applies)              │   │
│  │  ... (top 100 routes cover 70% of orders)                                   │   │
│  │                                                                              │   │
│  │  CACHING STRATEGY:                                                          │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ Cache Key: {CarrierCode}:{OriginCity}:{DestCity}:{ServiceType} │        │   │
│  │  │ Cache TTL: 15 minutes (rates typically stable for hours)       │        │   │
│  │  │ Expected Hit Rate: 85-90%                                      │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  │  WITH 90% CACHE HIT RATE:                                                   │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ Original API calls:    250,000/day                             │        │   │
│  │  │ Cache hits (90%):      225,000 (FREE - from Redis)            │        │   │
│  │  │ Actual API calls:      25,000/day (10% cache miss)            │        │   │
│  │  │                                                                │        │   │
│  │  │ NEW DAILY COST:        ¥416 (vs ¥4,165)                       │        │   │
│  │  │ MONTHLY SAVINGS:       ¥112,470                                │        │   │
│  │  │ YEARLY SAVINGS:        ¥1,349,460 (~$189,000 USD)             │        │   │
│  │  │                                                                │        │   │
│  │  │ ROI: Redis cluster cost ~¥2,000/month                         │        │   │
│  │  │      Savings: ¥112,470/month                                  │        │   │
│  │  │      ROI: 5,600%                                              │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Rate Caching Implementation Strategy

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    MULTI-LEVEL RATE CACHING ARCHITECTURE                             │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                        CACHING LAYERS                                        │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│                        ┌─────────────────────┐                                      │
│                        │   Rate Request      │                                      │
│                        └──────────┬──────────┘                                      │
│                                   │                                                  │
│                                   ▼                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  LEVEL 1: In-Memory Cache (IMemoryCache)                                    │   │
│  │  ─────────────────────────────────────────                                  │   │
│  │  TTL: 5 minutes │ Size: 10,000 entries │ Hit Rate: 60%                      │   │
│  │  Purpose: Eliminate repeated requests within same server                    │   │
│  │  用途: 消除同一服务器内的重复请求                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                   │ Miss                                             │
│                                   ▼                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  LEVEL 2: Distributed Cache (Redis)                                         │   │
│  │  ─────────────────────────────────────                                      │   │
│  │  TTL: 15 minutes │ Size: 1M+ entries │ Hit Rate: 80%                        │   │
│  │  Purpose: Share cached rates across all server instances                    │   │
│  │  用途: 跨所有服务器实例共享缓存报价                                            │   │
│  │                                                                              │   │
│  │  KEY STRUCTURE:                                                              │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │  rate:{carrier}:{origin_city}:{dest_city}:{service}:{weight}   │        │   │
│  │  │                                                                 │        │   │
│  │  │  Example keys:                                                  │        │   │
│  │  │  rate:SF:上海:北京:EXPRESS:1.5                                  │        │   │
│  │  │  rate:JD:深圳:广州:STANDARD:0.5                                 │        │   │
│  │  │  rate:ZTO:杭州:武汉:ECONOMY:2.0                                 │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                   │ Miss                                             │
│                                   ▼                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  LEVEL 3: Pre-computed Rate Tables (Background Job)                         │   │
│  │  ─────────────────────────────────────────────────                          │   │
│  │  Refresh: Every 6 hours │ Coverage: Top 1000 routes │ Hit Rate: 95%         │   │
│  │  Purpose: Pre-warm cache with known popular routes                          │   │
│  │  用途: 用已知热门路线预热缓存                                                  │   │
│  │                                                                              │   │
│  │  BACKGROUND JOB:                                                            │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │  // Every 6 hours, refresh top routes                          │        │   │
│  │  │  var topRoutes = await _analytics.GetTopRoutesAsync(1000);     │        │   │
│  │  │  foreach (var route in topRoutes)                              │        │   │
│  │  │  {                                                              │        │   │
│  │  │      foreach (var carrier in _activeCarriers)                  │        │   │
│  │  │      {                                                          │        │   │
│  │  │          var rate = await _carrier.GetRateAsync(route);        │        │   │
│  │  │          await _cache.SetAsync(BuildKey(carrier, route), rate);│        │   │
│  │  │      }                                                          │        │   │
│  │  │  }                                                              │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                   │ Miss                                             │
│                                   ▼                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  LEVEL 4: Carrier API (Real-time call)                                      │   │
│  │  ─────────────────────────────────────                                      │   │
│  │  When all caches miss, call actual carrier API                              │   │
│  │  Then populate all cache levels for future requests                         │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Cache Invalidation Strategies

| Strategy | When to Use | Implementation |
|----------|-------------|----------------|
| **Time-based (TTL)** | Rates change infrequently | Set 15-30 minute expiry |
| **Event-based** | Carrier announces rate change | Subscribe to carrier webhooks |
| **Version-based** | Carrier API version changes | Include version in cache key |
| **Weight-bucket** | Rates vary by weight bracket | Cache by weight bracket (0-1kg, 1-5kg, etc.) |

---

## 🔓 Open/Closed Principle in Carrier Integration (开闭原则应用)

> **Study Focus**: How to add new carriers WITHOUT modifying existing code  
> **Learning Goal**: Design systems that are open for extension, closed for modification

### The Problem: Adding a New Carrier

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    ADDING NEW CARRIER: BEST EXPRESS (百世快递)                       │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  SCENARIO: Business wants to add Best Express as a new carrier option               │
│  场景: 业务需要添加百世快递作为新的承运商选项                                          │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  ❌ BAD APPROACH (Violates OCP):                                            │   │
│  │  ─────────────────────────────────                                          │   │
│  │                                                                              │   │
│  │  // Must modify existing CarrierService.cs                                  │   │
│  │  public async Task<CarrierQuote> GetRateAsync(string carrierCode, ...)      │   │
│  │  {                                                                           │   │
│  │      switch (carrierCode)                                                   │   │
│  │      {                                                                       │   │
│  │          case "SF":                                                         │   │
│  │              return await CallSFApi(...);                                   │   │
│  │          case "JD":                                                         │   │
│  │              return await CallJDApi(...);                                   │   │
│  │          case "ZTO":                                                        │   │
│  │              return await CallZTOApi(...);                                  │   │
│  │          case "BEST":  // ❌ Must add new case here!                        │   │
│  │              return await CallBestApi(...);                                 │   │
│  │          default:                                                           │   │
│  │              throw new NotSupportedException();                             │   │
│  │      }                                                                       │   │
│  │  }                                                                           │   │
│  │                                                                              │   │
│  │  PROBLEMS:                                                                  │   │
│  │  • Must modify CarrierService every time (risk of bugs)                    │   │
│  │  • Switch statement grows unbounded                                        │   │
│  │  • Testing requires re-testing entire service                              │   │
│  │  • Single file becomes "god class"                                         │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  ✅ GOOD APPROACH (Follows OCP):                                            │   │
│  │  ────────────────────────────────                                           │   │
│  │                                                                              │   │
│  │  STEP 1: Create new adapter (new file, no existing code touched)           │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │  // NEW FILE: BestExpressAdapter.cs                            │        │   │
│  │  │  public class BestExpressAdapter : ICarrierAdapter             │        │   │
│  │  │  {                                                              │        │   │
│  │  │      public string CarrierCode => "BEST";                       │        │   │
│  │  │      public string CarrierName => "百世快递";                   │        │   │
│  │  │                                                                 │        │   │
│  │  │      public async Task<CarrierQuote> GetRateAsync(...)         │        │   │
│  │  │      {                                                          │        │   │
│  │  │          // Best Express specific implementation               │        │   │
│  │  │          var bestRequest = MapTobestFormat(request);           │        │   │
│  │  │          var response = await _httpClient.PostAsync(...);      │        │   │
│  │  │          return MapFromBestFormat(response);                   │        │   │
│  │  │      }                                                          │        │   │
│  │  │      // ... other interface methods                            │        │   │
│  │  │  }                                                              │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  │  STEP 2: Register in DI (configuration change only)                        │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │  // In Program.cs - just add ONE line                          │        │   │
│  │  │  services.AddTransient<ICarrierAdapter, SFExpressAdapter>();   │        │   │
│  │  │  services.AddTransient<ICarrierAdapter, JDLogisticsAdapter>(); │        │   │
│  │  │  services.AddTransient<ICarrierAdapter, ZTOExpressAdapter>();  │        │   │
│  │  │  services.AddTransient<ICarrierAdapter, BestExpressAdapter>(); │ // NEW │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  │  STEP 3: Done! Factory automatically discovers new adapter                 │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │  // CarrierAdapterFactory - NO CHANGES NEEDED                  │        │   │
│  │  │  public CarrierAdapterFactory(IEnumerable<ICarrierAdapter> adapters)   │        │   │
│  │  │  {                                                              │        │   │
│  │  │      // DI automatically injects ALL registered adapters       │        │   │
│  │  │      _adapters = adapters.ToDictionary(a => a.CarrierCode);   │        │   │
│  │  │  }                                                              │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  │  BENEFITS:                                                                  │   │
│  │  • Zero changes to existing CarrierService, Factory                        │   │
│  │  • New adapter tested in isolation                                         │   │
│  │  • Can deploy adapter as separate assembly/package                         │   │
│  │  • Easy to disable carrier (remove DI registration)                        │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### OCP Compliance Checklist for Carrier Integration

| Checkpoint | OCP Compliant | Anti-Pattern |
|------------|---------------|--------------|
| Adding new carrier | Create new class implementing interface | Add case to switch statement |
| Removing carrier | Remove DI registration | Comment out code blocks |
| Changing carrier behavior | Modify only that adapter | Modify shared base class |
| Adding new API method | Extend interface, update all adapters | Add to existing method with flags |
| Carrier-specific feature | Use adapter-specific extension | Add carrier checks in service |

### Extension Points for Advanced Scenarios

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    EXTENSION POINTS FOR CARRIER ADAPTERS                             │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  EXTENSION POINT 1: Carrier-Specific Services                               │   │
│  │  ───────────────────────────────────────────                                │   │
│  │                                                                              │   │
│  │  // Base interface for all carriers                                         │   │
│  │  public interface ICarrierAdapter                                           │   │
│  │  {                                                                           │   │
│  │      Task<CarrierQuote> GetRateAsync(RateRequest request);                  │   │
│  │      Task<BookingResult> BookShipmentAsync(BookingRequest request);         │   │
│  │      // ... standard methods                                                │   │
│  │  }                                                                           │   │
│  │                                                                              │   │
│  │  // Extension interface for carriers supporting COD                         │   │
│  │  public interface ICODCapableCarrier : ICarrierAdapter                      │   │
│  │  {                                                                           │   │
│  │      Task<decimal> GetCODFeeAsync(decimal codAmount);                       │   │
│  │      Task<bool> SetCODCollectionAsync(string trackingNo, decimal amount);   │   │
│  │  }                                                                           │   │
│  │                                                                              │   │
│  │  // Extension interface for carriers supporting cold chain                  │   │
│  │  public interface IColdChainCarrier : ICarrierAdapter                       │   │
│  │  {                                                                           │   │
│  │      Task<TemperatureLog[]> GetTemperatureLogsAsync(string trackingNo);     │   │
│  │      Task<bool> SetTemperatureRangeAsync(decimal min, decimal max);         │   │
│  │  }                                                                           │   │
│  │                                                                              │   │
│  │  // JD implements both base and COD                                         │   │
│  │  public class JDLogisticsAdapter : ICarrierAdapter, ICODCapableCarrier      │   │
│  │  {                                                                           │   │
│  │      // Implements all required methods                                     │   │
│  │  }                                                                           │   │
│  │                                                                              │   │
│  │  // SF implements base, COD, and cold chain                                 │   │
│  │  public class SFExpressAdapter :                                            │   │
│  │      ICarrierAdapter, ICODCapableCarrier, IColdChainCarrier                 │   │
│  │  {                                                                           │   │
│  │      // Implements all required methods                                     │   │
│  │  }                                                                           │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  EXTENSION POINT 2: Decorator Chain for Cross-Cutting Concerns              │   │
│  │  ─────────────────────────────────────────────────────────────              │   │
│  │                                                                              │   │
│  │  // Can wrap any adapter without modifying it                               │   │
│  │                                                                              │   │
│  │  Original Request                                                           │   │
│  │       │                                                                     │   │
│  │       ▼                                                                     │   │
│  │  ┌─────────────────┐                                                       │   │
│  │  │ LoggingDecorator │  // Logs all calls                                   │   │
│  │  └────────┬────────┘                                                       │   │
│  │           ▼                                                                 │   │
│  │  ┌─────────────────┐                                                       │   │
│  │  │ CachingDecorator │  // Caches rate queries                              │   │
│  │  └────────┬────────┘                                                       │   │
│  │           ▼                                                                 │   │
│  │  ┌─────────────────────┐                                                   │   │
│  │  │ CircuitBreakerProxy │  // Prevents cascade failures                     │   │
│  │  └────────┬────────────┘                                                   │   │
│  │           ▼                                                                 │   │
│  │  ┌─────────────────┐                                                       │   │
│  │  │ RetryDecorator   │  // Handles transient failures                       │   │
│  │  └────────┬────────┘                                                       │   │
│  │           ▼                                                                 │   │
│  │  ┌─────────────────┐                                                       │   │
│  │  │ SFExpressAdapter │  // Actual carrier call                              │   │
│  │  └─────────────────┘                                                       │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## �🏭 Chinese Logistics Industry Context (中国物流行业背景)

### Major Carrier API Characteristics

| Carrier | API Style | Auth Method | Rate Limit | Specialty |
|---------|-----------|-------------|------------|-----------|
| **顺丰 (SF)** | REST + Webhook | API Key + HMAC Sign | 1000 QPS | 高端时效件 |
| **京东物流 (JD)** | REST | OAuth 2.0 | 500 QPS | 电商仓配一体 |
| **中通 (ZTO)** | REST | API Key | 200 QPS | 网络覆盖广 |
| **圆通 (YTO)** | REST | API Key + Sign | 300 QPS | 价格优势 |
| **菜鸟 (Cainiao)** | REST | 淘宝开放平台 | Variable | 平台聚合 |

### SF Express Integration Notes (顺丰对接要点)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SF EXPRESS INTEGRATION SPECIFICS                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  AUTHENTICATION (签名认证):                                                   │
│  ──────────────────────────                                                 │
│  1. Concatenate: requestBody + timestamp + checkword (校验码)               │
│  2. MD5 hash the concatenated string                                        │
│  3. Base64 encode the hash                                                  │
│  4. Put in X-Signature header                                               │
│                                                                              │
│  SIGNATURE EXAMPLE:                                                          │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  string msgData = requestBody;                                  │        │
│  │  string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();  │        │
│  │  string checkword = "YOUR_SF_CHECKWORD";                        │        │
│  │                                                                 │        │
│  │  string toSign = msgData + timestamp + checkword;               │        │
│  │  byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(toSign));   │        │
│  │  string signature = Convert.ToBase64String(hash);               │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                                                              │
│  COMMON ERROR CODES:                                                         │
│  ────────────────────                                                       │
│  | Code | Meaning | 中文说明 | Action |                                     │
│  |------|---------|---------|--------|                                     │
│  | 4001 | Invalid sign | 签名无效 | Check checkword |                      │
│  | 4002 | Request expired | 请求过期 | Check timestamp |                   │
│  | 5001 | Address not covered | 地址不在服务范围 | Try other carrier |     │
│  | 5002 | Service unavailable | 服务暂不可用 | Trigger fallback |          │
│                                                                              │
│  WEBHOOK CALLBACKS (推送回调):                                               │
│  ─────────────────────────────                                              │
│  SF pushes status updates to your endpoint:                                 │
│  - Route updates (路由更新)                                                 │
│  - Delivery confirmation (签收确认)                                         │
│  - Exception alerts (异常通知)                                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### JD Logistics Integration Notes (京东物流对接要点)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    JD LOGISTICS INTEGRATION SPECIFICS                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  OAUTH 2.0 FLOW:                                                             │
│  ────────────────                                                           │
│  1. Register app at open.jdl.com                                            │
│  2. Get app_key and app_secret                                              │
│  3. OAuth flow to get access_token (有效期通常7天)                           │
│  4. Include token in Authorization header                                   │
│                                                                              │
│  TOKEN REFRESH STRATEGY:                                                     │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  // Recommended: Refresh token 1 hour before expiry            │        │
│  │  if (token.ExpiresAt < DateTime.UtcNow.AddHours(1))            │        │
│  │  {                                                              │        │
│  │      token = await RefreshTokenAsync(token.RefreshToken);       │        │
│  │  }                                                              │        │
│  └────────────────────────────────────────────────────────────────┘        │
│                                                                              │
│  JD-SPECIFIC FIELDS:                                                         │
│  ────────────────────                                                       │
│  - customerCode: 京东分配的客户编码                                          │
│  - orderType: 1=普通 2=到付 3=代收货款                                       │
│  - goodsType: 1=普通 2=生鲜 3=贵重品                                        │
│  - addedService: 增值服务代码数组                                            │
│                                                                              │
│  SPECIAL SERVICES:                                                           │
│  ─────────────────                                                          │
│  | serviceCode | 服务名称 | Description |                                   │
│  |-------------|---------|-------------|                                   │
│  | JD_COD | 代收货款 | Cash on Delivery |                                  │
│  | JD_SIGN | 签单返还 | Return signed receipt |                            │
│  | JD_COLD | 冷链 | Cold chain logistics |                                 │
│  | JD_INSURANCE | 保价 | Declared value insurance |                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📜 Interface Contracts

### ICarrierAdapter (Core Interface)

```
Interface: ICarrierAdapter
Namespace: DT.Express.Domain.Carriers.Adapters
Purpose: Unified contract for all carrier integrations

Properties:
┌────────────────────────────────────────────────────────────┐
│ string CarrierCode { get; }                                │
│   - Returns: Carrier identifier (e.g., "SF", "JD")         │
├────────────────────────────────────────────────────────────┤
│ string CarrierName { get; }                                │
│   - Returns: Display name (e.g., "顺丰速运")                │
├────────────────────────────────────────────────────────────┤
│ bool IsAvailable { get; }                                  │
│   - Returns: Current API availability status               │
└────────────────────────────────────────────────────────────┘

Methods:
┌────────────────────────────────────────────────────────────┐
│ Task<CarrierQuote> GetRateAsync(RateRequest request)       │
│   - Input: Shipment details for quote                      │
│   - Output: Rate quote with cost, ETA                      │
│   - Throws: CarrierApiException on failure                 │
├────────────────────────────────────────────────────────────┤
│ Task<BookingResult> BookShipmentAsync(BookingRequest req)  │
│   - Input: Booking details with shipment info              │
│   - Output: Tracking number, waybill ID                    │
│   - Throws: BookingFailedException on failure              │
├────────────────────────────────────────────────────────────┤
│ Task<CancellationResult> CancelShipmentAsync(string trackingNo) │
│   - Input: Tracking number to cancel                       │
│   - Output: Cancellation confirmation                      │
│   - Throws: CancellationFailedException on failure         │
├────────────────────────────────────────────────────────────┤
│ Task<TrackingInfo> GetTrackingStatusAsync(string trackingNo) │
│   - Input: Tracking number                                 │
│   - Output: Current status with history                    │
│   - Throws: TrackingNotFoundException if invalid           │
├────────────────────────────────────────────────────────────┤
│ Task<Label> GenerateLabelAsync(LabelRequest request)       │
│   - Input: Label generation parameters                     │
│   - Output: Label data (PDF/PNG/ZPL)                       │
│   - Throws: LabelGenerationException on failure            │
├────────────────────────────────────────────────────────────┤
│ Task<HealthStatus> CheckHealthAsync()                      │
│   - Output: API availability and response time             │
│   - Use: For monitoring and fallback decisions             │
└────────────────────────────────────────────────────────────┘
```

### ICarrierAdapterFactory (Factory Interface)

```
Interface: ICarrierAdapterFactory
Namespace: DT.Express.Domain.Carriers.Factories
Purpose: Create and manage carrier adapters

Methods:
┌────────────────────────────────────────────────────────────┐
│ ICarrierAdapter GetAdapter(string carrierCode)             │
│   - Input: Carrier code (e.g., "SF")                       │
│   - Output: Configured adapter instance                    │
│   - Throws: CarrierNotSupportedException if unknown        │
├────────────────────────────────────────────────────────────┤
│ IEnumerable<ICarrierAdapter> GetAllAdapters()              │
│   - Output: All registered adapters                        │
│   - Use: For rate comparison across all carriers           │
├────────────────────────────────────────────────────────────┤
│ IEnumerable<ICarrierAdapter> GetAvailableAdapters()        │
│   - Output: Only currently healthy adapters                │
│   - Use: For booking with fallback                         │
├────────────────────────────────────────────────────────────┤
│ bool IsCarrierSupported(string carrierCode)                │
│   - Output: Whether carrier is registered                  │
└────────────────────────────────────────────────────────────┘
```

### ICarrierSelectionService (Selection Strategy)

```
Interface: ICarrierSelectionService
Namespace: DT.Express.Application.Services
Purpose: Select optimal carrier based on criteria

Methods:
┌────────────────────────────────────────────────────────────┐
│ Task<CarrierQuote> SelectBestCarrierAsync(                 │
│     ShipmentDetails shipment,                              │
│     SelectionCriteria criteria)                            │
│   - Input: Shipment info + selection preference            │
│   - Output: Best carrier quote                             │
├────────────────────────────────────────────────────────────┤
│ Task<List<CarrierQuote>> GetAllQuotesAsync(                │
│     ShipmentDetails shipment)                              │
│   - Output: All carrier quotes ranked                      │
├────────────────────────────────────────────────────────────┤
│ Task<BookingResult> BookWithFallbackAsync(                 │
│     BookingRequest request,                                │
│     List<string> carrierPriority)                          │
│   - Input: Booking + ordered carrier preferences           │
│   - Output: Successful booking (tries next on failure)     │
└────────────────────────────────────────────────────────────┘
```

---

## 🗺️ Carrier API Mappings

### SF Express API Mapping

| Our Method | SF API Endpoint | Request Transform | Response Transform |
|------------|-----------------|-------------------|-------------------|
| GetRateAsync | /v2/price/query | ShipmentDetails → SFPriceRequest | SFPriceResponse → CarrierQuote |
| BookShipmentAsync | /v2/order/create | BookingRequest → SFOrderRequest | SFOrderResponse → BookingResult |
| CancelShipmentAsync | /v2/order/cancel | trackingNo → SFCancelRequest | SFCancelResponse → CancellationResult |
| GetTrackingStatusAsync | /v2/route/query | trackingNo → SFRouteRequest | SFRouteResponse → TrackingInfo |
| GenerateLabelAsync | /v2/waybill/print | LabelRequest → SFWaybillRequest | SFWaybillResponse → Label |

### JD Logistics API Mapping

| Our Method | JD API Endpoint | Request Transform | Response Transform |
|------------|-----------------|-------------------|-------------------|
| GetRateAsync | /api/price/calculate | ShipmentDetails → JDPriceReq | JDPriceResp → CarrierQuote |
| BookShipmentAsync | /api/order/submit | BookingRequest → JDOrderReq | JDOrderResp → BookingResult |
| CancelShipmentAsync | /api/order/cancel | trackingNo → JDCancelReq | JDCancelResp → CancellationResult |
| GetTrackingStatusAsync | /api/track/query | trackingNo → JDTrackReq | JDTrackResp → TrackingInfo |
| GenerateLabelAsync | /api/print/waybill | LabelRequest → JDPrintReq | JDPrintResp → Label |

### Status Code Mapping (Normalized)

| Our Status | SF Status | JD Status | ZTO Status | Description |
|------------|-----------|-----------|------------|-------------|
| CREATED | 10 | CREATED | 0 | Order created |
| PICKED_UP | 20 | COLLECTED | 1 | Package collected |
| IN_TRANSIT | 30 | TRANSPORTING | 2 | In transit |
| OUT_FOR_DELIVERY | 40 | DELIVERING | 3 | Last mile |
| DELIVERED | 50 | SIGNED | 4 | Delivered |
| EXCEPTION | 80 | EXCEPTION | 8 | Problem occurred |
| CANCELLED | 90 | CANCELLED | 9 | Cancelled |

---

## 📊 Data Models

### CarrierQuote (Response DTO)

| Property | Type | Description |
|----------|------|-------------|
| QuoteId | Guid | Unique quote identifier |
| CarrierCode | string | Carrier identifier |
| CarrierName | string | Display name |
| ServiceType | ServiceType | Express/Standard/Economy |
| TotalCost | Money | Total shipping cost |
| Currency | string | Cost currency (CNY) |
| EstimatedDelivery | DateTime | Expected delivery date |
| TransitDays | int | Days in transit |
| ValidUntil | DateTime | Quote expiration |
| Surcharges | List<Surcharge> | Additional fees |
| Restrictions | List<string> | Any limitations |

### BookingRequest (Input DTO)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| ShipmentId | Guid | ✅ | Internal shipment reference |
| CarrierCode | string | ✅ | Selected carrier |
| ServiceType | ServiceType | ✅ | Service level |
| Sender | ContactInfo | ✅ | Pickup contact |
| Recipient | ContactInfo | ✅ | Delivery contact |
| Packages | List<PackageInfo> | ✅ | Package details |
| PickupDate | DateTime | ⬜ | Requested pickup |
| SpecialInstructions | string | ⬜ | Handling notes |
| InsuranceValue | Money | ⬜ | Declared value |
| CODAmount | Money | ⬜ | Cash on delivery |

### BookingResult (Response DTO)

| Property | Type | Description |
|----------|------|-------------|
| Success | bool | Booking successful |
| TrackingNumber | string | Carrier tracking ID |
| WaybillNumber | string | Carrier waybill ID |
| CarrierCode | string | Booked carrier |
| BookingReference | string | Carrier booking ref |
| EstimatedPickup | DateTime | Expected pickup time |
| LabelUrl | string | Label download URL |
| ErrorMessage | string | If failed, reason |

### TrackingInfo (Response DTO)

| Property | Type | Description |
|----------|------|-------------|
| TrackingNumber | string | Carrier tracking ID |
| CarrierCode | string | Carrier identifier |
| CurrentStatus | ShipmentStatus | Normalized status |
| CurrentLocation | string | Latest location |
| EstimatedDelivery | DateTime | Updated ETA |
| DeliveredAt | DateTime? | Actual delivery time |
| SignedBy | string | Recipient name |
| Events | List<TrackingEvent> | Status history |

### TrackingEvent (Value Object)

| Property | Type | Description |
|----------|------|-------------|
| Timestamp | DateTime | When event occurred |
| Status | ShipmentStatus | Status at this point |
| Location | string | Where it happened |
| Description | string | Event description |
| RawStatus | string | Original carrier status |

---

## 🔌 Integration Points

### Upstream Dependencies (Inputs)

| System | Data Provided | Integration |
|--------|---------------|-------------|
| Dynamic Routing (01) | Route with distance/time | Route.Id for cost calculation |
| Order Processing (04) | Shipment details | BookingRequest data |

### Downstream Consumers (Outputs)

| System | Data Consumed | Integration |
|--------|---------------|-------------|
| Real-time Tracking (03) | TrackingNumber, CarrierCode | For status polling |
| Audit Tracking (05) | Booking events | Domain events |
| Order Processing (04) | BookingResult | Update order status |

### External API Dependencies

| Carrier | API Documentation | Sandbox URL |
|---------|-------------------|-------------|
| SF Express | open.sf-express.com | sandbox.sf-express.com |
| JD Logistics | open.jdl.com | sandbox.jdl.com |
| ZTO Express | open.zto.com | test.zto.com |
| YTO Express | open.yto.net.cn | test.yto.net.cn |

---

## 📚 Study Resources

### Chinese Tech Community References

| Source | Search Keywords | Focus |
|--------|-----------------|-------|
| CSDN | `多承运商 工厂模式 实战` | Factory pattern for carriers |
| CSDN | `3PL系统 接口适配器` | Adapter pattern implementation |
| Gitee | `3PL-Carrier-Adapter` | Working carrier adapter code |
| CSDN | `京东物流承运商集成` | JD integration approach |
| 掘金 | `菜鸟物流运力资源管理` | Cainiao platform design |

### Carrier API Documentation (Official)

| Carrier | Portal | Key Docs |
|---------|--------|----------|
| SF Express | open.sf-express.com | 开发指南, API文档 |
| JD Logistics | open.jdl.com | 接入文档, SDK |
| ZTO Express | open.zto.com | 对接指南 |
| Cainiao | open.taobao.com/doc | 物流API |

### Design Pattern References

| Resource | Content | Application |
|----------|---------|-------------|
| Refactoring Guru - Adapter | refactoring.guru/adapter | API unification |
| Refactoring Guru - Factory | refactoring.guru/factory | Adapter creation |
| Gitee: DesignPattern | dotnet-campus/DesignPattern | C# implementations |

---

## ✅ Acceptance Criteria

### Functional Acceptance

| ID | Criteria | Test Method |
|----|----------|-------------|
| AC-CR-001 | Can get rate from SF Express | Integration test |
| AC-CR-002 | Can get rate from JD Logistics | Integration test |
| AC-CR-003 | Can get rates from all carriers simultaneously | Parallel test |
| AC-CR-004 | Can book shipment with SF Express | Integration test |
| AC-CR-005 | Can cancel shipment | Integration test |
| AC-CR-006 | Can track shipment status | Integration test |
| AC-CR-007 | Can generate waybill label | Integration test |
| AC-CR-008 | New carrier can be added without core code change | Extension test |
| AC-CR-009 | Fallback works when primary carrier fails | Chaos test |
| AC-CR-010 | Status codes normalized correctly | Mapping test |

### Non-Functional Acceptance

| ID | Criteria | Target | Test Method |
|----|----------|--------|-------------|
| NFR-CR-001 | Rate query response time | < 3s (all carriers) | Performance |
| NFR-CR-002 | Booking response time | < 5s | Performance |
| NFR-CR-003 | API availability | > 99.5% | Monitoring |
| NFR-CR-004 | Concurrent bookings | 50/sec | Load test |
| NFR-CR-005 | Carrier onboarding time | < 1 day | Process |

---

## 🔗 Related Documents

- **Previous**: [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md) - Provides route for carrier selection
- **Next**: [03-REALTIME-TRACKING.md](03-REALTIME-TRACKING.md) - Uses tracking numbers from this domain
- **Uses patterns from**: [ADAPTER-PATTERN.md](../design-patterns/ADAPTER-PATTERN.md), [FACTORY-PATTERN.md](../design-patterns/FACTORY-PATTERN.md)
- **Index**: [00-INDEX.md](../00-INDEX.md)

---

*Document Version: 1.0*  
*Last Updated: 2026-01-31*  
*Status: ⬜ Not Started*

---

## 📈 Enterprise Implementation Guide (企业实施指南)

> **Study Focus**: How to implement carrier integration in a production environment  
> **Learning Goal**: Understand the phases and key decisions in real implementation

### Implementation Roadmap

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    CARRIER INTEGRATION IMPLEMENTATION ROADMAP                        │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  PHASE 1: FOUNDATION (Week 1-2)                                                     │
│  ═══════════════════════════════                                                    │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │ DELIVERABLES:                                                                │   │
│  │ □ ICarrierAdapter interface defined                                         │   │
│  │ □ ICarrierAdapterFactory interface defined                                  │   │
│  │ □ Domain models (BookingRequest, CarrierQuote, etc.)                        │   │
│  │ □ Status code normalization enum                                            │   │
│  │ □ Exception hierarchy (CarrierApiException, etc.)                           │   │
│  │                                                                              │   │
│  │ KEY DECISION: Unified model structure                                       │   │
│  │ ┌────────────────────────────────────────────────────────────────┐         │   │
│  │ │ • Define ContactInfo to cover ALL carrier contact formats     │         │   │
│  │ │ • Define PackageInfo with optional carrier-specific fields    │         │   │
│  │ │ • Create extensible metadata dictionary for edge cases        │         │   │
│  │ └────────────────────────────────────────────────────────────────┘         │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                    │
│  PHASE 2: FIRST CARRIER (Week 3-4) - Start with SF Express                         │
│  ═════════════════════════════════════════════════════════                         │
│                                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │ DELIVERABLES:                                                               │   │
│  │ □ SF Express sandbox account obtained                                       │   │
│  │ □ SFExpressAdapter implementing ICarrierAdapter                             │   │
│  │ □ SF API request/response models                                            │   │
│  │ □ SF signature authentication implementation                                │   │
│  │ □ Integration tests against SF sandbox                                      │   │
│  │ □ Rate mapping (SF rates → CarrierQuote)                                    │   │
│  │ □ Status mapping (SF status codes → unified status)                         │   │
│  │                                                                              │   │
│  │ WHY SF FIRST?                                                               │   │
│  │ ┌────────────────────────────────────────────────────────────────┐         │   │
│  │ │ ✓ Best documentation among Chinese carriers                    │         │   │
│  │ │ ✓ Most complex auth (HMAC sign) - if you handle SF, others easy│         │   │
│  │ │ ✓ Complete feature set to validate interface design            │         │   │
│  │ │ ✓ Reliable sandbox environment                                 │         │   │
│  │ └────────────────────────────────────────────────────────────────┘         │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  PHASE 3: SECOND CARRIER (Week 5-6) - Add JD Logistics                             │
│  ═════════════════════════════════════════════════════════                         │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │ DELIVERABLES:                                                                │   │
│  │ □ JD Open Platform account obtained                                         │   │
│  │ □ JDLogisticsAdapter implementing ICarrierAdapter                           │   │
│  │ □ OAuth 2.0 token management (refresh before expiry)                        │   │
│  │ □ JD-specific service codes mapped                                          │   │
│  │ □ Integration tests against JD sandbox                                      │   │
│  │                                                                              │   │
│  │ VALIDATE INTERFACE DESIGN:                                                  │   │
│  │ ┌────────────────────────────────────────────────────────────────┐         │   │
│  │ │ At this point, verify:                                         │         │   │
│  │ │ • Did SF adapter require interface changes for JD?            │         │   │
│  │ │   → If yes, refactor interface                                │         │   │
│  │ │   → If no, interface is robust                                │         │   │
│  │ │ • Are domain models sufficient for both?                      │         │   │
│  │ │ • Is status mapping complete?                                 │         │   │
│  │ └────────────────────────────────────────────────────────────────┘         │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  PHASE 4: FACTORY & SELECTION (Week 7-8)                                           │
│  ═══════════════════════════════════════                                           │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │ DELIVERABLES:                                                                │   │
│  │ □ Registry-based CarrierAdapterFactory                                      │   │
│  │ □ DI registration for all adapters                                          │   │
│  │ □ ICarrierSelectionService with strategies                                  │   │
│  │ □ Rate comparison logic (cheapest, fastest, best value)                     │   │
│  │ □ Carrier capability filtering                                              │   │
│  │                                                                              │   │
│  │ SELECTION STRATEGIES TO IMPLEMENT:                                          │   │
│  │ ┌────────────────────────────────────────────────────────────────┐         │   │
│  │ │ 1. CheapestRateStrategy - Sort by cost ascending              │         │   │
│  │ │ 2. FastestDeliveryStrategy - Sort by ETA ascending            │         │   │
│  │ │ 3. BestValueStrategy - Score = 0.6*cost + 0.4*time           │         │   │
│  │ │ 4. ReliabilityFirstStrategy - Filter by 99%+ SLA carriers    │         │   │
│  │ │ 5. CustomerPreferenceStrategy - Use customer's saved carrier │         │   │
│  │ └────────────────────────────────────────────────────────────────┘         │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  PHASE 5: RESILIENCE (Week 9-10)                                                   │
│  ═══════════════════════════════                                                   │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │ DELIVERABLES:                                                                │   │
│  │ □ Rate caching (Redis) with 15-minute TTL                                   │   │
│  │ □ Circuit breaker per carrier (Polly)                                       │   │
│  │ □ Fallback chain implementation                                             │   │
│  │ □ Health check endpoint per carrier                                         │   │
│  │ □ Retry policy with exponential backoff                                     │   │
│  │                                                                              │   │
│  │ POLLY CONFIGURATION EXAMPLE:                                                │   │
│  │ ┌────────────────────────────────────────────────────────────────┐         │   │
│  │ │ // Circuit breaker: Open after 5 failures, reset after 30s    │         │   │
│  │ │ var circuitBreaker = Policy                                   │         │   │
│  │ │     .Handle<CarrierApiException>()                            │         │   │
│  │ │     .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));       │         │   │
│  │ │                                                                │         │   │
│  │ │ // Retry: 3 attempts with exponential backoff                 │         │   │
│  │ │ var retry = Policy                                            │         │   │
│  │ │     .Handle<CarrierApiException>(e => e.IsTransient)         │         │   │
│  │ │     .WaitAndRetryAsync(3, attempt =>                         │         │   │
│  │ │         TimeSpan.FromSeconds(Math.Pow(2, attempt)));         │         │   │
│  │ └────────────────────────────────────────────────────────────────┘         │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  PHASE 6: SCALE (Week 11-12) - Add remaining carriers                              │
│  ════════════════════════════════════════════════════                              │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │ DELIVERABLES:                                                                │   │
│  │ □ ZTO Express adapter                                                       │   │
│  │ □ YTO Express adapter                                                       │   │
│  │ □ Yunda adapter                                                             │   │
│  │ □ STO adapter (optional)                                                    │   │
│  │ □ Load testing with all carriers                                            │   │
│  │ □ Performance benchmarks documented                                         │   │
│  │                                                                              │   │
│  │ EACH NEW ADAPTER SHOULD TAKE:                                               │   │
│  │ ┌────────────────────────────────────────────────────────────────┐         │   │
│  │ │ Day 1: Read API docs, get sandbox credentials                 │         │   │
│  │ │ Day 2: Implement request/response mapping                     │         │   │
│  │ │ Day 3: Implement authentication                               │         │   │
│  │ │ Day 4: Write integration tests                                │         │   │
│  │ │ Day 5: Code review, documentation                             │         │   │
│  │ │ ────────────────────────────────────────────────               │         │   │
│  │ │ Total: ~5 days per carrier (with good interface design)       │         │   │
│  │ └────────────────────────────────────────────────────────────────┘         │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Production Deployment Checklist

| Phase | Checkpoint | Verification |
|-------|------------|--------------|
| **Pre-Deployment** | All carrier sandboxes tested | Integration tests pass |
| | Rate caching configured | Redis connection verified |
| | Circuit breakers configured | Chaos testing passed |
| | Fallback chains defined | Manual failover tested |
| **Deployment** | Blue-green deployment | Old version ready to rollback |
| | Canary release (5% traffic) | Error rates monitored |
| | Gradual rollout | SLA metrics stable |
| **Post-Deployment** | Carrier API monitoring | Dashboards active |
| | Cost tracking enabled | Per-carrier API spend visible |
| | Alert rules configured | On-call notified on failures |

---

## 🚀 Advanced Optimization Directions (高级优化方向)

> **Study Focus**: What do enterprise systems optimize beyond basic integration?  
> **Learning Goal**: Understand advanced techniques for carrier integration at scale

### Advanced Technique 1: Predictive Carrier Selection

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    PREDICTIVE CARRIER SELECTION (预测性承运商选择)                    │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  PROBLEM: Static carrier selection doesn't account for real-time conditions         │
│  问题: 静态承运商选择不考虑实时情况                                                   │
│                                                                                      │
│  FACTORS THAT AFFECT CARRIER PERFORMANCE:                                           │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │ • 天气 (Weather): Heavy rain → ZTO delays in South China                   │   │
│  │ • 节假日 (Holidays): 双11 → All carriers congested                          │   │
│  │ • 容量 (Capacity): SF morning slots full → JD has availability            │   │
│  │ • 历史表现 (History): SF 上海→北京 on-time 99.2%                            │   │
│  │ • 实时堵塞 (Real-time): Highway accident → avoid ground carriers           │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│  SOLUTION: ML-based carrier recommendation                                          │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  Training Data:                                                             │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ Features:                          Label:                      │        │   │
│  │  │ • Route (origin, destination)      • Actual delivery time     │        │   │
│  │  │ • Day of week                      • On-time? (Y/N)           │        │   │
│  │  │ • Time of day                      • Damage rate              │        │   │
│  │  │ • Weather conditions               • Customer satisfaction    │        │   │
│  │  │ • Package characteristics                                      │        │   │
│  │  │ • Carrier used                                                 │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  │  Model Output:                                                              │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ For given shipment + current conditions:                       │        │   │
│  │  │                                                                │        │   │
│  │  │ SF Express:  P(on-time) = 0.94, Predicted ETA: 18.5 hours     │        │   │
│  │  │ JD Logistics: P(on-time) = 0.91, Predicted ETA: 20.2 hours    │        │   │
│  │  │ ZTO Express:  P(on-time) = 0.78, Predicted ETA: 28.6 hours    │        │   │
│  │  │                                                                │        │   │
│  │  │ Recommendation: SF Express (highest on-time probability)       │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Advanced Technique 2: Dynamic Rate Negotiation

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    DYNAMIC RATE NEGOTIATION (动态费率协商)                           │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  CONCEPT: Negotiate better rates based on volume commitments                        │
│  概念: 根据订单量承诺协商更优费率                                                     │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  TIERED PRICING EXAMPLE (阶梯定价示例):                                      │   │
│  │                                                                              │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ Monthly Volume    │ SF Rate/kg │ JD Rate/kg │ ZTO Rate/kg     │        │   │
│  │  ├───────────────────┼────────────┼────────────┼─────────────────┤        │   │
│  │  │ 0 - 10,000        │ ¥22        │ ¥20        │ ¥12             │        │   │
│  │  │ 10,001 - 50,000   │ ¥19 (-14%) │ ¥17 (-15%) │ ¥10 (-17%)      │        │   │
│  │  │ 50,001 - 100,000  │ ¥16 (-27%) │ ¥14 (-30%) │ ¥8 (-33%)       │        │   │
│  │  │ 100,001+          │ ¥14 (-36%) │ ¥12 (-40%) │ ¥6 (-50%)       │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  │  VOLUME COMMITMENT STRATEGY (运量承诺策略):                                  │   │
│  │                                                                              │   │
│  │  Current monthly volume: 80,000 shipments                                   │   │
│  │  Current tier: 50,001 - 100,000                                            │   │
│  │                                                                              │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ Option A: Stay at current tier                                 │        │   │
│  │  │   Cost: 80,000 × ¥16 = ¥1,280,000/month                       │        │   │
│  │  │                                                                │        │   │
│  │  │ Option B: Commit to 100,000 (SF discounts 20,000 buffer)      │        │   │
│  │  │   Cost: 80,000 × ¥14 = ¥1,120,000/month                       │        │   │
│  │  │   Savings: ¥160,000/month (12.5%)                             │        │   │
│  │  │   Risk: Must pay for 100,000 even if volume drops             │        │   │
│  │  │                                                                │        │   │
│  │  │ Decision: System analyzes volume trends and commits if        │        │   │
│  │  │          >90% confidence of hitting next tier                 │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Advanced Technique 3: Real-Time Capacity Pooling

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    REAL-TIME CAPACITY POOLING (实时运力池化)                         │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  CONCEPT: Query carrier available capacity before selection                         │
│  概念: 选择承运商前查询可用运力                                                       │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                              │   │
│  │  PROBLEM SCENARIO:                                                          │   │
│  │  ────────────────────                                                       │   │
│  │  Flash sale event: 100,000 orders in 2 hours                               │   │
│  │  SF Express daily capacity: 80,000 orders                                  │   │
│  │  Result: 20,000 orders rejected by SF → customer complaints                │   │
│  │                                                                              │   │
│  │  SOLUTION: Pre-check capacity before booking                               │   │
│  │  ────────────────────────────────────────                                  │   │
│  │                                                                              │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │  // Check capacity before selecting carrier                    │        │   │
│  │  │  var capacities = await _carriers.GetRealTimeCapacitiesAsync();│        │   │
│  │  │                                                                │        │   │
│  │  │  // Result:                                                    │        │   │
│  │  │  // SF: 15,000 remaining today (80,000 - 65,000 used)         │        │   │
│  │  │  // JD: 42,000 remaining today                                │        │   │
│  │  │  // ZTO: 120,000 remaining today                              │        │   │
│  │  │                                                                │        │   │
│  │  │  // Smart distribution:                                        │        │   │
│  │  │  // - VIP orders → SF (15,000)                                │        │   │
│  │  │  // - Express orders → JD (42,000)                            │        │   │
│  │  │  // - Standard orders → ZTO (43,000)                          │        │   │
│  │  │  // Total: 100,000 ✓                                          │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  │  API INTEGRATION (Carriers that support capacity API):                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐        │   │
│  │  │ SF Express: GET /v2/capacity/query                            │        │   │
│  │  │ JD Logistics: GET /api/capacity/available                     │        │   │
│  │  │ Cainiao Platform: GET /logistics/capacity/pool                │        │   │
│  │  │ ZTO: Not available (estimate from historical data)            │        │   │
│  │  └────────────────────────────────────────────────────────────────┘        │   │
│  │                                                                              │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## ❓ Study Questions for Self-Assessment (自我评估学习问题)

### Conceptual Understanding

| # | Question | Expected Understanding |
|---|----------|----------------------|
| 1 | Why is Adapter pattern preferred over Facade for carrier integration? | Adapter converts incompatible interfaces; Facade simplifies complex subsystems. Carriers need interface conversion. |
| 2 | What happens if we use Simple Factory instead of Registry-based Factory? | Must modify factory code when adding carriers - violates Open/Closed Principle |
| 3 | When would you use Proxy pattern instead of Adapter in carrier integration? | When adding cross-cutting concerns (caching, logging) without changing adapter logic |
| 4 | Why cache rate quotes for 15 minutes instead of 1 hour? | Balance between API cost savings and rate accuracy. Rates can change hourly. |
| 5 | What's the difference between fallback chain and circuit breaker? | Fallback = try next carrier on failure. Circuit breaker = stop trying after repeated failures. |

### Design Decisions

| # | Scenario | Question |
|---|----------|----------|
| 1 | New carrier uses SOAP instead of REST | How would you modify ICarrierAdapter to support both? |
| 2 | JD requires async callback for booking result | How to handle async booking while keeping interface synchronous? |
| 3 | SF changes their API format | What code changes? How to minimize impact? |
| 4 | Need to support international carriers (FedEx, DHL) | What interface modifications needed? |
| 5 | Some carriers have 10x higher rates | How to implement rate thresholds and alerts? |

### Implementation Challenges

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    CHALLENGE 1: Authentication Complexity                            │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  SITUATION: Each carrier uses different authentication                              │
│                                                                                      │
│  • SF Express: API Key + HMAC-MD5 signature in header                              │
│  • JD Logistics: OAuth 2.0 with refresh token                                      │
│  • ZTO: Simple API Key                                                             │
│  • Cainiao: Taobao Open Platform OAuth                                             │
│                                                                                      │
│  QUESTION: How do you design adapters to encapsulate auth complexity while         │
│            keeping the ICarrierAdapter interface clean?                            │
│                                                                                      │
│  HINT: Consider authentication as an internal concern of each adapter              │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    CHALLENGE 2: Partial Failures                                     │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  SITUATION: Querying 5 carriers for rates, 2 succeed, 2 timeout, 1 returns error   │
│                                                                                      │
│  QUESTION: What should the system return to the user?                              │
│                                                                                      │
│  Options:                                                                           │
│  A) Return only the 2 successful quotes                                            │
│  B) Return error because not all carriers responded                                │
│  C) Return 2 quotes + indicate 3 carriers unavailable                              │
│  D) Retry failed carriers with longer timeout                                      │
│                                                                                      │
│  DISCUSS: Trade-offs of each approach                                              │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    CHALLENGE 3: Rate Discrepancies                                  │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  SITUATION:                                                                         │
│  1. Customer sees rate quote: SF Express ¥22                                        │
│  2. Customer clicks "Book"                                                          │
│  3. SF API returns: "Rate increased to ¥25"                                         │
│                                                                                     │
│  QUESTION: How do you handle this gracefully?                                       │
│                                                                                     │
│  Consider:                                                                          │
│  • User experience                                                                  │
│  • Business rules (honor original quote?)                                           │
│  • Cache invalidation                                                               │
│  • Automated vs manual approval for price changes                                   │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Architecture Review Questions

| Question | Think About |
|----------|-------------|
| If SF Express API goes down for 2 hours during peak time, what's your disaster recovery plan? | Fallback carriers, capacity, SLA with customers |
| How would you design carrier integration for a company expanding from China to Southeast Asia? | International carriers, multi-currency, cross-border regulations |
| What metrics would you monitor to detect carrier performance degradation before customers notice? | Response times, error rates, on-time delivery % |
| How would you implement A/B testing to compare carrier performance? | Traffic splitting, control groups, statistical significance |

---

## 🔗 Related Documents

- **Previous**: [01-DYNAMIC-ROUTING.md](01-DYNAMIC-ROUTING.md) - Provides route for carrier selection
- **Next**: [03-REALTIME-TRACKING.md](03-REALTIME-TRACKING.md) - Uses tracking numbers from this domain
- **Uses patterns from**: [ADAPTER-PATTERN.md](../design-patterns/ADAPTER-PATTERN.md), [FACTORY-PATTERN.md](../design-patterns/FACTORY-PATTERN.md)
- **Index**: [00-INDEX.md](../00-INDEX.md)

---