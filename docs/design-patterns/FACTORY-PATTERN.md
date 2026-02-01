# 🏭 Factory Pattern Study Guide (工厂模式学习指南)

> **Status**: 📚 Study Document  
> **Pattern Type**: Creational Design Pattern (创建型设计模式)  
> **Primary Application**: Carrier Adapter Creation (承运商适配器创建)  

---

## 📖 Table of Contents

1. [Pattern Overview](#-pattern-overview)
2. [Problem It Solves](#-problem-it-solves)
3. [Pattern Variations](#-pattern-variations)
4. [Logistics Application](#-logistics-application)
5. [Factory + Dependency Injection](#-factory--dependency-injection)
6. [SOLID Principles Alignment](#-solid-principles-alignment)
7. [Implementation Variations](#-implementation-variations)
8. [Anti-Patterns to Avoid](#-anti-patterns-to-avoid)
9. [Chinese Tech References](#-chinese-tech-references)
10. [Self-Assessment](#-self-assessment)

---

## 🎯 Pattern Overview

### Definition (定义)

> **Factory Pattern** defines an interface for creating objects, but lets subclasses or implementing classes decide which classes to instantiate. Factory lets a class defer instantiation to subclasses or specialized factory methods.
>
> **工厂模式**定义了一个创建对象的接口，但由子类或实现类决定要实例化哪个类。工厂模式让类将实例化延迟到子类或专门的工厂方法。

### Visual Metaphor (形象比喻)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    THE RESTAURANT KITCHEN ANALOGY                           │
│                    餐厅厨房的比喻                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Think of ordering at a restaurant:                                         │
│  想象在餐厅点餐:                                                             │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         CUSTOMER (Client)                            │   │
│  │                           顾客 (客户端)                               │   │
│  │                                                                      │   │
│  │  "I'll have the 宫保鸡丁, please"                                    │   │
│  │  "I'll have the 麻婆豆腐, please"                                    │   │
│  │                                                                      │   │
│  │  Customer doesn't know HOW dishes are made                          │   │
│  │  顾客不知道菜是怎么做的                                               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                                    │ Order                                  │
│                                    ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         KITCHEN (Factory)                            │   │
│  │                           厨房 (工厂)                                 │   │
│  │                                                                      │   │
│  │  switch (orderName)                                                 │   │
│  │  {                                                                  │   │
│  │      "宫保鸡丁" → Chef A makes Kung Pao Chicken                     │   │
│  │      "麻婆豆腐" → Chef B makes Mapo Tofu                            │   │
│  │      "红烧肉"   → Chef C makes Braised Pork                         │   │
│  │  }                                                                  │   │
│  │                                                                      │   │
│  │  Kitchen ENCAPSULATES dish creation                                 │   │
│  │  厨房封装了菜品的创建过程                                             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                                    │ Returns dish                           │
│                                    ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         DISH (Product)                               │   │
│  │                          菜品 (产品)                                  │   │
│  │                                                                      │   │
│  │  Customer receives dish, doesn't care which chef made it            │   │
│  │  顾客收到菜，不关心哪个厨师做的                                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  In Code (Logistics):                                                        │
│  ────────────────────                                                       │
│  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐       │
│  │ CarrierService  │────▶│ AdapterFactory  │────▶│ ICarrierAdapter │       │
│  │ (Customer)      │     │ (Kitchen)       │     │ (Dish)          │       │
│  │                 │     │                 │     │                 │       │
│  │ "Give me SF"    │     │ Creates adapter │     │ SFExpressAdapter│       │
│  └─────────────────┘     └─────────────────┘     └─────────────────┘       │
│                                                                              │
│  BENEFIT: Client doesn't need to know how adapters are constructed         │
│  好处: 客户端不需要知道适配器是如何构造的                                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### GoF Classification

| Aspect | Classification |
|--------|----------------|
| **Type** | Creational Pattern (创建型模式) |
| **Scope** | Class or Object |
| **Intent** | Encapsulate object creation (封装对象创建) |
| **Related Patterns** | Abstract Factory, Builder, Singleton, Prototype |

---

## 🔥 Problem It Solves

### The Object Creation Problem (Without Factory)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    WITHOUT FACTORY: SCATTERED CREATION                       │
│                    没有工厂: 分散的对象创建                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  PROBLEM: Object creation logic scattered everywhere                        │
│  问题: 对象创建逻辑分散在各处                                                  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  // ❌ BAD: Client creates adapters directly                       │    │
│  │                                                                    │    │
│  │  public class CarrierService                                       │    │
│  │  {                                                                 │    │
│  │      public async Task<CarrierQuote> GetRateAsync(                │    │
│  │          string carrierCode, RateRequest request)                 │    │
│  │      {                                                             │    │
│  │          ICarrierAdapter adapter;                                  │    │
│  │                                                                    │    │
│  │          // ❌ Client must know about ALL adapter types            │    │
│  │          // ❌ Client must know constructor dependencies           │    │
│  │          if (carrierCode == "SF")                                  │    │
│  │          {                                                         │    │
│  │              // ❌ Complex construction with many dependencies     │    │
│  │              var httpClient = new HttpClient();                    │    │
│  │              var logger = LoggerFactory.Create(...);              │    │
│  │              var sfConfig = new SFConfiguration(                  │    │
│  │                  partnerId: "xxx",                                │    │
│  │                  checkword: "yyy",                                │    │
│  │                  baseUrl: "https://api.sf-express.com");          │    │
│  │              adapter = new SFExpressAdapter(                       │    │
│  │                  httpClient, logger, sfConfig);                   │    │
│  │          }                                                         │    │
│  │          else if (carrierCode == "JD")                            │    │
│  │          {                                                         │    │
│  │              // ❌ Different dependencies for JD                   │    │
│  │              var httpClient = new HttpClient();                    │    │
│  │              var logger = LoggerFactory.Create(...);              │    │
│  │              var tokenManager = new JDTokenManager(...);          │    │
│  │              var jdConfig = new JDConfiguration(...);             │    │
│  │              adapter = new JDLogisticsAdapter(                     │    │
│  │                  httpClient, logger, tokenManager, jdConfig);     │    │
│  │          }                                                         │    │
│  │          else if (carrierCode == "ZTO")                           │    │
│  │          {                                                         │    │
│  │              // ❌ Yet another set of dependencies                 │    │
│  │          }                                                         │    │
│  │          // ... more carriers                                      │    │
│  │                                                                    │    │
│  │          return await adapter.GetRateAsync(request);              │    │
│  │      }                                                             │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  PROBLEMS:                                                                   │
│  ─────────                                                                  │
│  • Client tightly coupled to ALL adapter implementations                   │
│  • Client knows about adapter construction details                         │
│  • Adding new carrier = modifying CarrierService                           │
│  • Testing requires creating all dependencies                              │
│  • Configuration spread across multiple places                             │
│  • Violates Single Responsibility (service creates AND uses adapters)     │
│                                                                              │
│  中文问题:                                                                   │
│  • 客户端与所有适配器实现紧耦合                                               │
│  • 客户端需要知道适配器的构造细节                                             │
│  • 新增承运商需要修改CarrierService                                          │
│  • 测试需要创建所有依赖项                                                     │
│  • 配置分散在多个地方                                                        │
│  • 违反单一职责（服务既创建又使用适配器）                                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### The Solution: Centralize Creation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    WITH FACTORY: CENTRALIZED CREATION                        │
│                    有工厂: 集中的对象创建                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  // ✅ GOOD: Client only uses factory interface                    │    │
│  │                                                                    │    │
│  │  public class CarrierService                                       │    │
│  │  {                                                                 │    │
│  │      private readonly ICarrierAdapterFactory _factory;            │    │
│  │                                                                    │    │
│  │      public CarrierService(ICarrierAdapterFactory factory)        │    │
│  │      {                                                             │    │
│  │          _factory = factory;  // Factory injected                 │    │
│  │      }                                                             │    │
│  │                                                                    │    │
│  │      public async Task<CarrierQuote> GetRateAsync(                │    │
│  │          string carrierCode, RateRequest request)                 │    │
│  │      {                                                             │    │
│  │          // ✅ Client doesn't know about adapter construction      │    │
│  │          // ✅ Client doesn't know about configurations            │    │
│  │          // ✅ Client doesn't know about dependencies              │    │
│  │          var adapter = _factory.GetAdapter(carrierCode);          │    │
│  │          return await adapter.GetRateAsync(request);              │    │
│  │      }                                                             │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  BENEFITS:                                                                   │
│  ─────────                                                                  │
│  ✅ Client decoupled from adapter implementations                          │
│  ✅ Construction details hidden in factory                                  │
│  ✅ Adding new carrier = update factory (or DI), not client                │
│  ✅ Testing: mock ICarrierAdapterFactory easily                            │
│  ✅ Configuration centralized in factory                                    │
│  ✅ Single Responsibility: Factory creates, Service uses                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🏗 Pattern Variations

### Factory Pattern Family

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FACTORY PATTERN VARIATIONS                                │
│                    工厂模式变体                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1. SIMPLE FACTORY (简单工厂)                                                │
│  ─────────────────────────────                                              │
│  • Single factory class with conditional logic                              │
│  • Uses switch/if-else to decide which product to create                   │
│  • NOT a GoF pattern, but commonly used                                    │
│                                                                              │
│  2. FACTORY METHOD (工厂方法)                                                │
│  ─────────────────────────────                                              │
│  • Defines interface for creating objects                                   │
│  • Subclasses decide which class to instantiate                            │
│  • GoF pattern, uses inheritance                                           │
│                                                                              │
│  3. ABSTRACT FACTORY (抽象工厂)                                              │
│  ────────────────────────────                                               │
│  • Creates families of related objects                                      │
│  • E.g., UI factory creates Button + TextField + Checkbox for same theme   │
│  • GoF pattern, most complex                                               │
│                                                                              │
│  4. REGISTRY-BASED FACTORY (注册表工厂)                                      │
│  ─────────────────────────────────────                                      │
│  • Products register themselves with factory                                │
│  • Factory looks up products in registry                                    │
│  • Most flexible, works great with DI                                      │
│  • ⭐ RECOMMENDED for carrier adapters                                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Variation Comparison

| Variation | Complexity | Flexibility | When to Use |
|-----------|------------|-------------|-------------|
| **Simple Factory** | Low | Low | Small fixed set of products |
| **Factory Method** | Medium | Medium | When subclasses should decide |
| **Abstract Factory** | High | High | Families of related products |
| **Registry Factory** | Medium | High | Dynamic product registration (DI) |

---

## 🚚 Logistics Application

### Simple Factory Implementation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SIMPLE FACTORY (简单工厂)                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  public class CarrierAdapterFactory                                │    │
│  │  {                                                                 │    │
│  │      private readonly IConfiguration _config;                      │    │
│  │      private readonly ILoggerFactory _loggerFactory;              │    │
│  │      private readonly IHttpClientFactory _httpClientFactory;       │    │
│  │                                                                    │    │
│  │      public ICarrierAdapter GetAdapter(string carrierCode)         │    │
│  │      {                                                             │    │
│  │          return carrierCode switch                                │    │
│  │          {                                                         │    │
│  │              "SF" => CreateSFAdapter(),                           │    │
│  │              "JD" => CreateJDAdapter(),                           │    │
│  │              "ZTO" => CreateZTOAdapter(),                         │    │
│  │              "YTO" => CreateYTOAdapter(),                         │    │
│  │              _ => throw new CarrierNotSupportedException(carrierCode) │    │
│  │          };                                                        │    │
│  │      }                                                             │    │
│  │                                                                    │    │
│  │      private ICarrierAdapter CreateSFAdapter()                    │    │
│  │      {                                                             │    │
│  │          var sfConfig = _config.GetSection("Carriers:SF")         │    │
│  │              .Get<SFConfiguration>();                             │    │
│  │          var httpClient = _httpClientFactory.CreateClient("SF");  │    │
│  │          var logger = _loggerFactory.CreateLogger<SFExpressAdapter>();│    │
│  │          return new SFExpressAdapter(httpClient, sfConfig, logger);│    │
│  │      }                                                             │    │
│  │                                                                    │    │
│  │      private ICarrierAdapter CreateJDAdapter()                    │    │
│  │      {                                                             │    │
│  │          // Similar but with OAuth token manager                  │    │
│  │          var jdConfig = _config.GetSection("Carriers:JD")         │    │
│  │              .Get<JDConfiguration>();                             │    │
│  │          var tokenManager = new JDTokenManager(jdConfig);         │    │
│  │          return new JDLogisticsAdapter(..., tokenManager);        │    │
│  │      }                                                             │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ❌ PROBLEM: Adding new carrier requires modifying this class              │
│  ❌ 问题: 新增承运商需要修改这个类                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Registry-Based Factory (Recommended)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    REGISTRY-BASED FACTORY (注册表工厂) ⭐                    │
│                    RECOMMENDED FOR DI ENVIRONMENTS                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  STEP 1: Define Factory Interface                                           │
│  ─────────────────────────────────                                          │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  public interface ICarrierAdapterFactory                           │    │
│  │  {                                                                 │    │
│  │      /// <summary>Get adapter by carrier code</summary>            │    │
│  │      ICarrierAdapter GetAdapter(string carrierCode);               │    │
│  │                                                                    │    │
│  │      /// <summary>Get all registered adapters</summary>            │    │
│  │      IEnumerable<ICarrierAdapter> GetAllAdapters();               │    │
│  │                                                                    │    │
│  │      /// <summary>Get only healthy adapters</summary>              │    │
│  │      IEnumerable<ICarrierAdapter> GetAvailableAdapters();         │    │
│  │                                                                    │    │
│  │      /// <summary>Check if carrier is supported</summary>          │    │
│  │      bool IsCarrierSupported(string carrierCode);                 │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  STEP 2: Implement Registry-Based Factory                                   │
│  ────────────────────────────────────────                                   │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  public class CarrierAdapterFactory : ICarrierAdapterFactory       │    │
│  │  {                                                                 │    │
│  │      private readonly Dictionary<string, ICarrierAdapter> _adapters;│    │
│  │      private readonly ILogger<CarrierAdapterFactory> _logger;      │    │
│  │                                                                    │    │
│  │      // DI injects ALL registered adapters!                        │    │
│  │      public CarrierAdapterFactory(                                 │    │
│  │          IEnumerable<ICarrierAdapter> adapters,                   │    │
│  │          ILogger<CarrierAdapterFactory> logger)                   │    │
│  │      {                                                             │    │
│  │          _logger = logger;                                        │    │
│  │                                                                    │    │
│  │          // Build dictionary for O(1) lookup                      │    │
│  │          _adapters = adapters.ToDictionary(                       │    │
│  │              a => a.CarrierCode,                                  │    │
│  │              a => a,                                              │    │
│  │              StringComparer.OrdinalIgnoreCase);                   │    │
│  │                                                                    │    │
│  │          _logger.LogInformation(                                  │    │
│  │              "Registered {Count} carriers: {Carriers}",           │    │
│  │              _adapters.Count,                                     │    │
│  │              string.Join(", ", _adapters.Keys));                  │    │
│  │      }                                                             │    │
│  │                                                                    │    │
│  │      public ICarrierAdapter GetAdapter(string carrierCode)         │    │
│  │      {                                                             │    │
│  │          if (_adapters.TryGetValue(carrierCode, out var adapter)) │    │
│  │          {                                                         │    │
│  │              return adapter;                                      │    │
│  │          }                                                         │    │
│  │                                                                    │    │
│  │          _logger.LogWarning(                                      │    │
│  │              "Carrier {Code} not found. Available: {Available}",  │    │
│  │              carrierCode, string.Join(", ", _adapters.Keys));     │    │
│  │                                                                    │    │
│  │          throw new CarrierNotSupportedException(carrierCode);     │    │
│  │      }                                                             │    │
│  │                                                                    │    │
│  │      public IEnumerable<ICarrierAdapter> GetAllAdapters()          │    │
│  │          => _adapters.Values;                                     │    │
│  │                                                                    │    │
│  │      public IEnumerable<ICarrierAdapter> GetAvailableAdapters()    │    │
│  │          => _adapters.Values.Where(a => a.IsAvailable);           │    │
│  │                                                                    │    │
│  │      public bool IsCarrierSupported(string carrierCode)           │    │
│  │          => _adapters.ContainsKey(carrierCode);                   │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  STEP 3: Register in DI                                                     │
│  ──────────────────────                                                     │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  // In Program.cs or ServiceCollectionExtensions.cs               │    │
│  │                                                                    │    │
│  │  public static IServiceCollection AddCarrierIntegration(           │    │
│  │      this IServiceCollection services,                            │    │
│  │      IConfiguration configuration)                                │    │
│  │  {                                                                 │    │
│  │      // Register each adapter - DI handles construction!          │    │
│  │      services.AddTransient<ICarrierAdapter, SFExpressAdapter>();  │    │
│  │      services.AddTransient<ICarrierAdapter, JDLogisticsAdapter>(); │    │
│  │      services.AddTransient<ICarrierAdapter, ZTOExpressAdapter>(); │    │
│  │      services.AddTransient<ICarrierAdapter, YTOExpressAdapter>(); │    │
│  │      services.AddTransient<ICarrierAdapter, YundaAdapter>();      │    │
│  │                                                                    │    │
│  │      // Register factory as singleton (holds adapter references)  │    │
│  │      services.AddSingleton<ICarrierAdapterFactory,                │    │
│  │          CarrierAdapterFactory>();                                │    │
│  │                                                                    │    │
│  │      return services;                                             │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ✅ BENEFIT: Adding new carrier = Just add ONE line in DI!                 │
│  ✅ 好处: 新增承运商只需要在DI配置里加一行!                                    │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  // Adding Best Express (百世快递)                                 │    │
│  │                                                                    │    │
│  │  // Step 1: Create new adapter class (new file)                   │    │
│  │  public class BestExpressAdapter : ICarrierAdapter { ... }        │    │
│  │                                                                    │    │
│  │  // Step 2: Register in DI (ONE line!)                            │    │
│  │  services.AddTransient<ICarrierAdapter, BestExpressAdapter>();    │    │
│  │                                                                    │    │
│  │  // Step 3: DONE! Factory automatically discovers it              │    │
│  │  // CarrierService, Factory - NO CHANGES NEEDED                   │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Factory Pattern Sequence Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FACTORY PATTERN SEQUENCE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  CarrierService          Factory                Adapters                    │
│       │                    │                       │                        │
│       │  GetAdapter("SF")  │                       │                        │
│       │──────────────────▶│                       │                        │
│       │                    │                       │                        │
│       │                    │ Look up in registry  │                        │
│       │                    │─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ▶│                        │
│       │                    │                       │                        │
│       │                    │◀─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │                        │
│       │                    │   SFExpressAdapter   │                        │
│       │                    │                       │                        │
│       │◀──────────────────│                       │                        │
│       │  ICarrierAdapter   │                       │                        │
│       │                    │                       │                        │
│       │  GetRateAsync(request)                    │                        │
│       │─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─▶│                        │
│       │                    │                       │                        │
│       │                    │                       │ Call SF API            │
│       │                    │                       │──────────────▶         │
│       │                    │                       │                        │
│       │◀─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─│                        │
│       │    CarrierQuote    │                       │                        │
│       │                    │                       │                        │
│                                                                              │
│  KEY INSIGHT: CarrierService never knows about SFExpressAdapter directly!  │
│  关键洞察: CarrierService 从不直接知道 SFExpressAdapter!                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 💉 Factory + Dependency Injection

### Why They Work Together

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FACTORY + DI SYNERGY                                      │
│                    工厂 + 依赖注入 协同                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  TRADITIONAL FACTORY (without DI):                                          │
│  ─────────────────────────────────                                          │
│  Factory must create objects manually                                       │
│  Factory must manage all dependencies                                       │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  // Factory must know ALL dependencies                             │    │
│  │  private ICarrierAdapter CreateSFAdapter()                        │    │
│  │  {                                                                 │    │
│  │      // Factory creates everything - tedious!                     │    │
│  │      var httpClient = new HttpClient();                           │    │
│  │      var logger = new Logger<SFExpressAdapter>();                 │    │
│  │      var config = new SFConfiguration(...);                       │    │
│  │      return new SFExpressAdapter(httpClient, logger, config);     │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ──────────────────────────────────────────────────────────────────────────│
│                                                                              │
│  FACTORY + DI (modern approach):                                            │
│  ───────────────────────────────                                            │
│  DI container creates objects with all dependencies                         │
│  Factory just LOCATES the right pre-built object                           │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  // DI container handles ALL construction                          │    │
│  │  // Factory just receives pre-built adapters                       │    │
│  │                                                                    │    │
│  │  public CarrierAdapterFactory(                                     │    │
│  │      IEnumerable<ICarrierAdapter> adapters)  // DI provides these │    │
│  │  {                                                                 │    │
│  │      _adapters = adapters.ToDictionary(a => a.CarrierCode);       │    │
│  │      // Factory doesn't create anything - just organizes!         │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  RESPONSIBILITY SPLIT:                                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  DI Container:                      Factory:                        │   │
│  │  • Creates adapter instances        • Provides lookup by code       │   │
│  │  • Injects dependencies            • Returns IEnumerable<all>       │   │
│  │  • Manages lifetime (transient/    • Filters by availability       │   │
│  │    singleton/scoped)               • Validates carrier exists       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Keyed Services (.NET 8+)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    .NET 8 KEYED SERVICES                                     │
│                    Alternative to custom factory                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  .NET 8 introduced Keyed Services - can replace simple factory!            │
│  .NET 8 引入了键控服务 - 可以替代简单工厂!                                     │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  // Registration with keys                                         │    │
│  │  services.AddKeyedTransient<ICarrierAdapter, SFExpressAdapter>("SF");  │    │
│  │  services.AddKeyedTransient<ICarrierAdapter, JDLogisticsAdapter>("JD");│    │
│  │  services.AddKeyedTransient<ICarrierAdapter, ZTOExpressAdapter>("ZTO");│    │
│  │                                                                    │    │
│  │  // Usage in service                                               │    │
│  │  public class CarrierService                                       │    │
│  │  {                                                                 │    │
│  │      public CarrierService(                                        │    │
│  │          [FromKeyedServices("SF")] ICarrierAdapter sfAdapter,     │    │
│  │          [FromKeyedServices("JD")] ICarrierAdapter jdAdapter)     │    │
│  │      {                                                             │    │
│  │          // Direct injection by key                               │    │
│  │      }                                                             │    │
│  │  }                                                                 │    │
│  │                                                                    │    │
│  │  // OR dynamic resolution                                          │    │
│  │  public class CarrierService                                       │    │
│  │  {                                                                 │    │
│  │      private readonly IServiceProvider _provider;                  │    │
│  │                                                                    │    │
│  │      public ICarrierAdapter GetAdapter(string code)                │    │
│  │      {                                                             │    │
│  │          return _provider.GetRequiredKeyedService<ICarrierAdapter>(code); │    │
│  │      }                                                             │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  COMPARISON:                                                                 │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  Custom Factory            vs       Keyed Services                   │  │
│  ├──────────────────────────────────────────────────────────────────────┤  │
│  │  ✓ Full control                    ✓ Less code                      │  │
│  │  ✓ GetAllAdapters()               ✗ No GetAll out-of-box            │  │
│  │  ✓ GetAvailableAdapters()         ✗ No filtering built-in          │  │
│  │  ✓ Custom validation              ✗ Limited customization           │  │
│  │  ✗ More boilerplate               ✓ Framework feature               │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  RECOMMENDATION: Use custom factory when you need:                          │
│  • GetAllAdapters() for rate comparison                                    │
│  • GetAvailableAdapters() for health-filtered selection                    │
│  • Custom logging/metrics on adapter creation                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## ⚖️ SOLID Principles Alignment

### How Factory Pattern Supports SOLID

| Principle | Without Factory | With Factory |
|-----------|----------------|--------------|
| **S** - Single Responsibility | Service both creates AND uses adapters | Factory creates, Service uses |
| **O** - Open/Closed | Add carrier = modify service | Add carrier = new class + DI registration |
| **L** - Liskov Substitution | N/A directly | Factory returns interchangeable ICarrierAdapter |
| **I** - Interface Segregation | Client depends on all adapter types | Client depends only on ICarrierAdapterFactory |
| **D** - Dependency Inversion | Service depends on concrete adapters | Service depends on factory interface |

### Dependency Inversion Deep Dive

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    DEPENDENCY INVERSION IN ACTION                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  WITHOUT FACTORY (Violates DIP):                                            │
│  ───────────────────────────────                                            │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │       HIGH-LEVEL MODULE            LOW-LEVEL MODULES                │   │
│  │       (CarrierService)             (Concrete Adapters)              │   │
│  │                                                                      │   │
│  │  ┌─────────────────┐          ┌─────────────────────────────────┐  │   │
│  │  │ CarrierService  │─────────▶│ SFExpressAdapter                │  │   │
│  │  │                 │          │ JDLogisticsAdapter               │  │   │
│  │  │                 │─────────▶│ ZTOExpressAdapter                │  │   │
│  │  │                 │          │ ...                              │  │   │
│  │  └─────────────────┘          └─────────────────────────────────┘  │   │
│  │                                                                      │   │
│  │  ❌ High-level depends on low-level                                 │   │
│  │  ❌ 高级模块依赖低级模块                                              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  WITH FACTORY (Follows DIP):                                                │
│  ────────────────────────────                                               │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │       HIGH-LEVEL              ABSTRACTION              LOW-LEVEL    │   │
│  │                                                                      │   │
│  │  ┌─────────────────┐    ┌──────────────────────┐                   │   │
│  │  │ CarrierService  │───▶│ICarrierAdapterFactory│                   │   │
│  │  └─────────────────┘    └──────────┬───────────┘                   │   │
│  │                                    │                                │   │
│  │                                    │ implements                     │   │
│  │                                    │                                │   │
│  │                         ┌──────────▼───────────┐                   │   │
│  │                         │CarrierAdapterFactory │                   │   │
│  │                         └──────────┬───────────┘                   │   │
│  │                                    │                                │   │
│  │                                    │ uses                           │   │
│  │                                    ▼                                │   │
│  │                         ┌───────────────────────────────────┐      │   │
│  │                         │ ICarrierAdapter interface         │      │   │
│  │                         └───────────────────────────────────┘      │   │
│  │                                    △                                │   │
│  │              ┌─────────────────────┼─────────────────────┐         │   │
│  │              │                     │                     │         │   │
│  │         SFAdapter             JDAdapter             ZTOAdapter     │   │
│  │                                                                      │   │
│  │  ✅ Both high and low-level depend on abstractions                  │   │
│  │  ✅ 高级和低级模块都依赖于抽象                                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Implementation Variations

### Variation 1: Factory with Caching

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FACTORY WITH CACHED ADAPTERS                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  USE CASE: Adapters are expensive to create (hold connections, etc.)        │
│  场景: 适配器创建成本高（持有连接等）                                          │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  public class CachingCarrierAdapterFactory : ICarrierAdapterFactory│    │
│  │  {                                                                 │    │
│  │      private readonly ConcurrentDictionary<string, Lazy<ICarrierAdapter>> │    │
│  │          _cache = new();                                           │    │
│  │      private readonly IServiceProvider _serviceProvider;           │    │
│  │      private readonly Dictionary<string, Type> _adapterTypes;     │    │
│  │                                                                    │    │
│  │      public CachingCarrierAdapterFactory(IServiceProvider sp)     │    │
│  │      {                                                             │    │
│  │          _serviceProvider = sp;                                   │    │
│  │          _adapterTypes = new Dictionary<string, Type>             │    │
│  │          {                                                         │    │
│  │              ["SF"] = typeof(SFExpressAdapter),                   │    │
│  │              ["JD"] = typeof(JDLogisticsAdapter),                 │    │
│  │              ["ZTO"] = typeof(ZTOExpressAdapter)                  │    │
│  │          };                                                        │    │
│  │      }                                                             │    │
│  │                                                                    │    │
│  │      public ICarrierAdapter GetAdapter(string carrierCode)         │    │
│  │      {                                                             │    │
│  │          return _cache.GetOrAdd(carrierCode, code =>              │    │
│  │              new Lazy<ICarrierAdapter>(() =>                      │    │
│  │                  CreateAdapter(code))).Value;                     │    │
│  │      }                                                             │    │
│  │                                                                    │    │
│  │      private ICarrierAdapter CreateAdapter(string code)           │    │
│  │      {                                                             │    │
│  │          if (!_adapterTypes.TryGetValue(code, out var type))      │    │
│  │              throw new CarrierNotSupportedException(code);        │    │
│  │                                                                    │    │
│  │          return (ICarrierAdapter)_serviceProvider                 │    │
│  │              .GetRequiredService(type);                           │    │
│  │      }                                                             │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ⚠️ NOTE: With DI, usually register adapters as Singleton if              │
│     they're stateless. Caching factory is for special cases.               │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Variation 2: Factory with Fallback Chain

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FACTORY WITH FALLBACK SUPPORT                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  USE CASE: Get adapters in priority order for fallback booking             │
│  场景: 按优先级顺序获取适配器用于故障转移                                      │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  public interface ICarrierAdapterFactory                           │    │
│  │  {                                                                 │    │
│  │      ICarrierAdapter GetAdapter(string carrierCode);               │    │
│  │                                                                    │    │
│  │      // New: Get adapters in fallback order                        │    │
│  │      IEnumerable<ICarrierAdapter> GetFallbackChain(               │    │
│  │          IEnumerable<string> priorityOrder);                      │    │
│  │  }                                                                 │    │
│  │                                                                    │    │
│  │  // Implementation                                                 │    │
│  │  public IEnumerable<ICarrierAdapter> GetFallbackChain(             │    │
│  │      IEnumerable<string> priorityOrder)                           │    │
│  │  {                                                                 │    │
│  │      foreach (var code in priorityOrder)                          │    │
│  │      {                                                             │    │
│  │          if (_adapters.TryGetValue(code, out var adapter)         │    │
│  │              && adapter.IsAvailable)                              │    │
│  │          {                                                         │    │
│  │              yield return adapter;                                │    │
│  │          }                                                         │    │
│  │      }                                                             │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  USAGE:                                                                      │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  // In BookingService                                              │    │
│  │  public async Task<BookingResult> BookWithFallbackAsync(           │    │
│  │      BookingRequest request,                                      │    │
│  │      IEnumerable<string> carrierPriority)                         │    │
│  │  {                                                                 │    │
│  │      var adapters = _factory.GetFallbackChain(carrierPriority);   │    │
│  │                                                                    │    │
│  │      foreach (var adapter in adapters)                            │    │
│  │      {                                                             │    │
│  │          try                                                      │    │
│  │          {                                                         │    │
│  │              return await adapter.BookShipmentAsync(request);     │    │
│  │          }                                                         │    │
│  │          catch (CarrierApiException ex)                           │    │
│  │          {                                                         │    │
│  │              _logger.LogWarning(ex,                               │    │
│  │                  "Carrier {Code} failed, trying next",            │    │
│  │                  adapter.CarrierCode);                            │    │
│  │          }                                                         │    │
│  │      }                                                             │    │
│  │                                                                    │    │
│  │      throw new AllCarriersFailedException();                      │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Variation 3: Factory with Rate Comparison

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FACTORY WITH PARALLEL RATE QUERIES                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  USE CASE: Query all carriers simultaneously for rate comparison            │
│  场景: 同时查询所有承运商进行价格对比                                          │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │  public class CarrierRateComparisonService                         │    │
│  │  {                                                                 │    │
│  │      private readonly ICarrierAdapterFactory _factory;             │    │
│  │                                                                    │    │
│  │      public async Task<IEnumerable<CarrierQuote>> GetAllRatesAsync(│    │
│  │          RateRequest request,                                     │    │
│  │          CancellationToken ct = default)                          │    │
│  │      {                                                             │    │
│  │          var adapters = _factory.GetAvailableAdapters();          │    │
│  │                                                                    │    │
│  │          // Query ALL carriers in parallel                        │    │
│  │          var tasks = adapters.Select(async adapter =>             │    │
│  │          {                                                         │    │
│  │              try                                                  │    │
│  │              {                                                     │    │
│  │                  return await adapter.GetRateAsync(request);      │    │
│  │              }                                                     │    │
│  │              catch (Exception ex)                                 │    │
│  │              {                                                     │    │
│  │                  _logger.LogWarning(ex,                           │    │
│  │                      "Rate query failed for {Carrier}",           │    │
│  │                      adapter.CarrierCode);                        │    │
│  │                  return null;  // Don't fail entire query        │    │
│  │              }                                                     │    │
│  │          });                                                       │    │
│  │                                                                    │    │
│  │          var results = await Task.WhenAll(tasks);                 │    │
│  │                                                                    │    │
│  │          return results                                           │    │
│  │              .Where(r => r != null)                               │    │
│  │              .OrderBy(r => r.TotalCost);                          │    │
│  │      }                                                             │    │
│  │  }                                                                 │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## ⚠️ Anti-Patterns to Avoid

### Anti-Pattern 1: Service Locator (Hidden Dependencies)

```csharp
// ❌ BAD: Factory uses service locator internally
public class BadCarrierAdapterFactory : ICarrierAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;  // Service Locator!
    
    public ICarrierAdapter GetAdapter(string carrierCode)
    {
        // ❌ Hidden dependency - no way to know what's needed from outside
        return carrierCode switch
        {
            "SF" => _serviceProvider.GetRequiredService<SFExpressAdapter>(),
            "JD" => _serviceProvider.GetRequiredService<JDLogisticsAdapter>(),
            _ => throw new Exception()
        };
    }
}

// ✅ GOOD: Dependencies explicit through constructor
public class GoodCarrierAdapterFactory : ICarrierAdapterFactory
{
    private readonly Dictionary<string, ICarrierAdapter> _adapters;
    
    // ✅ All adapters injected - dependencies clear
    public GoodCarrierAdapterFactory(IEnumerable<ICarrierAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(a => a.CarrierCode);
    }
}
```

### Anti-Pattern 2: Factory that Does Too Much

```csharp
// ❌ BAD: Factory handles business logic
public class BadCarrierAdapterFactory : ICarrierAdapterFactory
{
    public async Task<BookingResult> BookWithBestCarrierAsync(
        BookingRequest request)
    {
        // ❌ Factory shouldn't select best carrier - that's business logic!
        var rates = await GetAllRatesAsync(request);
        var best = rates.OrderBy(r => r.Cost).First();
        
        // ❌ Factory shouldn't do booking - that's service logic!
        return await GetAdapter(best.CarrierCode).BookShipmentAsync(request);
    }
}

// ✅ GOOD: Factory only creates/provides adapters
public class GoodCarrierAdapterFactory : ICarrierAdapterFactory
{
    public ICarrierAdapter GetAdapter(string carrierCode) => ...;
    public IEnumerable<ICarrierAdapter> GetAllAdapters() => ...;
    // Selection and booking handled by CarrierService
}
```

### Anti-Pattern 3: Static Factory

```csharp
// ❌ BAD: Static factory - untestable
public static class CarrierAdapterFactory
{
    public static ICarrierAdapter GetAdapter(string code)
    {
        // ❌ Can't mock this in tests
        // ❌ Can't inject dependencies
        return code switch
        {
            "SF" => new SFExpressAdapter(new HttpClient(), ...),
            _ => throw new Exception()
        };
    }
}

// ✅ GOOD: Instance-based factory with interface
public class CarrierAdapterFactory : ICarrierAdapterFactory
{
    // ✅ Mockable in tests
    // ✅ Dependencies injected
    public ICarrierAdapter GetAdapter(string code) { ... }
}
```

---

## 🇨🇳 Chinese Tech References

### CSDN Articles to Study

| Search Keyword | Focus | 推荐等级 |
|---------------|-------|----------|
| `工厂模式 C# 实战` | Basic implementation | ★★★★★ |
| `简单工厂 抽象工厂 区别` | Pattern comparison | ★★★★☆ |
| `依赖注入 工厂模式 结合` | DI + Factory | ★★★★★ |
| `物流系统 工厂模式` | Logistics application | ★★★★☆ |
| `.NET Core DI 工厂` | .NET specific | ★★★★☆ |

### Gitee Repositories

| Repository | Content |
|------------|---------|
| `dotnet-campus/DesignPattern` | C# pattern examples |
| `ABP-CN/abp-samples` | ABP factory patterns |
| `dotnet/runtime` | .NET factory implementations |

---

## 📝 Self-Assessment

### Knowledge Check Questions

| # | Question | Expected Understanding |
|---|----------|----------------------|
| 1 | What's the difference between Simple Factory and Factory Method? | Simple = static method, Factory Method = uses inheritance |
| 2 | Why use Registry-based Factory over Simple Factory? | Open/Closed - add products without modifying factory |
| 3 | How does Factory work with DI in .NET? | DI creates objects, Factory organizes/provides them |
| 4 | When should Factory be Singleton vs Scoped? | Singleton if adapters stateless, Scoped if per-request state |
| 5 | What's Service Locator anti-pattern and how to avoid it? | Hidden dependencies via IServiceProvider - prefer explicit injection |

### Practical Exercises

1. **[ ] Implement Registry-Based Factory**
   - Create ICarrierAdapterFactory interface
   - Implement CarrierAdapterFactory with Dictionary
   - Register all adapters in DI
   - Verify factory discovers new adapters automatically

2. **[ ] Add GetAvailableAdapters() Method**
   - Filter by adapter.IsAvailable
   - Use for fallback chain selection
   - Test with simulated carrier outage

3. **[ ] Implement Parallel Rate Comparison**
   - Use GetAllAdapters() from factory
   - Query rates with Task.WhenAll
   - Handle partial failures gracefully

4. **[ ] Compare with .NET 8 Keyed Services**
   - Implement same scenario with keyed services
   - Document pros/cons vs custom factory
   - Decide which approach fits your use case

### Design Challenge

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    DESIGN CHALLENGE: CARRIER ONBOARDING                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  SCENARIO: Product wants to add new carrier (Best Express 百世快递)          │
│                                                                             │
│  REQUIREMENTS:                                                              │
│  1. Add new carrier with ZERO changes to:                                   │
│     • CarrierService                                                        │
│     • CarrierAdapterFactory                                                 │
│     • Existing adapter classes                                              │
│                                                                             │
│  2. New carrier should be automatically:                                    │
│     • Discovered by factory                                                 │
│     • Included in rate comparisons                                         │
│     • Available in fallback chains                                          │
│                                                                              │
│  DELIVERABLES:                                                               │
│  □ BestExpressAdapter.cs (new file)                                        │
│  □ BestExpressAdapterTests.cs (new file)                                   │
│  □ ONE line in DependencyInjection.cs                                       │
│                                                                             │
│  SUCCESS CRITERIA:                                                          │
│  • Git diff shows only NEW files + 1 line in DI                             │
│  • Existing tests still pass (no regressions)                               │
│  • Factory.GetAdapter("BEST") returns new adapter                           │
│  • Factory.GetAllAdapters() includes Best Express                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔗 Related Documents

- **Applied in**: [02-MULTI-CARRIER.md](../core-domains/02-MULTI-CARRIER.md)
- **Creates products**: [ADAPTER-PATTERN.md](ADAPTER-PATTERN.md)
- **Can combine with**: [STRATEGY-PATTERN.md](STRATEGY-PATTERN.md) (strategy selection)
- **Index**: [00-INDEX.md](../00-INDEX.md)

---