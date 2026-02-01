# 🔀 CQRS Pattern Study Guide (命令查询职责分离学习指南)

> **Status**: 📚 Study Document  
> **Pattern Type**: Architectural Pattern  
> **Primary Application**: Order Processing Read/Write Optimization (订单处理读写优化)  
> **Related Domain**: [04-ORDER-PROCESSING.md](../core-domains/04-ORDER-PROCESSING.md)

---

## 📖 Table of Contents

1. [Pattern Overview](#-pattern-overview)
2. [Problem It Solves](#-problem-it-solves)
3. [Pattern Structure](#-pattern-structure)
4. [Implementation with MediatR](#-implementation-with-mediatr)
5. [Read Model Synchronization](#-read-model-synchronization)
6. [Consistency Considerations](#-consistency-considerations)
7. [When to Use CQRS](#-when-to-use-cqrs)
8. [Anti-Patterns to Avoid](#-anti-patterns-to-avoid)
9. [CQRS + Event Sourcing](#-cqrs--event-sourcing)
10. [Chinese Tech References](#-chinese-tech-references)
11. [Self-Assessment](#-self-assessment)

---

## 🎯 Pattern Overview

### Definition (定义)

> **CQRS (Command Query Responsibility Segregation)** separates read and write operations into different models. Commands change state, Queries return data.
>
> **CQRS（命令查询职责分离）** 将读取和写入操作分离到不同的模型中。命令改变状态，查询返回数据。

### Visual Metaphor: Restaurant Kitchen (餐厅厨房比喻)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    THE RESTAURANT KITCHEN ANALOGY                            │
│                    餐厅厨房的比喻                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Traditional (单一模型):                                                      │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                         ONE COUNTER                                   │   │
│  │                                                                       │   │
│  │   "I want to order" ──▶ ┌─────────┐ ◀── "What's the menu?"           │   │
│  │   "Change my order" ──▶ │  SAME   │ ◀── "Is my food ready?"          │   │
│  │   "Cancel order"    ──▶ │ COUNTER │ ◀── "What's today's special?"    │   │
│  │                         └─────────┘                                   │   │
│  │                                                                       │   │
│  │   Problem: Order-taker overwhelmed answering questions while          │   │
│  │            trying to process orders. Customers wait in same queue.    │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  CQRS (读写分离):                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                                                                       │   │
│  │   COMMAND COUNTER (写端)           QUERY COUNTER (读端)               │   │
│  │   ┌─────────────────┐              ┌─────────────────┐               │   │
│  │   │                 │              │                 │               │   │
│  │   │ "I want to      │              │ "What's the     │               │   │
│  │   │  order..."      │              │  menu?"         │               │   │
│  │   │                 │              │                 │               │   │
│  │   │ "Change my      │              │ "Is my food     │               │   │
│  │   │  order..."      │              │  ready?"        │               │   │
│  │   │                 │              │                 │               │   │
│  │   │ Takes time,     │              │ Fast answers,   │               │   │
│  │   │ validates,      │              │ pre-computed    │               │   │
│  │   │ processes       │              │ info            │               │   │
│  │   │                 │              │                 │               │   │
│  │   └────────┬────────┘              └────────▲────────┘               │   │
│  │            │                                │                        │   │
│  │            │        KITCHEN SYNC            │                        │   │
│  │            └─────▶ (厨房同步) ───────────────┘                        │   │
│  │                    Orders placed update                              │   │
│  │                    the status board                                  │   │
│  │                                                                       │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  KEY INSIGHT:                                                                │
│  • Queries don't need to wait for write operations                          │
│  • Write operations focus on business logic, not query optimization         │
│  • Status board (Read Model) is eventually consistent with kitchen          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Core Concepts (核心概念)

| Concept | Description | Order Processing Example |
|---------|-------------|--------------------------|
| **Command** | Intent to change state | `CreateOrderCommand`, `CancelOrderCommand` |
| **Query** | Request for data (no side effects) | `GetOrderByIdQuery`, `ListOrdersQuery` |
| **Write Model** | Optimized for business logic | `Order` aggregate with state machine |
| **Read Model** | Optimized for queries | `OrderListView`, `OrderDetailView` |
| **Synchronization** | Keeping read model updated | Domain events → Projection handlers |

---

## 🔥 Problem It Solves

### The Traditional Approach (传统方式)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ❌ SINGLE MODEL APPROACH                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  public class OrderService                                                   │
│  {                                                                           │
│      private readonly OrderDbContext _context;                               │
│                                                                              │
│      // WRITE: Complex business logic                                        │
│      public async Task<Order> CreateOrder(CreateOrderRequest request)        │
│      {                                                                       │
│          var order = new Order(...)                                          │
│          {                                                                   │
│              // Rich domain model                                            │
│              // State machine                                                │
│              // Business rules                                               │
│              // Validations                                                  │
│          };                                                                  │
│          _context.Orders.Add(order);                                         │
│          await _context.SaveChangesAsync();                                  │
│          return order;                                                       │
│      }                                                                       │
│                                                                              │
│      // READ: Complex joins to get display data                              │
│      public async Task<OrderDto> GetOrderById(Guid id)                       │
│      {                                                                       │
│          return await _context.Orders                                        │
│              .Include(o => o.Items)                                          │
│              .Include(o => o.Customer)                                       │
│              .Include(o => o.Shipments)                                      │
│                  .ThenInclude(s => s.TrackingEvents)                         │
│              .Include(o => o.Payments)                                       │
│              .Include(o => o.StatusHistory)                                  │
│              .Where(o => o.Id == id)                                         │
│              .Select(o => new OrderDto                                       │
│              {                                                               │
│                  // Map everything...                                        │
│                  // 50+ properties                                           │
│              })                                                              │
│              .FirstOrDefaultAsync();                                         │
│      }                                                                       │
│                                                                              │
│      // PROBLEM 1: Write model has relationships for display only            │
│      // PROBLEM 2: Read queries lock tables during writes                    │
│      // PROBLEM 3: Cannot optimize read/write independently                  │
│      // PROBLEM 4: Single database becomes bottleneck                        │
│  }                                                                           │
│                                                                              │
│  Performance Issues:                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Writes: 100/sec (business logic + validation)                      │    │
│  │  Reads:  10,000/sec needed (dashboard, tracking, API)               │    │
│  │                                                                     │    │
│  │  With single model:                                                 │    │
│  │  • Reads blocked during writes (table locks)                        │    │
│  │  • Cannot scale reads without scaling writes                        │    │
│  │  • Join-heavy queries slow down                                     │    │
│  │  • 800ms average response time                                      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Real-World Pain Point (真实痛点)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    双11订单系统演进 (Double 11 Order System Evolution)        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Phase 1 (2015): Single database, single model                              │
│  ────────────────────────────────────────────                               │
│  • 100K orders/hour                                                         │
│  • Database CPU 95% during peak                                             │
│  • Query response: 2-5 seconds                                              │
│  • Customer complaints: "Can't see my order"                                │
│                                                                              │
│  Phase 2 (2017): Read replicas                                              │
│  ────────────────────────────────────────────                               │
│  • Writes to master, reads from replicas                                    │
│  • 500K orders/hour                                                         │
│  • Problem: Replication lag (10-30 seconds)                                 │
│  • Customer: "Order not found" (just placed it!)                            │
│                                                                              │
│  Phase 3 (2019): CQRS adoption                                              │
│  ────────────────────────────────────────────                               │
│  • Separate read models (Redis + Elasticsearch)                             │
│  • Write model: SQL Server (normalized)                                     │
│  • Read model: Denormalized, pre-computed                                   │
│  • 5M orders/hour                                                           │
│  • Query response: 50-100ms                                                 │
│  • Eventual consistency: 100-200ms                                          │
│                                                                              │
│  Phase 4 (2023): CQRS + Event Sourcing                                      │
│  ────────────────────────────────────────────                               │
│  • Complete audit trail                                                     │
│  • Replay capability for debugging                                          │
│  • Multiple read models for different use cases                             │
│  • 50M+ orders/hour (京东2023双11)                                          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🏗️ Pattern Structure

### Architecture Diagram (架构图)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CQRS ARCHITECTURE                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                              ┌─────────────┐                                │
│                              │   Client    │                                │
│                              │   (App/API) │                                │
│                              └──────┬──────┘                                │
│                                     │                                        │
│                    ┌────────────────┴────────────────┐                      │
│                    │                                 │                      │
│                    ▼                                 ▼                      │
│  ╔═════════════════════════════════╗   ╔═════════════════════════════════╗ │
│  ║         COMMAND SIDE            ║   ║          QUERY SIDE             ║ │
│  ║         (写端)                   ║   ║          (读端)                  ║ │
│  ╠═════════════════════════════════╣   ╠═════════════════════════════════╣ │
│  ║                                 ║   ║                                 ║ │
│  ║  ┌───────────────────────────┐  ║   ║  ┌───────────────────────────┐  ║ │
│  ║  │    Command Handler        │  ║   ║  │     Query Handler         │  ║ │
│  ║  │    (命令处理器)            │  ║   ║  │     (查询处理器)           │  ║ │
│  ║  │                           │  ║   ║  │                           │  ║ │
│  ║  │  • Validate command       │  ║   ║  │  • Direct database read   │  ║ │
│  ║  │  • Load aggregate         │  ║   ║  │  • No domain logic        │  ║ │
│  ║  │  • Execute domain logic   │  ║   ║  │  • Return DTO directly    │  ║ │
│  ║  │  • Persist changes        │  ║   ║  │  • Cache if applicable    │  ║ │
│  ║  │  • Publish events         │  ║   ║  │                           │  ║ │
│  ║  └─────────────┬─────────────┘  ║   ║  └─────────────┬─────────────┘  ║ │
│  ║                │                ║   ║                │                ║ │
│  ║                ▼                ║   ║                ▼                ║ │
│  ║  ┌───────────────────────────┐  ║   ║  ┌───────────────────────────┐  ║ │
│  ║  │      Write Model          │  ║   ║  │       Read Model          │  ║ │
│  ║  │      (写模型)              │  ║   ║  │       (读模型)             │  ║ │
│  ║  │                           │  ║   ║  │                           │  ║ │
│  ║  │  ┌─────────────────────┐  │  ║   ║  │  ┌─────────────────────┐  │  ║ │
│  ║  │  │   Order Aggregate   │  │  ║   ║  │  │   OrderListView    │  │  ║ │
│  ║  │  │   - State machine   │  │  ║   ║  │  │   - Denormalized   │  │  ║ │
│  ║  │  │   - Business rules  │  │  ║   ║  │  │   - Pre-computed   │  │  ║ │
│  ║  │  │   - Domain events   │  │  ║   ║  │  │   - Query-optimized│  │  ║ │
│  ║  │  └─────────────────────┘  │  ║   ║  │  └─────────────────────┘  │  ║ │
│  ║  │                           │  ║   ║  │                           │  ║ │
│  ║  └─────────────┬─────────────┘  ║   ║  └─────────────▲─────────────┘  ║ │
│  ║                │                ║   ║                │                ║ │
│  ║                ▼                ║   ║                │                ║ │
│  ║  ┌───────────────────────────┐  ║   ║                │                ║ │
│  ║  │    Write Database         │  ║   ║                │                ║ │
│  ║  │    (SQL Server)           │  ║   ║                │                ║ │
│  ║  │    - Normalized           │  ║   ║                │                ║ │
│  ║  │    - Transactional        │  ║   ║                │                ║ │
│  ║  └─────────────┬─────────────┘  ║   ╚════════════════╪════════════════╝ │
│  ║                │                ║                    │                  │
│  ╚════════════════╪════════════════╝                    │                  │
│                   │                                     │                  │
│                   │        SYNCHRONIZATION              │                  │
│                   │        (同步机制)                    │                  │
│                   ▼                                     │                  │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    Message Queue (RabbitMQ/Kafka)                    │   │
│  │                                                                      │   │
│  │   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐             │   │
│  │   │OrderCreated │    │OrderUpdated │    │OrderStatus  │             │   │
│  │   │   Event     │    │   Event     │    │  Changed    │             │   │
│  │   └──────┬──────┘    └──────┬──────┘    └──────┬──────┘             │   │
│  │          └──────────────────┴──────────────────┘                    │   │
│  │                             │                                        │   │
│  └─────────────────────────────┼────────────────────────────────────────┘   │
│                                │                                            │
│                                ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    Projection Handler (投影处理器)                    │   │
│  │                                                                      │   │
│  │   Handle(OrderCreatedEvent):                                         │   │
│  │     → Insert into OrderListView                                      │   │
│  │     → Update Redis cache                                             │   │
│  │     → Index in Elasticsearch                                         │   │
│  │                                                                      │   │
│  └──────────────────────────────────────────────────────┬───────────────┘   │
│                                                         │                   │
│                                                         ▼                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    Read Database(s)                                  │   │
│  │   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐               │   │
│  │   │   Redis     │   │ Elasticsearch│   │  SQL Views  │               │   │
│  │   │   Cache     │   │   Search    │   │  (Reports)  │               │   │
│  │   └─────────────┘   └─────────────┘   └─────────────┘               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Command vs Query Comparison (命令与查询对比)

| Aspect | Command (命令) | Query (查询) |
|--------|---------------|--------------|
| **Purpose** | Change system state | Read system state |
| **Side Effects** | Yes (always) | No (never) |
| **Idempotency** | Should be (with ID) | Always |
| **Validation** | Full business validation | None (data is valid) |
| **Model** | Rich domain model | DTO/View model |
| **Database** | Normalized, transactional | Denormalized, eventually consistent |
| **Caching** | Not applicable | Highly cacheable |
| **Scaling** | Harder (business logic) | Easier (stateless reads) |

---

## 💻 Implementation with MediatR

### Project Structure (项目结构)

```
DT.Express.Application/
├── Orders/
│   ├── Commands/
│   │   ├── CreateOrder/
│   │   │   ├── CreateOrderCommand.cs
│   │   │   ├── CreateOrderCommandHandler.cs
│   │   │   └── CreateOrderCommandValidator.cs
│   │   ├── DispatchOrder/
│   │   │   ├── DispatchOrderCommand.cs
│   │   │   └── DispatchOrderCommandHandler.cs
│   │   └── CancelOrder/
│   │       ├── CancelOrderCommand.cs
│   │       └── CancelOrderCommandHandler.cs
│   │
│   ├── Queries/
│   │   ├── GetOrderById/
│   │   │   ├── GetOrderByIdQuery.cs
│   │   │   ├── GetOrderByIdQueryHandler.cs
│   │   │   └── OrderDetailDto.cs
│   │   ├── ListOrders/
│   │   │   ├── ListOrdersQuery.cs
│   │   │   ├── ListOrdersQueryHandler.cs
│   │   │   └── OrderSummaryDto.cs
│   │   └── SearchOrders/
│   │       ├── SearchOrdersQuery.cs
│   │       └── SearchOrdersQueryHandler.cs
│   │
│   └── EventHandlers/
│       ├── OrderCreatedEventHandler.cs
│       └── OrderStatusChangedEventHandler.cs
```

### Command Implementation (命令实现)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    COMMAND: CreateOrderCommand                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  // Command Definition                                                       │
│  public record CreateOrderCommand : IRequest<CreateOrderResult>              │
│  {                                                                           │
│      public Guid? IdempotencyKey { get; init; }  // For duplicate detection  │
│      public CustomerInfo Customer { get; init; }                             │
│      public AddressDto ShippingAddress { get; init; }                        │
│      public List<OrderItemDto> Items { get; init; }                          │
│      public ServiceLevel ServiceLevel { get; init; }                         │
│      public string? Notes { get; init; }                                     │
│  }                                                                           │
│                                                                              │
│  // Command Handler                                                          │
│  public class CreateOrderCommandHandler                                      │
│      : IRequestHandler<CreateOrderCommand, CreateOrderResult>                │
│  {                                                                           │
│      private readonly IOrderRepository _orderRepository;                     │
│      private readonly IAddressValidator _addressValidator;                   │
│      private readonly IEventPublisher _eventPublisher;                       │
│                                                                              │
│      public async Task<CreateOrderResult> Handle(                            │
│          CreateOrderCommand request,                                         │
│          CancellationToken cancellationToken)                                │
│      {                                                                       │
│          // 1. Check for duplicate (idempotency)                             │
│          if (request.IdempotencyKey.HasValue)                                │
│          {                                                                   │
│              var existing = await _orderRepository                           │
│                  .FindByIdempotencyKeyAsync(request.IdempotencyKey.Value);   │
│              if (existing != null)                                           │
│                  return CreateOrderResult.FromExisting(existing);            │
│          }                                                                   │
│                                                                              │
│          // 2. Validate shipping address                                     │
│          var addressResult = await _addressValidator                         │
│              .ValidateAsync(request.ShippingAddress);                        │
│          if (!addressResult.IsValid)                                         │
│              throw new ValidationException(addressResult.Errors);            │
│                                                                              │
│          // 3. Create order (domain logic)                                   │
│          var order = Order.Create(                                           │
│              request.Customer,                                               │
│              addressResult.NormalizedAddress,                                │
│              request.Items.Select(i => new OrderItem(i)).ToList(),           │
│              request.ServiceLevel,                                           │
│              request.Notes,                                                  │
│              request.IdempotencyKey);                                        │
│                                                                              │
│          // 4. Persist                                                       │
│          await _orderRepository.AddAsync(order);                             │
│          await _orderRepository.UnitOfWork.SaveChangesAsync();               │
│                                                                              │
│          // 5. Publish domain events (for read model sync)                   │
│          await _eventPublisher.PublishAsync(order.DomainEvents);             │
│                                                                              │
│          // 6. Return result                                                 │
│          return new CreateOrderResult                                        │
│          {                                                                   │
│              OrderId = order.Id,                                             │
│              OrderNumber = order.OrderNumber,                                │
│              Status = order.Status,                                          │
│              CreatedAt = order.CreatedAt                                     │
│          };                                                                  │
│      }                                                                       │
│  }                                                                           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Query Implementation (查询实现)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    QUERY: GetOrderByIdQuery                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  // Query Definition                                                         │
│  public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailDto>;   │
│                                                                              │
│  // Query Handler - READS FROM READ MODEL                                    │
│  public class GetOrderByIdQueryHandler                                       │
│      : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>                    │
│  {                                                                           │
│      private readonly IReadOnlyDbContext _readDb;                            │
│      private readonly IDistributedCache _cache;                              │
│                                                                              │
│      public async Task<OrderDetailDto> Handle(                               │
│          GetOrderByIdQuery request,                                          │
│          CancellationToken cancellationToken)                                │
│      {                                                                       │
│          // 1. Try cache first                                               │
│          var cacheKey = $"order:{request.OrderId}";                          │
│          var cached = await _cache.GetAsync<OrderDetailDto>(cacheKey);       │
│          if (cached != null)                                                 │
│              return cached;                                                  │
│                                                                              │
│          // 2. Read from denormalized read model (NO JOINS!)                 │
│          var order = await _readDb.OrderDetailViews                          │
│              .FirstOrDefaultAsync(                                           │
│                  o => o.Id == request.OrderId,                               │
│                  cancellationToken);                                         │
│                                                                              │
│          if (order == null)                                                  │
│              throw new NotFoundException(nameof(Order), request.OrderId);    │
│                                                                              │
│          // 3. Map to DTO (or read model IS the DTO)                         │
│          var dto = MapToDto(order);                                          │
│                                                                              │
│          // 4. Cache for future requests                                     │
│          await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));      │
│                                                                              │
│          return dto;                                                         │
│      }                                                                       │
│  }                                                                           │
│                                                                              │
│  // NOTE: No Include(), no joins, no domain logic!                           │
│  // Read model is already optimized for this exact query                     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### MediatR Pipeline Behaviors (MediatR管道行为)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    MEDIATR PIPELINE                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Request Flow:                                                               │
│                                                                              │
│  ┌──────────┐    ┌──────────────────────────────────────────────────────┐   │
│  │ Controller│───▶│                  MediatR Pipeline                    │   │
│  └──────────┘    │                                                      │   │
│                  │   ┌────────────────────────────────────────────────┐ │   │
│                  │   │ 1. LoggingBehavior                             │ │   │
│                  │   │    - Log request/response                      │ │   │
│                  │   │    - Track timing                              │ │   │
│                  │   └───────────────────────────────────────────────┬┘ │   │
│                  │                                                    │  │   │
│                  │   ┌───────────────────────────────────────────────┴┐ │   │
│                  │   │ 2. ValidationBehavior (Commands only)          │ │   │
│                  │   │    - Run FluentValidation                      │ │   │
│                  │   │    - Throw ValidationException if fails        │ │   │
│                  │   └───────────────────────────────────────────────┬┘ │   │
│                  │                                                    │  │   │
│                  │   ┌───────────────────────────────────────────────┴┐ │   │
│                  │   │ 3. TransactionBehavior (Commands only)         │ │   │
│                  │   │    - Begin transaction                         │ │   │
│                  │   │    - Commit on success, rollback on failure    │ │   │
│                  │   └───────────────────────────────────────────────┬┘ │   │
│                  │                                                    │  │   │
│                  │   ┌───────────────────────────────────────────────┴┐ │   │
│                  │   │ 4. CachingBehavior (Queries only)              │ │   │
│                  │   │    - Check cache                               │ │   │
│                  │   │    - Return cached if available                │ │   │
│                  │   └───────────────────────────────────────────────┬┘ │   │
│                  │                                                    │  │   │
│                  │   ┌───────────────────────────────────────────────┴┐ │   │
│                  │   │ 5. Handler                                      │ │   │
│                  │   │    - Command: Execute business logic            │ │   │
│                  │   │    - Query: Read from database                  │ │   │
│                  │   └────────────────────────────────────────────────┘ │   │
│                  │                                                      │   │
│                  └──────────────────────────────────────────────────────┘   │
│                                                                              │
│  Configuration:                                                              │
│                                                                              │
│  services.AddMediatR(cfg => {                                                │
│      cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);             │
│      cfg.AddBehavior<IPipelineBehavior<,>, LoggingBehavior<,>>();           │
│      cfg.AddBehavior<IPipelineBehavior<,>, ValidationBehavior<,>>();        │
│      cfg.AddBehavior<IPipelineBehavior<,>, TransactionBehavior<,>>();       │
│      cfg.AddBehavior<IPipelineBehavior<,>, CachingBehavior<,>>();           │
│  });                                                                         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Read Model Synchronization

### Synchronization Strategies (同步策略)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SYNCHRONIZATION STRATEGIES                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Strategy 1: Event-Driven (事件驱动) - RECOMMENDED                     │  │
│  │  ─────────────────────────────────────────────────────────────────────│  │
│  │                                                                       │  │
│  │  Command Handler  ──▶  Message Queue  ──▶  Projection Handler         │  │
│  │       │                    │                     │                    │  │
│  │       │ 1. Save to         │ 2. Publish          │ 3. Update         │  │
│  │       │    Write DB        │    Event            │    Read Model      │  │
│  │       ▼                    ▼                     ▼                    │  │
│  │  [Write DB]           [RabbitMQ]            [Read DBs]                │  │
│  │                                                                       │  │
│  │  Pros: Decoupled, scalable, resilient (retry on failure)             │  │
│  │  Cons: Eventual consistency (50-200ms delay)                         │  │
│  │  Use: Standard operations, high throughput                           │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Strategy 2: Synchronous Dual-Write (同步双写)                         │  │
│  │  ─────────────────────────────────────────────────────────────────────│  │
│  │                                                                       │  │
│  │  Command Handler                                                      │  │
│  │       │                                                               │  │
│  │       ├──▶ 1. Save to Write DB                                        │  │
│  │       │         │                                                     │  │
│  │       │         │ (same transaction)                                  │  │
│  │       │         │                                                     │  │
│  │       └──▶ 2. Update Read Model ◀──┘                                  │  │
│  │                                                                       │  │
│  │  Pros: Strong consistency                                            │  │
│  │  Cons: Coupled, slower, single point of failure                      │  │
│  │  Use: Critical data that must be immediately consistent              │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Strategy 3: Change Data Capture (CDC) (变更数据捕获)                  │  │
│  │  ─────────────────────────────────────────────────────────────────────│  │
│  │                                                                       │  │
│  │  Write DB ──▶ Transaction Log ──▶ Debezium ──▶ Read Models            │  │
│  │                                                                       │  │
│  │  Pros: No application code changes, captures all changes             │  │
│  │  Cons: Infrastructure complexity                                     │  │
│  │  Use: Legacy systems, database-heavy applications                    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Strategy 4: Scheduled Rebuild (定时重建)                              │  │
│  │  ─────────────────────────────────────────────────────────────────────│  │
│  │                                                                       │  │
│  │  Scheduled Job (e.g., every hour)                                     │  │
│  │       │                                                               │  │
│  │       ├──▶ Read all orders from Write DB                              │  │
│  │       │                                                               │  │
│  │       └──▶ Rebuild Read Model completely                              │  │
│  │                                                                       │  │
│  │  Pros: Simple, self-healing                                          │  │
│  │  Cons: Stale data between rebuilds, resource-intensive               │  │
│  │  Use: Analytics, reports, non-critical views                         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Projection Handler Example (投影处理器示例)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PROJECTION HANDLER                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  public class OrderProjectionHandler :                                       │
│      INotificationHandler<OrderCreatedEvent>,                                │
│      INotificationHandler<OrderStatusChangedEvent>,                          │
│      INotificationHandler<OrderCancelledEvent>                               │
│  {                                                                           │
│      private readonly ReadDbContext _readDb;                                 │
│      private readonly IDistributedCache _cache;                              │
│      private readonly IElasticClient _elastic;                               │
│                                                                              │
│      public async Task Handle(                                               │
│          OrderCreatedEvent e,                                                │
│          CancellationToken ct)                                               │
│      {                                                                       │
│          // 1. Insert into SQL read model                                    │
│          var listView = new OrderListView                                    │
│          {                                                                   │
│              Id = e.OrderId,                                                 │
│              OrderNumber = e.OrderNumber,                                    │
│              CustomerName = e.CustomerName,                                  │
│              Status = (int)OrderStatus.CREATED,                              │
│              StatusDisplayName = "已创建",                                   │
│              TotalAmount = e.TotalAmount,                                    │
│              ItemCount = e.ItemCount,                                        │
│              CreatedAt = e.OccurredAt,                                       │
│              LastUpdatedAt = e.OccurredAt                                    │
│          };                                                                  │
│          _readDb.OrderListViews.Add(listView);                               │
│          await _readDb.SaveChangesAsync(ct);                                 │
│                                                                              │
│          // 2. Update Redis cache                                            │
│          var detailView = new OrderDetailView { /* ... */ };                 │
│          await _cache.SetAsync(                                              │
│              $"order:{e.OrderId}",                                           │
│              detailView,                                                     │
│              TimeSpan.FromMinutes(30));                                      │
│                                                                              │
│          // 3. Index in Elasticsearch for search                             │
│          await _elastic.IndexDocumentAsync(new OrderSearchDocument           │
│          {                                                                   │
│              Id = e.OrderId,                                                 │
│              OrderNumber = e.OrderNumber,                                    │
│              CustomerName = e.CustomerName,                                  │
│              // Full-text searchable fields                                  │
│          });                                                                 │
│      }                                                                       │
│                                                                              │
│      public async Task Handle(                                               │
│          OrderStatusChangedEvent e,                                          │
│          CancellationToken ct)                                               │
│      {                                                                       │
│          // 1. Update SQL read model                                         │
│          var view = await _readDb.OrderListViews.FindAsync(e.OrderId);       │
│          if (view != null)                                                   │
│          {                                                                   │
│              view.Status = (int)e.NewStatus;                                 │
│              view.StatusDisplayName = GetDisplayName(e.NewStatus);           │
│              view.LastUpdatedAt = e.OccurredAt;                              │
│              await _readDb.SaveChangesAsync(ct);                             │
│          }                                                                   │
│                                                                              │
│          // 2. Invalidate cache (will be refreshed on next read)             │
│          await _cache.RemoveAsync($"order:{e.OrderId}");                     │
│                                                                              │
│          // 3. Update Elasticsearch                                          │
│          await _elastic.UpdateAsync<OrderSearchDocument>(                    │
│              e.OrderId,                                                      │
│              u => u.Doc(new { Status = (int)e.NewStatus }));                 │
│      }                                                                       │
│  }                                                                           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## ⚖️ Consistency Considerations

### Eventual Consistency Explained (最终一致性解释)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    EVENTUAL CONSISTENCY TIMELINE                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  T=0ms      User creates order                                              │
│     │                                                                        │
│     ▼                                                                        │
│  T=50ms     Write DB committed ✅                                            │
│     │       └── Order exists in write model                                  │
│     │       └── Response returned to user                                    │
│     │                                                                        │
│     ▼                                                                        │
│  T=100ms    Event published to queue                                        │
│     │                                                                        │
│     ▼                                                                        │
│  T=150ms    Projection handler processes event                              │
│     │       └── Read model updated ✅                                        │
│     │       └── Cache updated ✅                                             │
│     │       └── Search index updated ✅                                      │
│     │                                                                        │
│     ▼                                                                        │
│  T=200ms    Read model fully consistent                                     │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  CONSISTENCY WINDOW: 0-200ms                                        │    │
│  │                                                                     │    │
│  │  During this window:                                                │    │
│  │  • Write model: Has the order ✅                                    │    │
│  │  • Read model: May not have the order ⚠️                            │    │
│  │                                                                     │    │
│  │  User experience:                                                   │    │
│  │  • "Order created!" shows new order number                          │    │
│  │  • If user immediately searches, might not find it                  │    │
│  │  • After 200ms, order visible everywhere                            │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Handling Consistency Issues (处理一致性问题)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CONSISTENCY HANDLING STRATEGIES                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Problem 1: "Order not found" after creation                                │
│  ─────────────────────────────────────────────                              │
│                                                                              │
│  Solution A: Return created data in response                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  POST /orders                                                       │    │
│  │  Response: {                                                        │    │
│  │    "orderId": "123",                                                │    │
│  │    "orderNumber": "ORD-001",                                        │    │
│  │    "status": "CREATED",                                             │    │
│  │    ... // All data user needs                                       │    │
│  │  }                                                                  │    │
│  │  // User doesn't need to query immediately                          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  Solution B: Read-your-writes consistency                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  // If order just created, read from write model                    │    │
│  │  public async Task<OrderDto> GetOrder(Guid id)                      │    │
│  │  {                                                                  │    │
│  │      var cached = await _readModel.FindAsync(id);                   │    │
│  │      if (cached != null) return cached;                             │    │
│  │                                                                     │    │
│  │      // Fallback to write model (eventual consistency window)       │    │
│  │      var order = await _writeModel.FindAsync(id);                   │    │
│  │      if (order != null) return MapToDto(order);                     │    │
│  │                                                                     │    │
│  │      throw new NotFoundException();                                 │    │
│  │  }                                                                  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  Problem 2: User sees stale data                                            │
│  ─────────────────────────────────────────────                              │
│                                                                              │
│  Solution: Show "Last updated" timestamp                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Order Status: DISPATCHED                                           │    │
│  │  Last updated: 2 seconds ago                                        │    │
│  │  [Refresh]                                                          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  Problem 3: Projection handler fails                                        │
│  ─────────────────────────────────────────────                              │
│                                                                              │
│  Solution: Retry + Dead Letter Queue                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Event ──▶ Handler ──▶ [Fail] ──▶ Retry (3x) ──▶ Dead Letter Queue  │    │
│  │                                                                     │    │
│  │  Monitoring: Alert if DLQ has messages                              │    │
│  │  Recovery: Manual investigation or scheduled retry                  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## ✅ When to Use CQRS

### Decision Matrix (决策矩阵)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CQRS DECISION MATRIX                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Score each factor (1-5), sum the total:                                    │
│                                                                              │
│  ┌────────────────────────────────────┬───────┬─────────────────────────┐   │
│  │ Factor                             │ Score │ Explanation             │   │
│  ├────────────────────────────────────┼───────┼─────────────────────────┤   │
│  │ Read/Write ratio > 10:1            │   5   │ Many more reads         │   │
│  │ Read/Write ratio 5:1 - 10:1        │   3   │ Moderate read-heavy     │   │
│  │ Read/Write ratio < 5:1             │   1   │ Balanced                │   │
│  ├────────────────────────────────────┼───────┼─────────────────────────┤   │
│  │ Complex domain with business rules │   5   │ Rich write model needed │   │
│  │ Moderate domain complexity         │   3   │ Some business rules     │   │
│  │ Simple CRUD operations             │   1   │ No complex rules        │   │
│  ├────────────────────────────────────┼───────┼─────────────────────────┤   │
│  │ Multiple read representations      │   5   │ Dashboard, API, Search  │   │
│  │ Few read representations           │   2   │ One or two views        │   │
│  ├────────────────────────────────────┼───────┼─────────────────────────┤   │
│  │ High performance requirements      │   5   │ <100ms response needed  │   │
│  │ Standard performance               │   2   │ <1s acceptable          │   │
│  ├────────────────────────────────────┼───────┼─────────────────────────┤   │
│  │ Team familiar with CQRS            │   3   │ Know the patterns       │   │
│  │ Team new to CQRS                   │  -2   │ Learning curve cost     │   │
│  └────────────────────────────────────┴───────┴─────────────────────────┘   │
│                                                                              │
│  Total Score Interpretation:                                                 │
│  • 15+: Strongly consider CQRS ✅                                           │
│  • 10-14: CQRS could help, evaluate carefully                               │
│  • <10: Probably don't need CQRS ❌                                         │
│                                                                              │
│  Example: Order Processing System                                            │
│  • Read/Write ratio: 10:1 (dashboard, tracking, API) → 5                    │
│  • Complex domain: State machine, validation → 5                            │
│  • Multiple views: List, Detail, Search, Stats → 5                          │
│  • Performance: <100ms for queries → 5                                      │
│  • Team experience: Moderate → 2                                            │
│  • Total: 22 → CQRS is a good fit ✅                                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### When NOT to Use CQRS (何时不使用)

| Scenario | Why Not CQRS |
|----------|--------------|
| Simple CRUD application | Overhead not worth it |
| Balanced read/write ratio | No benefit from separation |
| Strong consistency required everywhere | Eventual consistency unacceptable |
| Small team with no CQRS experience | Learning curve too steep |
| Tight deadline | Additional complexity |
| Single database without scaling needs | Complexity without benefit |

---

## ⚠️ Anti-Patterns to Avoid

### Anti-Pattern 1: Queries That Modify State (查询修改状态)

```
❌ BAD: Query handler updates data

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await _context.Orders.FindAsync(request.OrderId);
        
        // ❌ WRONG! Queries should NEVER modify state
        order.LastViewedAt = DateTime.UtcNow;
        order.ViewCount++;
        await _context.SaveChangesAsync();
        
        return MapToDto(order);
    }
}

✅ GOOD: Use a separate command for tracking views, or event-based tracking
```

### Anti-Pattern 2: Command That Returns Complex Data (命令返回复杂数据)

```
❌ BAD: Command returns full entity with joins

public async Task<OrderWithAllDetailsDto> Handle(CreateOrderCommand cmd, ...)
{
    var order = Order.Create(...);
    await _repository.AddAsync(order);
    
    // ❌ WRONG! Don't query complex data in command handler
    return await _context.Orders
        .Include(o => o.Items)
        .Include(o => o.Customer)
        .Include(o => o.Shipments)
        .Select(o => new OrderWithAllDetailsDto { ... })
        .FirstAsync();
}

✅ GOOD: Return only essential data (ID, status), client queries separately

public async Task<CreateOrderResult> Handle(CreateOrderCommand cmd, ...)
{
    var order = Order.Create(...);
    await _repository.AddAsync(order);
    
    return new CreateOrderResult
    {
        OrderId = order.Id,
        OrderNumber = order.OrderNumber,
        Status = order.Status
    };
}
```

### Anti-Pattern 3: Shared Model Between Read and Write (读写共享模型)

```
❌ BAD: Same entity class for read and write

// Used everywhere - write handlers AND queries
public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; }
    // ... 50 more properties
    
    // Domain methods for write
    public void Confirm() { ... }
    public void Cancel() { ... }
    
    // Computed properties for read
    public string StatusDisplayName => ...;
    public decimal TotalWithTax => ...;
}

✅ GOOD: Separate models

// Write model - rich domain
public class Order : AggregateRoot
{
    private IOrderState _state;
    public void Confirm() => _state.Confirm(this);
}

// Read model - optimized for display
public class OrderListView  // Denormalized
{
    public Guid Id { get; set; }
    public string StatusDisplayName { get; set; }  // Pre-computed
    public decimal TotalWithTax { get; set; }  // Pre-calculated
}
```

### Anti-Pattern 4: Synchronous Read Model Update (同步更新读模型)

```
❌ BAD: Update read model in same transaction

public async Task Handle(CreateOrderCommand cmd, ...)
{
    using var transaction = await _context.BeginTransactionAsync();
    
    // Write to write model
    var order = Order.Create(...);
    _context.Orders.Add(order);
    
    // ❌ WRONG! Coupling write and read in same transaction
    var readView = new OrderListView { ... };
    _readContext.OrderListViews.Add(readView);
    
    await _context.SaveChangesAsync();
    await _readContext.SaveChangesAsync();
    await transaction.CommitAsync();
}

✅ GOOD: Use events for eventual consistency

public async Task Handle(CreateOrderCommand cmd, ...)
{
    var order = Order.Create(...);
    await _repository.AddAsync(order);
    await _repository.UnitOfWork.SaveChangesAsync();
    
    // Publish event - projection handler updates read model
    await _eventPublisher.PublishAsync(order.DomainEvents);
}
```

---

## 🔗 CQRS + Event Sourcing

### Why Combine? (为何组合?)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CQRS + EVENT SOURCING                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  CQRS alone:                                                                │
│  • Separate read/write models                                               │
│  • Write model can still use traditional storage                            │
│                                                                              │
│  Event Sourcing:                                                            │
│  • Store all changes as events                                              │
│  • Rebuild state by replaying events                                        │
│                                                                              │
│  Combined (CQRS + ES):                                                      │
│  • Write side: Stores events only                                           │
│  • Read side: Projections from events                                       │
│  • Natural fit: Events drive read model updates                             │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                                                                     │    │
│  │   Command ──▶ Aggregate ──▶ Events ──▶ Event Store                  │    │
│  │                               │                                     │    │
│  │                               │ (same events)                       │    │
│  │                               ▼                                     │    │
│  │                        Projection Handlers                          │    │
│  │                               │                                     │    │
│  │                   ┌───────────┼───────────┐                         │    │
│  │                   ▼           ▼           ▼                         │    │
│  │               [Redis]   [Elastic]   [SQL View]                      │    │
│  │                                                                     │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  Benefits:                                                                   │
│  • Complete audit trail (all events stored)                                 │
│  • Can rebuild read models from scratch                                     │
│  • Can create new read models for past data                                 │
│  • Time-travel debugging                                                    │
│                                                                              │
│  Used by: 京东物流, 顺丰快递, 蚂蚁金服                                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🇨🇳 Chinese Tech References

### Industry Examples (行业案例)

| Company | CQRS Implementation | Key Learning |
|---------|---------------------|--------------|
| 京东物流 | Full CQRS + Event Sourcing | 50M+ orders/day on Double 11 |
| 阿里巴巴 | Selective CQRS for high-read APIs | Focus on read-heavy scenarios |
| 蚂蚁金服 | CQRS for transaction history | Event sourcing for audit |
| 美团 | CQRS for order tracking | Redis-based read models |

### Chinese Tech Community Resources

| Platform | Search Keywords | Content Type |
|----------|-----------------|--------------|
| CSDN | `CQRS 订单系统 实战` | Implementation tutorials |
| 掘金 | `命令查询分离 物流` | Case studies |
| 知乎 | `CQRS 什么时候用` | Architecture discussions |
| Gitee | `CQRS-Sample` | Open source examples |

### Recommended Articles (推荐文章)

```
1. "京东物流订单系统CQRS实践" - 京东技术
   内容：如何用CQRS支撑双11高峰

2. "CQRS落地中的坑与解决方案" - 美团技术
   内容：实际落地经验与最终一致性处理

3. "从单体到CQRS：订单系统演进之路" - 阿里技术
   内容：渐进式CQRS改造方法

4. "CQRS + Event Sourcing 在金融系统的应用" - 蚂蚁金服
   内容：强审计要求下的架构选择
```

---

## 📝 Self-Assessment

### Knowledge Check (知识检查)

**Q1**: What is the main difference between a Command and a Query?
<details>
<summary>Answer</summary>
Commands change system state (side effects), Queries only read data (no side effects).
</details>

**Q2**: Why is eventual consistency acceptable in CQRS?
<details>
<summary>Answer</summary>
Because read operations don't affect business logic correctness. Users can tolerate seeing slightly stale data for display purposes, while write operations maintain strong consistency.
</details>

**Q3**: How do you handle "order not found" after just creating it?
<details>
<summary>Answer</summary>
1. Return all needed data in the create response
2. Implement read-your-writes: fallback to write model if not in read model
3. Set appropriate user expectations with "Last updated" timestamps
</details>

**Q4**: When should you NOT use CQRS?
<details>
<summary>Answer</summary>
- Simple CRUD applications
- Balanced read/write ratio
- Strong consistency required everywhere
- Small team with no experience
- Tight deadlines
</details>

**Q5**: What's the benefit of combining CQRS with Event Sourcing?
<details>
<summary>Answer</summary>
Events naturally drive read model updates, complete audit trail, ability to rebuild read models, time-travel debugging, can create new read models for historical data.
</details>

### Coding Exercise (编码练习)

```
Exercise: Implement a simplified CQRS for Product Catalog

Requirements:
1. Commands:
   - CreateProductCommand (name, price, stock)
   - UpdatePriceCommand (productId, newPrice)
   
2. Queries:
   - GetProductByIdQuery
   - ListProductsQuery (with pagination)
   
3. Read Model:
   - ProductListView (id, name, price, inStock: bool)
   
4. Sync via events:
   - ProductCreatedEvent → updates ProductListView
   - PriceUpdatedEvent → updates ProductListView

Bonus:
- Add Redis caching for queries
- Implement validation behavior
- Add eventual consistency handling

Time: 90 minutes
```

### Architecture Discussion (架构讨论)

```
Scenario: You're architecting an e-commerce order system that needs to:
- Handle 10,000 orders/hour during normal times
- Handle 100,000 orders/hour during promotions
- Provide <100ms response time for order queries
- Support full-text search across orders
- Maintain complete audit trail
- Allow customers to see their orders immediately after placing

Questions:
1. Would you use CQRS? What's your scoring using the decision matrix?
2. What read models would you create?
3. How would you handle the "immediately see order" requirement?
4. Would you add Event Sourcing? Why or why not?
5. What sync strategy would you use?

Discuss with your team or write your analysis.
```

---

## 🔗 Related Documents

- **Domain Spec**: [04-ORDER-PROCESSING.md](../core-domains/04-ORDER-PROCESSING.md)
- **State Pattern**: [STATE-PATTERN.md](STATE-PATTERN.md) - Often used with CQRS
- **Observer Pattern**: [OBSERVER-PATTERN.md](OBSERVER-PATTERN.md) - For event-driven sync
- **Strategy Pattern**: [STRATEGY-PATTERN.md](STRATEGY-PATTERN.md) - For dispatch algorithms

---

*Document Version: 1.0*  
*Created: 2026-01-31*  
*Status: 📚 Study Document*
