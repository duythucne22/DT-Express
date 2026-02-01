# 🔌 Interceptor Pattern (拦截器模式) - Study Guide

> **Pattern Category**: Behavioral / Cross-Cutting Concern  
> **Primary Use in DT-Express**: Audit Tracking (05-AUDIT-TRACKING.md)  
> **Difficulty Level**: ⭐⭐⭐ Intermediate  
> **Prerequisites**: Basic OOP, Middleware concept, EF Core basics

---

## 📋 Table of Contents

1. [Pattern Overview](#pattern-overview)
2. [Real-World Analogy](#real-world-analogy)
3. [Pattern Structure](#pattern-structure)
4. [DT-Express Implementation](#dt-express-implementation)
5. [Code Examples](#code-examples)
6. [Interceptor vs Similar Patterns](#interceptor-vs-similar-patterns)
7. [Advanced Topics](#advanced-topics)
8. [Common Pitfalls](#common-pitfalls)
9. [Chinese Tech References](#chinese-tech-references)
10. [Self-Assessment](#self-assessment)

---

## 🎯 Pattern Overview

### What is the Interceptor Pattern?

The **Interceptor Pattern** allows you to **transparently insert behavior** before, after, or around an operation without modifying the operation itself. It's like placing a checkpoint that all traffic must pass through.

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    INTERCEPTOR PATTERN CONCEPT (拦截器概念)                      │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│   Without Interceptor:                                                          │
│   ┌──────────┐                                        ┌──────────────┐          │
│   │  Caller  │ ────────────────────────────────────>  │   Target     │          │
│   └──────────┘          Direct call                   └──────────────┘          │
│                                                                                 │
│   With Interceptor:                                                             │
│   ┌──────────┐       ┌──────────────────┐            ┌──────────────┐           │
│   │  Caller  │ ────> │   Interceptor    │ ────────>  │   Target     │           │
│   └──────────┘       │                  │            └──────────────┘           │
│                      │  • Pre-process   │                   │                   │
│                      │  • Log/Audit     │                   │                   │
│                      │  • Validate      │                   │                   │
│                      │  • Transform     │  <────────────────┘                   │
│                      │  • Post-process  │         Response                      │
│                      └──────────────────┘                                       │
│   Key Insight: Caller doesn't know interceptor exists (透明性)                   │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Why Use Interceptor Pattern?

| Benefit | Description | Example |
|---------|-------------|---------|
| **Transparency** | Caller unaware of interception | Business code doesn't change for auditing |
| **Separation of Concerns** | Cross-cutting logic isolated | Logging separate from business logic |
| **Centralization** | Single place for common behavior | One audit interceptor for all entities |
| **Testability** | Interceptor tested independently | Mock interceptor in unit tests |
| **Extensibility** | Add/remove interceptors easily | Add performance monitoring later |

### When to Use?

✅ **Use Interceptor When:**
- Need to add behavior to ALL operations of a type
- Cross-cutting concerns (logging, auditing, security)
- Want to keep business code clean
- Behavior needs to be consistent across operations

❌ **Don't Use When:**
- Behavior is specific to one operation (use decorator)
- Need complex conditional logic per operation
- Performance overhead is critical (each interceptor adds latency)

---

## 🏢 Real-World Analogy

### Airport Security Checkpoint (机场安检)

```
┌────────────────────────────────────────────────────────────────────────────────────┐
│                    AIRPORT SECURITY ANALOGY (机场安检类比)                          │
├────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                    │
│   You want to board a flight (your goal = SaveChanges to database)                 │
│                                                                                    │
│   ┌──────────┐     ┌────────────────────────────────────┐     ┌──────────────┐     │
│   │          │     │     Security Checkpoint            │     │              │     │
│   │ Passenger│     │     (Interceptor)                  │     │   Airplane   │     │
│   │ (Caller) │     │                                    │     │  (Database)  │     │
│   │          │     │  ┌─────────────────────────────┐   │     │              │     │
│   │          │────>│  │ 1. Check ticket (validate)  │   │────>│              │     │
│   │          │     │  │ 2. Scan luggage (inspect)   │   │     │              │     │
│   │          │     │  │ 3. Record entry (audit)     │   │     │              │     │
│   │          │     │  │ 4. Allow/Deny (authorize)   │   │     │              │     │
│   │          │     │  └─────────────────────────────┘   │     │              │     │
│   └──────────┘     └────────────────────────────────────┘     └──────────────┘     │
│                                                                                    │
│   Key Points:                                                                      │
│   • Every passenger MUST go through security (consistent)                          │
│   • Passenger's goal (boarding) unchanged (transparent)                            │
│   • Security doesn't care where you're going (generic)                             │
│   • Rules can change without passengers knowing (flexible)                         │
│   • Some passengers may be stopped (can modify/reject)                             │
│                                                                                    │
│   In DT-Express:                                                                   │
│   • Passenger = SaveChanges() call                                                 │
│   • Security = AuditInterceptor                                                    │
│   • Luggage scan = Capture entity changes                                          │
│   • Record entry = Write to AuditLog                                               │
│   • Airplane = Database                                                            │
└────────────────────────────────────────────────────────────────────────────────────┘
```

### Hotel Concierge (酒店礼宾)

Another analogy: A hotel concierge intercepts all guest requests:

```
Guest Request               Concierge (Interceptor)              Service Provider
─────────────────          ─────────────────────────            ──────────────────
"I need a taxi"    ────>   • Log the request                    Taxi Company
                           • Check guest status (VIP?)   ────> 
                           • Add hotel commission
                           • Track for billing

The guest just asks for a taxi, unaware of:
- Logging happening
- VIP status being checked
- Commission being added
- Billing being tracked
```

---

## 🏗️ Pattern Structure

### UML Class Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    INTERCEPTOR PATTERN STRUCTURE                                    │
├─────────────────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────────────────────┐     │
│  │  <<interface>> IInterceptor                                                │     │
│  │  ──────────────────────────────────────────────────────────────────────    │     │
│  │  + InterceptBefore(context: InterceptionContext) : void                    │     │
│  │  + InterceptAfter(context: InterceptionContext, result: T) : T             │     │
│  │  + InterceptException(context: InterceptionContext, ex: Exception) : void  │     │
│  └────────────────────────────────────────────────────────────────────────────┘     │
│                                      ^                                              │
│                                      │ implements                                   │
│                                      │                                              │
│         ┌────────────────────────────┼──────────────────────────┐                   │
│         │                            │                          │                   │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐               │
│  │LoggingInterceptor│    │ AuditInterceptor │    │CachingInterceptor│               │
│  ├──────────────────┤    ├──────────────────┤    ├──────────────────┤               │
│  │ - _logger        │    │ - _auditService  │    │ - _cache         │               │
│  ├──────────────────┤    ├──────────────────┤    ├──────────────────┤               │
│  │ + InterceptBefore│    │ + InterceptBefore│    │ + InterceptBefore│               │
│  │ + InterceptAfter │    │ + InterceptAfter │    │ + InterceptAfter │               │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘               │
│  ┌────────────────────────────────────────────────────────────────────────────┐     │
│  │  InterceptorChain (Dispatcher)                                             │     │
│  │  ──────────────────────────────────────────────────────────────────────    │     │
│  │  - _interceptors: List<IInterceptor>                                       │     │
│  │  - _target: object                                                         │     │
│  │  ──────────────────────────────────────────────────────────────────────    │     │
│  │  + Execute(method, args) : result                                          │     │
│  │      1. ForEach interceptor: InterceptBefore()                             │     │
│  │      2. Call target method                                                 │     │
│  │      3. ForEach interceptor (reverse): InterceptAfter()                    │     │
│  └────────────────────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Execution Flow

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    INTERCEPTOR EXECUTION FLOW (执行流程)                             │
├─────────────────────────────────────────────────────────────────────────────────────┤
│   Client calls SaveChanges()                                                         │
│          │                                                                           │
│          ▼                                                                           │
│   ┌─────────────────────────────────────────────────────────────────┐               │
│   │  INTERCEPTOR CHAIN                                               │               │
│   │                                                                  │               │
│   │  ┌──────────────────────────────────────────────────────────┐   │               │
│   │  │  Interceptor 1: SavingChanges() - PRE                    │   │               │
│   │  │  • Capture changed entities                              │   │               │
│   │  │  • Create audit records                                  │   │               │
│   │  └──────────────────────────────────────────────────────────┘   │               │
│   │                         │                                        │               │
│   │                         ▼                                        │               │
│   │  ┌──────────────────────────────────────────────────────────┐   │               │
│   │  │  Interceptor 2: ValidationInterceptor - PRE              │   │               │
│   │  │  • Validate entity state                                 │   │               │
│   │  │  • Check business rules                                  │   │               │
│   │  └──────────────────────────────────────────────────────────┘   │               │
│   │                         │                                        │               │
│   │                         ▼                                        │               │
│   │  ┌──────────────────────────────────────────────────────────┐   │               │
│   │  │              ACTUAL DATABASE OPERATION                    │   │               │
│   │  │              SaveChanges() to SQL Server                  │   │               │
│   │  └──────────────────────────────────────────────────────────┘   │               │
│   │                         │                                        │               │
│   │                         ▼                                        │               │
│   │  ┌──────────────────────────────────────────────────────────┐   │               │
│   │  │  Interceptor 2: ValidationInterceptor - POST             │   │               │
│   │  │  • (optional post-processing)                            │   │               │
│   │  └──────────────────────────────────────────────────────────┘   │               │
│   │                         │                                        │               │
│   │                         ▼                                        │               │
│   │  ┌──────────────────────────────────────────────────────────┐   │               │
│   │  │  Interceptor 1: SavedChanges() - POST                    │   │               │
│   │  │  • Persist audit records                                 │   │               │
│   │  │  • Publish events                                        │   │               │
│   │  └──────────────────────────────────────────────────────────┘   │               │
│   │                                                                  │               │
│   └─────────────────────────────────────────────────────────────────┘               │
│          │                                                                           │
│          ▼                                                                           │
│   Return to Client                                                                   │
│                                                                                      │
│   Note: Interceptors execute in order (pre) and reverse order (post)                │
│         Like Russian nesting dolls (俄罗斯套娃)                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🚚 DT-Express Implementation

### Audit Interceptor for Entity Changes

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    DT-EXPRESS AUDIT INTERCEPTOR                                      │
│                    物流系统审计拦截器                                                │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   Business Code (doesn't know about auditing):                                       │
│   ┌──────────────────────────────────────────────────────────────────────────┐      │
│   │  public async Task<Order> CreateOrderAsync(CreateOrderCommand cmd)       │      │
│   │  {                                                                        │      │
│   │      var order = new Order(cmd.CustomerId, cmd.Items);                   │      │
│   │      _context.Orders.Add(order);                                          │      │
│   │      await _context.SaveChangesAsync();  // Interceptor triggered here   │      │
│   │      return order;                                                        │      │
│   │  }                                                                        │      │
│   └──────────────────────────────────────────────────────────────────────────┘      │
│                                          │                                           │
│                                          │ triggers                                  │
│                                          ▼                                           │
│   ┌──────────────────────────────────────────────────────────────────────────┐      │
│   │  AuditInterceptor : SaveChangesInterceptor                               │      │
│   │  ────────────────────────────────────────────────────────────────────    │      │
│   │                                                                          │      │
│   │  SavingChanges() - BEFORE database write:                                │      │
│   │  ┌────────────────────────────────────────────────────────────────┐     │      │
│   │  │  foreach (var entry in context.ChangeTracker.Entries())        │     │      │
│   │  │  {                                                              │     │      │
│   │  │      if (entry.Entity is IAuditableEntity)                     │     │      │
│   │  │      {                                                          │     │      │
│   │  │          var audit = new AuditEntry                            │     │      │
│   │  │          {                                                      │     │      │
│   │  │              EntityType = entry.Entity.GetType().Name,         │     │      │
│   │  │              EntityId = GetPrimaryKey(entry),                  │     │      │
│   │  │              Action = MapState(entry.State),                   │     │      │
│   │  │              OldValues = GetOriginalValues(entry),             │     │      │
│   │  │              NewValues = GetCurrentValues(entry),              │     │      │
│   │  │              UserId = _userContext.UserId,                     │     │      │
│   │  │              Timestamp = DateTime.UtcNow                       │     │      │
│   │  │          };                                                     │     │      │
│   │  │          _pendingAudits.Add(audit);                            │     │      │
│   │  │      }                                                          │     │      │
│   │  │  }                                                              │     │      │
│   │  └────────────────────────────────────────────────────────────────┘     │      │
│   │                                                                          │      │
│   │  SavedChanges() - AFTER database write:                                  │      │
│   │  ┌────────────────────────────────────────────────────────────────┐     │      │
│   │  │  // Now we have generated IDs for new entities                 │     │      │
│   │  │  foreach (var audit in _pendingAudits)                         │     │      │
│   │  │  {                                                              │     │      │
│   │  │      await _auditStore.SaveAsync(audit);                       │     │      │
│   │  │  }                                                              │     │      │
│   │  │  _pendingAudits.Clear();                                       │     │      │
│   │  └────────────────────────────────────────────────────────────────┘     │      │
│   │                                                                          │      │
│   └──────────────────────────────────────────────────────────────────────────┘      │
│                                                                                      │
│   Result: Every entity change automatically audited!                                │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Entities Tracked

| Entity | Audited Fields | Special Handling |
|--------|----------------|------------------|
| **Order** | All fields | PII masking on customer info |
| **Shipment** | Status, carrier, tracking | Log carrier API calls |
| **TrackingEvent** | All fields | High volume - batch writes |
| **Customer** | All fields | Heavy PII masking |
| **Route** | Selected route, cost | Decision audit |

---

## 💻 Code Examples

### Basic EF Core SaveChangesInterceptor

```csharp
/// <summary>
/// EF Core审计拦截器 - 自动捕获所有实体变更
/// Audit Interceptor - Automatically captures all entity changes
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IUserContext _userContext;
    private readonly IAuditStore _auditStore;
    private readonly ILogger<AuditInterceptor> _logger;
    
    // Thread-local storage for pending audits (线程本地存储)
    private readonly AsyncLocal<List<AuditEntry>> _pendingAudits = new();

    public AuditInterceptor(
        IUserContext userContext,
        IAuditStore auditStore,
        ILogger<AuditInterceptor> logger)
    {
        _userContext = userContext;
        _auditStore = auditStore;
        _logger = logger;
    }

    /// <summary>
    /// 在SaveChanges之前调用 - 捕获变更
    /// Called BEFORE SaveChanges - Capture changes
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureChanges(eventData.Context!);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// 异步版本
    /// Async version
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureChanges(eventData.Context!);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// 在SaveChanges成功后调用 - 持久化审计记录
    /// Called AFTER SaveChanges succeeds - Persist audit records
    /// </summary>
    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        PersistAuditRecords();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PersistAuditRecordsAsync();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// 在SaveChanges失败时调用 - 记录失败
    /// Called when SaveChanges fails - Log failure
    /// </summary>
    public override void SaveChangesFailed(
        DbContextErrorEventData eventData)
    {
        _logger.LogError(eventData.Exception, 
            "SaveChanges failed. Pending audits discarded: {Count}", 
            _pendingAudits.Value?.Count ?? 0);
        
        _pendingAudits.Value?.Clear();
        base.SaveChangesFailed(eventData);
    }

    private void CaptureChanges(DbContext context)
    {
        _pendingAudits.Value ??= new List<AuditEntry>();
        
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity)
            .Where(e => e.State is EntityState.Added 
                            or EntityState.Modified 
                            or EntityState.Deleted);

        foreach (var entry in entries)
        {
            var audit = CreateAuditEntry(entry);
            _pendingAudits.Value.Add(audit);
        }
    }

    private AuditEntry CreateAuditEntry(EntityEntry entry)
    {
        var audit = new AuditEntry
        {
            EntityType = entry.Entity.GetType().Name,
            Action = MapState(entry.State),
            Timestamp = DateTime.UtcNow,
            UserId = _userContext.UserId,
            UserName = _userContext.UserName,
            IpAddress = _userContext.IpAddress,
            CorrelationId = _userContext.CorrelationId
        };

        // Capture values based on state
        switch (entry.State)
        {
            case EntityState.Added:
                audit.NewValues = SerializeValues(entry.CurrentValues);
                // EntityId will be set after SaveChanges for generated keys
                break;
                
            case EntityState.Modified:
                audit.OldValues = SerializeValues(entry.OriginalValues);
                audit.NewValues = SerializeValues(entry.CurrentValues);
                audit.EntityId = GetPrimaryKey(entry);
                // Only capture changed properties
                audit.ChangedProperties = GetChangedProperties(entry);
                break;
                
            case EntityState.Deleted:
                audit.OldValues = SerializeValues(entry.OriginalValues);
                audit.EntityId = GetPrimaryKey(entry);
                break;
        }

        return audit;
    }

    private AuditAction MapState(EntityState state) => state switch
    {
        EntityState.Added => AuditAction.Create,
        EntityState.Modified => AuditAction.Update,
        EntityState.Deleted => AuditAction.Delete,
        _ => AuditAction.Unknown
    };

    private string GetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        var values = key?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .Where(v => v != null);
        return string.Join(",", values ?? Array.Empty<string>());
    }

    private string SerializeValues(PropertyValues values)
    {
        var dict = values.Properties
            .ToDictionary(
                p => p.Name,
                p => values[p]
            );
        return JsonSerializer.Serialize(dict);
    }

    private List<string> GetChangedProperties(EntityEntry entry)
    {
        return entry.Properties
            .Where(p => p.IsModified)
            .Select(p => p.Metadata.Name)
            .ToList();
    }

    private async Task PersistAuditRecordsAsync()
    {
        if (_pendingAudits.Value is null || _pendingAudits.Value.Count == 0)
            return;

        try
        {
            await _auditStore.SaveBatchAsync(_pendingAudits.Value);
        }
        finally
        {
            _pendingAudits.Value.Clear();
        }
    }

    private void PersistAuditRecords()
    {
        PersistAuditRecordsAsync().GetAwaiter().GetResult();
    }
}
```

### Registration in DI

```csharp
// Program.cs or Startup.cs
public static class AuditExtensions
{
    public static IServiceCollection AddAuditInterceptor(
        this IServiceCollection services)
    {
        // Register dependencies
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<IAuditStore, SqlAuditStore>();
        
        // Register interceptor
        services.AddScoped<AuditInterceptor>();
        
        // Configure DbContext with interceptor
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });
        
        return services;
    }
}
```

### High-Performance Async Version with Channel

```csharp
/// <summary>
/// 高性能异步审计拦截器 - 使用Channel实现非阻塞写入
/// High-performance async audit interceptor using Channel for non-blocking writes
/// </summary>
public class AsyncAuditInterceptor : SaveChangesInterceptor, IDisposable
{
    private readonly Channel<AuditEntry> _channel;
    private readonly Task _writerTask;
    private readonly CancellationTokenSource _cts;
    private readonly ObjectPool<AuditEntry> _entryPool;

    public AsyncAuditInterceptor(
        IUserContext userContext,
        IAuditStore auditStore,
        ObjectPool<AuditEntry> entryPool)
    {
        _entryPool = entryPool;
        
        // Bounded channel with backpressure (有界通道,带背压)
        _channel = Channel.CreateBounded<AuditEntry>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,  // Optimized for single consumer
                SingleWriter = false
            });

        _cts = new CancellationTokenSource();
        
        // Background writer task (后台写入任务)
        _writerTask = Task.Run(async () =>
        {
            await ProcessAuditEntriesAsync(auditStore, _cts.Token);
        });
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // Capture and enqueue - NON-BLOCKING (捕获并入队 - 非阻塞)
        foreach (var entry in GetAuditableEntries(eventData.Context!))
        {
            var audit = _entryPool.Get();
            PopulateAuditEntry(audit, entry);
            
            // TryWrite is non-blocking - if channel full, drops entry
            // In production, consider Wait mode or overflow handling
            if (!_channel.Writer.TryWrite(audit))
            {
                _entryPool.Return(audit);
                // Log overflow warning
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task ProcessAuditEntriesAsync(
        IAuditStore store, 
        CancellationToken ct)
    {
        var batch = new List<AuditEntry>(100);
        var lastFlush = DateTime.UtcNow;

        await foreach (var entry in _channel.Reader.ReadAllAsync(ct))
        {
            batch.Add(entry);

            // Flush when batch full OR timeout (批量满或超时刷新)
            var shouldFlush = batch.Count >= 100 
                || (DateTime.UtcNow - lastFlush).TotalSeconds >= 5;

            if (shouldFlush)
            {
                await store.SaveBatchAsync(batch);
                
                // Return entries to pool (归还对象池)
                foreach (var e in batch)
                    _entryPool.Return(e);
                    
                batch.Clear();
                lastFlush = DateTime.UtcNow;
            }
        }

        // Flush remaining on shutdown
        if (batch.Count > 0)
        {
            await store.SaveBatchAsync(batch);
        }
    }

    public void Dispose()
    {
        _channel.Writer.Complete();
        _cts.Cancel();
        _writerTask.Wait(TimeSpan.FromSeconds(10));
        _cts.Dispose();
    }
}
```

---

## ⚖️ Interceptor vs Similar Patterns

### Comparison Table

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    INTERCEPTOR vs RELATED PATTERNS                                   │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  Pattern        │ Intent                        │ Transparency │ Granularity        │
│  ───────────────┼───────────────────────────────┼──────────────┼───────────────────│
│  Interceptor    │ Add behavior to ALL ops       │ High         │ Coarse (all calls)│
│  Decorator      │ Add behavior to specific obj  │ Medium       │ Fine (per object) │
│  Proxy          │ Control access to object      │ High         │ Per object        │
│  Middleware     │ Request pipeline processing   │ High         │ Per request       │
│  Observer       │ React to events               │ Low          │ Per event type    │
│                                                                                      │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  INTERCEPTOR:                                                                        │
│  ┌─────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐               │
│  │ Caller  │───▶│ Interceptor │───▶│ Interceptor │───▶│   Target    │               │
│  └─────────┘    │    Chain    │    │    Chain    │    └─────────────┘               │
│                 └─────────────┘    └─────────────┘                                  │
│  • Wraps the invocation pipeline                                                    │
│  • Can modify/reject request                                                        │
│  • Caller unaware of interceptors                                                   │
│                                                                                      │
│  DECORATOR:                                                                          │
│  ┌─────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐               │
│  │ Caller  │───▶│ Decorator A │───▶│ Decorator B │───▶│  Component  │               │
│  └─────────┘    └─────────────┘    └─────────────┘    └─────────────┘               │
│  • Same interface as target                                                         │
│  • Adds responsibilities to specific instance                                       │
│  • Caller may know about decoration                                                 │
│                                                                                      │
│  PROXY:                                                                              │
│  ┌─────────┐    ┌─────────────┐                      ┌─────────────┐               │
│  │ Caller  │───▶│    Proxy    │─────────────────────▶│ Real Object │               │
│  └─────────┘    └─────────────┘                      └─────────────┘               │
│  • Controls access (lazy load, security, caching)                                   │
│  • Same interface as real object                                                    │
│  • May or may not delegate to real object                                           │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### When to Use Each

| Scenario | Best Pattern | Reason |
|----------|--------------|--------|
| Audit ALL database changes | **Interceptor** | Cross-cutting, transparent |
| Add logging to ONE service | **Decorator** | Targeted enhancement |
| Lazy load expensive resource | **Proxy** | Access control |
| HTTP request processing | **Middleware** | Pipeline model |
| React to domain events | **Observer** | Loose coupling |

---

## 🔬 Advanced Topics

### Multiple Interceptors Ordering

```csharp
// Order matters! Executed first to last for Pre, last to first for Post
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.AddInterceptors(
        sp.GetRequiredService<TimingInterceptor>(),      // 1st pre, 3rd post
        sp.GetRequiredService<ValidationInterceptor>(),  // 2nd pre, 2nd post
        sp.GetRequiredService<AuditInterceptor>()        // 3rd pre, 1st post
    );
});
```

### Interceptor for Query Operations

```csharp
/// <summary>
/// 查询拦截器 - 记录数据访问
/// Query Interceptor - Log data access
/// </summary>
public class QueryAuditInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        // Log query for compliance (记录查询用于合规)
        LogQuery(command.CommandText, eventData);
        return base.ReaderExecuting(command, eventData, result);
    }
    
    private void LogQuery(string sql, CommandEventData eventData)
    {
        // Check if accessing sensitive tables
        if (sql.Contains("Customer") || sql.Contains("Payment"))
        {
            // Log access to sensitive data
        }
    }
}
```

### Transaction Interceptor

```csharp
/// <summary>
/// 事务拦截器 - 追踪事务边界
/// Transaction Interceptor - Track transaction boundaries
/// </summary>
public class TransactionAuditInterceptor : DbTransactionInterceptor
{
    public override InterceptionResult TransactionStarting(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult result)
    {
        _logger.LogInformation("Transaction starting: {TransactionId}", 
            eventData.TransactionId);
        return base.TransactionStarting(connection, eventData, result);
    }

    public override void TransactionCommitted(
        DbTransaction transaction,
        TransactionEndEventData eventData)
    {
        _logger.LogInformation("Transaction committed: {TransactionId}", 
            eventData.TransactionId);
        base.TransactionCommitted(transaction, eventData);
    }

    public override void TransactionRolledBack(
        DbTransaction transaction,
        TransactionEndEventData eventData)
    {
        _logger.LogWarning("Transaction rolled back: {TransactionId}", 
            eventData.TransactionId);
        base.TransactionRolledBack(transaction, eventData);
    }
}
```

---

## ⚠️ Common Pitfalls

### 1. Circular Reference in Audit Storage

```csharp
// ❌ BAD: Audit interceptor triggers itself
public class AuditInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(...)
    {
        var audit = new AuditLog { ... };
        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync(); // ⚠️ Triggers interceptor again!
    }
}

// ✅ GOOD: Use separate context or direct SQL
public class AuditInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(...)
    {
        // Option 1: Separate DbContext without interceptor
        using var auditContext = new AuditDbContext();
        auditContext.AuditLogs.Add(audit);
        await auditContext.SaveChangesAsync();
        
        // Option 2: Direct SQL/Dapper
        await _auditStore.SaveDirectAsync(audit);
    }
}
```

### 2. Performance Impact

```csharp
// ❌ BAD: Synchronous audit in hot path
public override int SavedChanges(...)
{
    foreach (var audit in _pending)
    {
        _httpClient.PostAsync("/audit", audit).Wait(); // Blocking!
    }
}

// ✅ GOOD: Async with Channel
public override int SavedChanges(...)
{
    foreach (var audit in _pending)
    {
        _channel.Writer.TryWrite(audit); // Non-blocking
    }
}
```

### 3. Missing Exception Handling

```csharp
// ❌ BAD: Exception in interceptor breaks transaction
public override ValueTask<int> SavingChangesAsync(...)
{
    var json = JsonSerializer.Serialize(entity); // May throw!
}

// ✅ GOOD: Catch and log, don't break business operation
public override ValueTask<int> SavingChangesAsync(...)
{
    try
    {
        var json = JsonSerializer.Serialize(entity);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to serialize for audit");
        // Don't rethrow - audit failure shouldn't break business
    }
}
```

---

## 🇨🇳 Chinese Tech References

### Industry Examples

| Company | Implementation | Reference |
|---------|----------------|-----------|
| **阿里巴巴** | 操作日志组件 | CSDN: `阿里操作日志实现` |
| **京东物流** | 全链路审计 | Search: `京东物流审计系统` |
| **顺丰** | 快递轨迹追踪 | Search: `顺丰审计日志` |

### Search Keywords

| Topic | Search Terms |
|-------|--------------|
| EF Core拦截器 | `EF Core SaveChangesInterceptor 审计` |
| 高性能日志 | `C# Channel 异步日志 高性能` |
| 审计系统设计 | `操作日志系统设计 最佳实践` |

---

## ✅ Self-Assessment

### Knowledge Check

1. **What is the key benefit of Interceptor over Decorator for auditing?**
   - Answer: Transparency - business code doesn't know about auditing

2. **When is `SavingChanges` vs `SavedChanges` called?**
   - `SavingChanges`: Before database write (capture changes)
   - `SavedChanges`: After successful write (persist audit)

3. **How do you avoid circular references in audit interceptor?**
   - Use separate DbContext or direct SQL for audit storage

4. **What's the execution order for multiple interceptors?**
   - Pre: First to last, Post: Last to first (like nested dolls)

### Coding Challenge

Implement an interceptor that:
1. Tracks which user accessed which Customer records
2. Logs access time and accessed fields
3. Stores in a separate AccessLog table
4. Doesn't impact main query performance

### Discussion Questions

1. How would you handle audit for soft-deleted entities?
2. What's the tradeoff between sync and async audit writes?
3. How would you implement audit for bulk operations?

---

## 🔗 Related Patterns

- **Decorator Pattern**: For targeted behavior enhancement → [DECORATOR-PATTERN.md](DECORATOR-PATTERN.md)
- **Observer Pattern**: For event-based reactions → [OBSERVER-PATTERN.md](OBSERVER-PATTERN.md)
- **Strategy Pattern**: For storage backend selection → [STRATEGY-PATTERN.md](STRATEGY-PATTERN.md)

---