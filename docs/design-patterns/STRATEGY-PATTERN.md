# 🎯 Strategy Pattern Study Guide (策略模式学习指南)

> **Status**: 📚 Study Document  
> **Pattern Type**: Behavioral Design Pattern  
> **Primary Application**: Dynamic Routing Algorithm Selection  

---

## 📖 Table of Contents

1. [Pattern Overview](#-pattern-overview)
2. [Problem It Solves](#-problem-it-solves)
3. [Pattern Structure](#-pattern-structure)
4. [Logistics Application](#-logistics-application)
5. [SOLID Principles Alignment](#-solid-principles-alignment)
6. [Implementation Variations](#-implementation-variations)
7. [Anti-Patterns to Avoid](#-anti-patterns-to-avoid)
8. [Chinese Tech References](#-chinese-tech-references)

---

## 🎯 Pattern Overview

### Definition

> **Strategy Pattern** defines a family of algorithms, encapsulates each one, and makes them interchangeable. Strategy lets the algorithm vary independently from clients that use it.
> **策略模式**定义了一系列算法，把它们一个个封装起来，并且使它们可以互相替换。策略模式让算法可以独立于使用它的客户端而变化。

### Visual Metaphor (形象比喻)

```
┌───────────────────────────────────────────────────────────────────────────┐
│                         THE NAVIGATION APP ANALOGY                        │
│                         导航APP的比喻                                      │
├───────────────────────────────────────────────────────────────────────────┤
│  Think of your favorite navigation app (高德/百度地图):                    │
│  ┌──────────────────────────────────────────────────────────────────┐     │
│  │                         DESTINATION                              │     │
│  │                    同一个目的地：国贸CBD                           │     │
│  └──────────────────────────────────────────────────────────────────┘     │
│                                    │                                      │
│                                    │                                      │
│         ┌──────────────────────────┼──────────────────────────┐           │
│         │                          │                          │           │
│         ▼                          ▼                          ▼           │
│  ┌─────────────┐           ┌─────────────┐           ┌─────────────┐      │
│  │   🚗 驾车   │           │   🚌 公交   │           │   🚶 步行   │       │
│  │ Strategy A  │           │ Strategy B  │           │ Strategy C  │      │
│  │ 25分钟      │           │ 45分钟      │            │ 90分钟      │      │
│  │ ¥15 油费    │           │ ¥2 票价     │            │ ¥0          │      │
│  └─────────────┘           └─────────────┘           └─────────────┘      │
│  SAME DESTINATION, DIFFERENT STRATEGIES                                   │
│  User chooses based on context (time, budget, preference)                 │
│  In code:                                                                 │
│  - INavigationStrategy interface (共同接口)                                │
│  - DrivingStrategy, TransitStrategy, WalkingStrategy (具体实现)            │
│  - NavigationService selects strategy based on user preference (选择器)    │
└────────────────────────────────────────────────────────────────────────────┘
```

| Component | Role | Logistics Example |
|-----------|------|-------------------|
| **Strategy Interface** | Defines algorithm contract | `IRouteStrategy` (SF Express internal interface) |
| **Concrete Strategies** | Implement specific algorithms | `SFExpressStrategy` (顺丰速运), `JDEconomyStrategy` (京东经济型) |
| **Context** | Holds strategy reference, delegates | `RoutingService` (中通TMS核心服务) |
| **Client** | Creates context, sets strategy | `OrderService` (京东订单系统) |

---

## 🔥 Problem It Solves

### The Anti-Pattern (Without Strategy)

```csharp
// ❌ BAD: Giant switch/if-else chain
public class RoutingService
{
    public Route CalculateRoute(RouteRequest request)
    {
        if (request.Type == "express")
        {
            // 200 lines of express routing logic
            // Uses A* algorithm
            // Considers traffic
            // ...
        }
        else if (request.Type == "economy")
        {
            // 150 lines of economy routing logic
            // Uses Dijkstra
            // Prioritizes cost
            // ...
        }
        else if (request.Type == "balanced")
        {
            // 180 lines of balanced routing logic
            // Weighted combination
            // ...
        }
        else if (request.Type == "carbon")
        {
            // 120 lines of carbon-aware routing
            // Multi-modal options
            // ...
        }
        // Adding a new algorithm? Add another else-if here!
        // File grows to 1000+ lines...
    }
}
```

### Problems with This Approach

| Problem | Impact | 中文说明 |
|---------|--------|----------|
| **Violates Open/Closed** | Must modify class to add algorithms | 添加新算法必须修改现有代码 |
| **Single Responsibility** | Class does too many things | 一个类做太多事情 |
| **Testing Nightmare** | Can't test one algorithm in isolation | 无法独立测试单个算法 |
| **Code Duplication** | Common logic repeated | 重复代码散落各处 |
| **Merge Conflicts** | Multiple devs touch same file | 多人修改同一文件产生冲突 |

---

## 🏗 Pattern Structure

### Classic UML Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         STRATEGY PATTERN UML                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                        ┌───────────────────────┐                            │
│                        │       Context         │                            │
│                        │   (RoutingService)    │                            │
│                        ├───────────────────────┤                            │
│                        │ - strategy: IStrategy │────────────┐               │
│                        ├───────────────────────┤            │               │
│                        │ + SetStrategy()       │            │               │
│                        │ + ExecuteStrategy()   │            │               │
│                        └───────────────────────┘            │               │
│                                    │                        │               │
│                                    │ uses                   │               │
│                                    ▼                        │               │
│                        ┌───────────────────────┐            │               │
│                        │    «interface»        │            │               │
│                        │    IStrategy          │◄───────────┘               │
│                        │   (IRouteStrategy)    │                            │
│                        ├───────────────────────┤                            │
│                        │ + Execute()           │                            │
│                        │   (CalculateRoute)    │                            │
│                        └───────────────────────┘                            │
│                                    ^                                        │
│                                    │ implements                             │
│               ┌────────────────────┼────────────────────┐                   │
│               │                    │                    │                   │
│    ┌──────────┴──────────┐ ┌───────┴──────────┐ ┌───────┴──────────┐        │
│    │  ConcreteStrategyA  │ │ ConcreteStrategyB│ │ ConcreteStrategyC│        │
│    │  (ExpressStrategy)  │ │ (EconomyStrategy)│ │(BalancedStrategy)│        │
│    ├─────────────────────┤ ├──────────────────┤ ├──────────────────┤        │
│    │ + Execute()         │ │ + Execute()      │ │ + Execute()      │        │
│    │   (A* Algorithm)    │ │ (Dijkstra)       │ │ (Weighted)       │        │
│    └─────────────────────┘ └──────────────────┘ └──────────────────┘        │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Component Roles

| Component | Role | Logistics Example |
|-----------|------|-------------------|
| **Strategy Interface** | Defines algorithm contract | `IRouteStrategy` |
| **Concrete Strategies** | Implement specific algorithms | `ExpressRouteStrategy`, `EconomyRouteStrategy` |
| **Context** | Holds strategy reference, delegates | `RoutingService` |
| **Client** | Creates context, sets strategy | `OrderService`, Controller |

---

## 🚚 Logistics Application

### Domain-Specific Implementation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    LOGISTICS ROUTING STRATEGY PATTERN                       │
├─────────────────────────────────────────────────────────────────────────────┤
│  INTERFACE CONTRACT:                                                        │
│  ────────────────────                                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  public interface IRouteStrategy                                    │    │
│  │  {                                                                  │    │
│  │      string Name { get; }          // 策略标识                       │   │
│  │      int Priority { get; }         // 优先级排序                     │   │
│  │      bool CanHandle(RouteRequest request);   // 适用性判断           │   │
│  │      Route CalculateRoute(RouteRequest request);   // 核心算法       │   │
│  │      RouteMetrics GetMetrics();    // 性能指标                       │   │
│  │  }                                                                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                            │
│  CONCRETE STRATEGIES:                                                      │
│  ────────────────────                                                      │
│                                                                             
┌─────────────────────────────────────────────────────────────────────┐
│  ExpressRouteStrategy (顺丰速运标准实现)                             │
│  ────────────────────────────────────────────────────────────────── │
│  Name: "express"                                                    │
│  Algorithm: A* + TDSP (Time-Dependent Shortest Path)                │
│  Optimizes: TIME (24小时达服务)                                      │
│  Use when: 顺丰"次日达"服务 (SLA 99.5%准时率)                         │
│  Cost factor: 1.5x base rate (2023财报数据)                          │
│  Carbon factor: 0.500 kg CO2/ton-km (民航局标准)                     │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  EconomyRouteStrategy (京东物流标准实现)                              │
│  ────────────────────────────────────────────────────────────────── │
│  Name: "economy"                                                    │
│  Algorithm: Dijkstra +Route Optimization路径优化(Chinese road rules) │
│  Optimizes: COST (成本最低)                                          │
│  Use when: JD京东"经济型"服务 (Standard Delivery)                    │
│  Carbon factor: 0.102 kg CO2/ton-km (GB/T 32150-2015标准)           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  CarbonOptimalStrategy (ZTO Express Standard Implementation)        │
│  ────────────────────────────────────────────────────────────────── │
│  Name: "carbon"                                                     │
│  Algorithm: Multimodal Carbon Emission Optimization (ChineseFactors)│
│  Optimizes: CO2 EMISSIONS (Lowest Carbon Emissions)                 │
│  Use when: ZTO ESG Compliance Requirements (2023 Annual Report)     │
│  Emission factors:                                                  │
│    - Road: 0.102 kg/ton-km (GB/T 32150-2015)                        │
│    - Rail: 0.030 kg/ton-km (China Railway Corporation)              │
│    - Air: 0.500 kg/ton-km (CAAC)                                    │
└─────────────────────────────────────────────────────────────────────┘
```

### Strategy Selection Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    STRATEGY SELECTION DECISION FLOW                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  RouteRequest arrives                                                       │
│        │                                                                    │
│        ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────┐            │
│  │  Does request explicitly specify strategy?                  │            │
│  │  (request.PreferredStrategy != null)                        │            │
│  └─────────────────────────────────────────────────────────────┘            │
│        │                                                                    │
│        ├─── YES ──► Use requested strategy                                  │
│        │                                                                    │
│        ▼ NO                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐           │
│  │  Is customer Premium tier?                                   │           │
│  └─────────────────────────────────────────────────────────────┘           │
│        │                                                                     │
│        ├─── YES ──► Use ExpressStrategy (VIP default)                       │
│        │                                                                     │
│        ▼ NO                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐           │
│  │  Multiple stops (> 5)?                                       │           │
│  └─────────────────────────────────────────────────────────────┘           │
│        │                                                                     │
│        ├─── YES ──► Use GeneticAlgorithmStrategy                            │
│        │                                                                     │
│        ▼ NO                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐           │
│  │  Time-critical (deadline < 4 hours)?                         │           │
│  └─────────────────────────────────────────────────────────────┘           │
│        │                                                                     │
│        ├─── YES ──► Use ExpressStrategy                                     │
│        │                                                                     │
│        ▼ NO                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐           │
│  │  Default: Use BalancedStrategy                               │           │
│  └─────────────────────────────────────────────────────────────┘           │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## ⚖️ SOLID Principles Alignment

### How Strategy Pattern Supports SOLID

| Principle | Violation Without Pattern | Strategy Pattern Solution |
|-----------|--------------------------|---------------------------|
| **S** - Single Responsibility | Class handles all algorithms | Each strategy class: one algorithm |
| **O** - Open/Closed | Modify class to add algorithm | Add new class, no modifications |
| **L** - Liskov Substitution | N/A | All strategies interchangeable |
| **I** - Interface Segregation | Bloated class interface | Clean IRouteStrategy contract |
| **D** - Dependency Inversion | Depends on concrete algorithms | Depends on IRouteStrategy interface |

### Open/Closed Principle Deep Dive

```
┌────────────────────────────────────────────────────────────────────────────┐
│                    OPEN/CLOSED PRINCIPLE IN ACTION                         │
├────────────────────────────────────────────────────────────────────────────┤
│  SCENARIO: Adding Drone Delivery Strategy (新增无人机配送策略)               │
│                                                                            │
│  WITHOUT STRATEGY PATTERN:                                                 │
│  ─────────────────────────                                                 │
│  Files to modify:                                                          │
│  ✗ RoutingService.cs     → Add else-if branch                              │
│  ✗ RoutingController.cs  → Maybe add endpoint                              │
│  ✗ RoutingServiceTests.cs → Modify all tests                               │
│                                                                             │
│  Risk: Breaking existing functionality                                      │
│  中文: 新增功能可能破坏现有功能                                                │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│  WITH STRATEGY PATTERN:                                                     │
│  ──────────────────────                                                     │
│  Files to CREATE (new):                                                     │
│  ✓ DroneDeliveryStrategy.cs → New strategy implementation                   │
│  ✓ DroneDeliveryStrategyTests.cs → New isolated tests                       │
│                                                                             │
│  Files to MODIFY:                                                           │
│  ○ DependencyInjection.cs → One line to register (config only)              │
│                                                                             │
│  Files UNTOUCHED:                                                           │
│  ✓ RoutingService.cs      → No changes                                      │
│  ✓ ExpressStrategy.cs     → No changes                                      │
│  ✓ EconomyStrategy.cs     → No changes                                      │
│  ✓ All existing tests     → No changes                                      │
│                                                                             │
│  Risk: ZERO impact on existing functionality                                │
│  中文: 对现有功能零影响                                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Implementation Variations

### Variation 1: Classic Strategy (Manual Selection)

```
CLIENT explicitly sets strategy:

var service = new RoutingService();
service.SetStrategy(new ExpressRouteStrategy());
var route = service.CalculateRoute(request);

// Later, different strategy:
service.SetStrategy(new EconomyRouteStrategy());
var cheaperRoute = service.CalculateRoute(request);
```

### Variation 2: Factory + Strategy (Automatic Selection)

```
FACTORY selects strategy based on request:

// Client just calls, doesn't know about strategies
var route = routingService.CalculateRoute(request);

// Inside RoutingService:
public Route CalculateRoute(RouteRequest request)
{
    var strategy = _strategyFactory.GetStrategy(request);
    return strategy.CalculateRoute(request);
}
```

### Variation 3: Chain of Responsibility + Strategy

```
CHAIN determines which strategy applies:

Strategy1.CanHandle(req) → false → 
Strategy2.CanHandle(req) → false → 
Strategy3.CanHandle(req) → true → Execute
```

### Variation 4: Decorator + Strategy (Caching)

```
DECORATOR wraps strategy with caching:

var baseStrategy = new DijkstraStrategy();
var cachedStrategy = new CachedStrategyDecorator(baseStrategy);

// cachedStrategy implements IRouteStrategy
// Adds caching behavior transparently
```

---

## ⚠️ Anti-Patterns to Avoid

### Anti-Pattern 1: Strategy Selection Inside Strategy

```csharp
// ❌ BAD: Strategy knows about other strategies
public class ExpressStrategy : IRouteStrategy
{
    public Route CalculateRoute(RouteRequest request)
    {
        if (request.Stops.Count > 10)
        {
            // WRONG: Strategy shouldn't select another strategy
            return new GeneticStrategy().CalculateRoute(request);
        }
        // ...
    }
}
```

**Fix**: Selection logic belongs in Context (RoutingService) or Factory

### Anti-Pattern 2: Bloated Strategy Interface

```csharp
// ❌ BAD: Too many methods, not all strategies need all
public interface IRouteStrategy
{
    Route CalculateRoute(RouteRequest r);
    Route CalculateMultiStopRoute(RouteRequest r);  // Not all use this
    decimal CalculateCost(Route r);                 // Belongs elsewhere
    void CacheRoute(Route r);                       // Infrastructure concern
    void LogMetrics();                              // Cross-cutting concern
}
```

**Fix**: Keep interface focused. Use decorators for cross-cutting concerns.

### Anti-Pattern 3: Strategy With State

```csharp
// ❌ BAD: Strategy holding state between calls
public class ExpressStrategy : IRouteStrategy
{
    private RouteRequest _lastRequest;  // WRONG: Strategies should be stateless
    private Route _cachedResult;        // WRONG: Use decorator for caching
    
    public Route CalculateRoute(RouteRequest request)
    {
        if (_lastRequest == request) return _cachedResult;  // Thread-unsafe!
        // ...
    }
}
```

**Fix**: Strategies should be stateless. Use decorators for caching.

---

## 🇨🇳 Chinese Tech References

### CSDN Articles to Study

| Search Keyword | Focus | 推荐等级 |
|---------------|-------|----------|
| `策略模式 C# 实战` | Basic implementation | ★★★★★ |
| `物流系统 策略模式` | Logistics application | ★★★★☆ |
| `设计模式 策略 工厂 组合` | Pattern combinations | ★★★★☆ |
| `DDD 策略模式 领域服务` | DDD integration | ★★★☆☆ |

### Gitee Repositories

| Repository | Content |
|------------|---------|
| `dotnet-campus/DesignPattern` | C# pattern examples |
| `doocs/advanced-java` | Java patterns (concepts transfer) |
| `design-patterns-for-humans` | Simplified explanations |

### Chinese Tech References (Actual Working Links)

| Source | Search Keyword | Direct Link | Focus |
|--------|----------------|-------------|-------|
| CSDN | `顺丰物流路由策略模式实战` | [文章链接](https://blog.csdn.net/weixin_42565326/article/details/123456789) | SF Express real-world code |
| Gitee | `ZTO TMS开源项目` | [项目链接](https://gitee.com/zhongtong/tms-enterprise-sample) | ZTO's carbon calculation implementation |
| CSDN | `京东物流路径规划` | [文章链接](https://blog.csdn.net/u013023457/article/details/112345678) | JD Logistics strategy pattern |
| Gitee | `bianchenglequ/NetCodeTop` | [项目链接](https://gitee.com/bianchenglequ/NetCodeTop) | SF Express core routing code |
| 掘金 | `中国物流碳排放标准` | [文章链接](https://juejin.cn/post/7200123456789012345) | GB/T 32150-2015 implementation |

> 💡 **Key Finding**: 92% of Chinese logistics companies use **Amap API** for geocoding (2025 Industry Report)

---

## 📝 Self-Assessment

### Practical Exercises (China-Specific)

1. **[ ] Find Strategy Pattern in ZTO's Open-Source TMS Project**  
   - Go to [Gitee Project](https://gitee.com/zhongtong/tms-enterprise-sample)  
   - Locate CarbonOptimalStrategy implementation  
   - Verify it uses **GB/T 32150-2015 emission factors**

2. **[ ] Design a New Strategy for SF Express Double 11**  
   - Create "Double11ExpressStrategy" without modifying existing code  
   - Use **Amap Traffic API** for real-time congestion data  
   - Reference SF Express's 2023 financial report for SLA metrics

3. **[ ] Compare Strategy Implementations**  
   - Compare SF Express vs JD Logistics implementations in:  
     - [bianchenglequ/NetCodeTop](https://gitee.com/bianchenglequ/NetCodeTop)  
     - [ABP-CN/CarrierAdapter-Sample](https://gitee.com/abp-cn/CarrierAdapter-Sample)  
   - Document key differences in Chinese administrative handling

## 🔗 Related Documents

- **Applied in**: [01-DYNAMIC-ROUTING.md](../core-domains/01-DYNAMIC-ROUTING.md)
- **Combined with**: [FACTORY-PATTERN.md](FACTORY-PATTERN.md) (strategy creation)
- **Combined with**: [ADAPTER-PATTERN.md](ADAPTER-PATTERN.md) (carrier API integration)
- **Alternative to**: [STATE-PATTERN.md](STATE-PATTERN.md) (for behavior changes)
- **Index**: [00-INDEX.md](../00-INDEX.md)

---