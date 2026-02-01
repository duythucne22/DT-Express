# 🎀 Decorator Pattern (装饰器模式) - Study Guide

> **Pattern Category**: Structural  
> **Primary Use in DT-Express**: PII Masking in Audit System  
> **Difficulty Level**: ⭐⭐ Beginner-Intermediate  
> **Prerequisites**: Basic OOP, Interface concept, Composition over Inheritance

---

## 📋 Table of Contents

1. [Pattern Overview](#pattern-overview)
2. [Real-World Analogy](#real-world-analogy)
3. [Pattern Structure](#pattern-structure)
4. [DT-Express Implementation](#dt-express-implementation)
5. [Code Examples](#code-examples)
6. [Decorator vs Similar Patterns](#decorator-vs-similar-patterns)
7. [Advanced Topics](#advanced-topics)
8. [Common Pitfalls](#common-pitfalls)
9. [Chinese Tech References](#chinese-tech-references)
10. [Self-Assessment](#self-assessment)

---

## 🎯 Pattern Overview

### What is the Decorator Pattern?

The **Decorator Pattern** allows you to **dynamically add behavior** to objects by wrapping them in decorator objects. Each decorator adds one specific responsibility, and decorators can be stacked.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    DECORATOR PATTERN CONCEPT (装饰器概念)                            │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   Without Decorator (Inheritance Explosion):                                         │
│   ┌─────────────────────────────────────────────────────────────────────────┐       │
│   │                         AuditProcessor                                   │       │
│   │                              △                                           │       │
│   │    ┌─────────────────────────┼─────────────────────────┐                │       │
│   │    │                         │                         │                │       │
│   │ AuditProcessor       AuditProcessor            AuditProcessor           │       │
│   │ WithMasking          WithHashing               WithMaskingAndHashing    │       │
│   │                                                ❌ Combinatorial explosion│       │
│   └─────────────────────────────────────────────────────────────────────────┘       │
│                                                                                      │
│   With Decorator (Composable):                                                       │
│   ┌─────────────────────────────────────────────────────────────────────────┐       │
│   │                                                                          │       │
│   │   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐              │       │
│   │   │   Masking    │───▶│   Hashing    │───▶│    Core      │              │       │
│   │   │  Decorator   │    │  Decorator   │    │  Processor   │              │       │
│   │   └──────────────┘    └──────────────┘    └──────────────┘              │       │
│   │                                                                          │       │
│   │   Each decorator adds ONE responsibility                                 │       │
│   │   Decorators can be combined in any order                               │       │
│   │   New behaviors added without modifying existing code                   │       │
│   │   ✅ Single Responsibility Principle                                    │       │
│   │                                                                          │       │
│   └─────────────────────────────────────────────────────────────────────────┘       │
│                                                                                      │
│   Key Insight: "Wrap and delegate" (包装并委托)                                      │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Why Use Decorator Pattern?

| Benefit | Description | Example |
|---------|-------------|---------|
| **Single Responsibility** | Each decorator does one thing | PIIMasking only masks, Hashing only hashes |
| **Open/Closed** | Add behaviors without modifying | Add encryption without changing core |
| **Composable** | Mix and match decorators | Mask → Hash → Compress |
| **Runtime Flexibility** | Change behavior at runtime | Add/remove decorators based on config |
| **Testable** | Test each decorator in isolation | Unit test masking separately |

### When to Use?

✅ **Use Decorator When:**
- Need to add responsibilities dynamically
- Want to avoid inheritance explosion
- Responsibilities can be withdrawn
- Extension by subclassing is impractical

❌ **Don't Use When:**
- Only one way to combine behaviors (use inheritance)
- All objects need all behaviors (bake it in)
- Order of decoration doesn't matter AND performance critical

---

## 🎂 Real-World Analogy

### Coffee Shop (咖啡店)

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    COFFEE SHOP ANALOGY (咖啡店类比)                                  │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   You order coffee and add extras:                                                   │
│                                                                                      │
│   Base Coffee (基础咖啡)                                                             │
│   ┌─────────────────────┐                                                           │
│   │      Espresso       │   Cost: ¥20                                               │
│   │      ☕             │                                                            │
│   └─────────────────────┘                                                           │
│             │                                                                        │
│             │ + Milk (牛奶装饰器)                                                     │
│             ▼                                                                        │
│   ┌─────────────────────┐                                                           │
│   │   Milk Decorator    │   Cost: ¥20 + ¥5 = ¥25                                    │
│   │   ┌─────────────┐   │                                                           │
│   │   │  Espresso   │   │                                                           │
│   │   └─────────────┘   │                                                           │
│   └─────────────────────┘                                                           │
│             │                                                                        │
│             │ + Caramel (焦糖装饰器)                                                  │
│             ▼                                                                        │
│   ┌─────────────────────┐                                                           │
│   │  Caramel Decorator  │   Cost: ¥25 + ¥8 = ¥33                                    │
│   │   ┌─────────────┐   │                                                           │
│   │   │Milk+Espresso│   │                                                           │
│   │   └─────────────┘   │                                                           │
│   └─────────────────────┘                                                           │
│             │                                                                        │
│             │ + Whipped Cream (奶油装饰器)                                           │
│             ▼                                                                        │
│   ┌─────────────────────┐                                                           │
│   │   Cream Decorator   │   Cost: ¥33 + ¥6 = ¥39                                    │
│   │   ┌─────────────┐   │                                                           │
│   │   │Caramel+Milk │   │   Description: "Espresso with Milk,                       │
│   │   │+Espresso    │   │                 Caramel, and Whipped Cream"               │
│   │   └─────────────┘   │                                                           │
│   └─────────────────────┘                                                           │
│                                                                                      │
│   Key Points:                                                                        │
│   • Same interface: GetCost(), GetDescription()                                     │
│   • Each decorator wraps and delegates                                              │
│   • Order matters: Milk before Caramel vs Caramel before Milk                       │
│   • Can add same decorator twice: Double shot = Espresso + Espresso decorator       │
│                                                                                      │
│   In DT-Express Audit:                                                               │
│   • Espresso = CoreAuditProcessor                                                   │
│   • Milk = PIIMaskingDecorator                                                      │
│   • Caramel = HashingDecorator                                                      │
│   • Whipped Cream = CompressionDecorator                                            │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Gift Wrapping (礼物包装)

Another analogy: Wrapping a gift with multiple layers:

```
Gift Box (礼物盒)
    ↓ wrap with
Tissue Paper (薄纸)
    ↓ wrap with  
Decorative Box (装饰盒)
    ↓ wrap with
Ribbon (丝带)
    ↓ wrap with
Gift Bag (礼品袋)

Each layer:
• Adds its own "presentation"
• Delegates to inner layer for the actual gift
• Can be removed independently
• Same interface: Unwrap() returns the gift
```

---

## 🏗️ Pattern Structure

### UML Class Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    DECORATOR PATTERN STRUCTURE                                       │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌────────────────────────────────────────────────────────────────────────────┐     │
│  │  <<interface>> IComponent                                                   │     │
│  │  ──────────────────────────────────────────────────────────────────────    │     │
│  │  + Operation() : Result                                                    │     │
│  └────────────────────────────────────────────────────────────────────────────┘     │
│                                        △                                            │
│                                        │ implements                                 │
│                   ┌────────────────────┴────────────────────┐                       │
│                   │                                         │                       │
│  ┌────────────────────────────┐         ┌────────────────────────────────────┐     │
│  │  ConcreteComponent         │         │  <<abstract>> Decorator            │     │
│  │  (Core implementation)     │         │  ──────────────────────────────    │     │
│  ├────────────────────────────┤         │  - _wrapped: IComponent             │     │
│  │  + Operation() : Result    │         ├────────────────────────────────────┤     │
│  │    // actual work          │         │  + Decorator(wrapped: IComponent)  │     │
│  └────────────────────────────┘         │  + Operation() : Result            │     │
│                                         │    // _wrapped.Operation()          │     │
│                                         └────────────────────────────────────┘     │
│                                                         △                           │
│                                                         │                           │
│                              ┌───────────────────────────┼───────────────────────┐  │
│                              │                           │                       │  │
│               ┌──────────────────────┐    ┌──────────────────────┐              │  │
│               │  ConcreteDecoratorA  │    │  ConcreteDecoratorB  │              │  │
│               │  (e.g., Masking)     │    │  (e.g., Hashing)     │              │  │
│               ├──────────────────────┤    ├──────────────────────┤              │  │
│               │  - _additionalState  │    │  - _additionalState  │              │  │
│               ├──────────────────────┤    ├──────────────────────┤              │  │
│               │  + Operation()       │    │  + Operation()       │              │  │
│               │    // add behavior A │    │    // add behavior B │              │  │
│               │    // call base      │    │    // call base      │              │  │
│               └──────────────────────┘    └──────────────────────┘              │  │
│                                                                                      │
│  Key Points:                                                                         │
│  • IComponent: Common interface for both core and decorators                        │
│  • ConcreteComponent: The "real" implementation                                     │
│  • Decorator: Base class holding reference to wrapped component                     │
│  • ConcreteDecorators: Add specific behaviors                                       │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Decoration Flow

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    DECORATOR EXECUTION FLOW (执行流程)                               │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   Client calls Process(data) on outermost decorator:                                │
│                                                                                      │
│   ┌─────────────────────────────────────────────────────────────────────────────┐   │
│   │                                                                             │   │
│   │   ┌───────────────────────────────────────────────────────────────────┐    │   │
│   │   │  HashingDecorator.Process(data)                                   │    │   │
│   │   │  ─────────────────────────────────────────────────────────────    │    │   │
│   │   │  1. Pre-process: Create hash of original                          │    │   │
│   │   │  ─────────────────────────────────────────────────────────────    │    │   │
│   │   │                           │                                       │    │   │
│   │   │                           ▼                                       │    │   │
│   │   │   ┌───────────────────────────────────────────────────────┐      │    │   │
│   │   │   │  PIIMaskingDecorator.Process(data)                    │      │    │   │
│   │   │   │  ─────────────────────────────────────────────────    │      │    │   │
│   │   │   │  1. Pre-process: Detect PII fields                    │      │    │   │
│   │   │   │  2. Mask: phone → 138****5678                         │      │    │   │
│   │   │   │  ─────────────────────────────────────────────────    │      │    │   │
│   │   │   │                           │                           │      │    │   │
│   │   │   │                           ▼                           │      │    │   │
│   │   │   │   ┌───────────────────────────────────────────┐      │      │    │   │
│   │   │   │   │  CoreAuditProcessor.Process(data)         │      │      │    │   │
│   │   │   │   │  ─────────────────────────────────────    │      │      │    │   │
│   │   │   │   │  1. Serialize to JSON                     │      │      │    │   │
│   │   │   │   │  2. Add metadata                          │      │      │    │   │
│   │   │   │   │  3. Return AuditEntry                     │      │      │    │   │
│   │   │   │   └────────────────────┬──────────────────────┘      │      │    │   │
│   │   │   │                        │ return                       │      │    │   │
│   │   │   │   ◀────────────────────┘                              │      │    │   │
│   │   │   │  3. Post-process: (optional)                          │      │    │   │
│   │   │   └────────────────────┬──────────────────────────────────┘      │    │   │
│   │   │                        │ return                                   │    │   │
│   │   │   ◀────────────────────┘                                          │    │   │
│   │   │  2. Post-process: Attach hash to entry                            │    │   │
│   │   └────────────────────────────────────────────────────────────────────┘    │   │
│   │                                                                             │   │
│   └─────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
│   Result: AuditEntry with masked PII and integrity hash                             │
│                                                                                      │
│   Note: Execution is like onion layers - outside in, then inside out               │
│         (像洋葱层层包裹,由外向内再由内向外)                                          │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🚚 DT-Express Implementation

### PII Masking Decorator for Audit

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    DT-EXPRESS AUDIT DECORATOR CHAIN                                  │
│                    物流系统审计装饰器链                                              │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   Configuration (DI Setup):                                                          │
│   ┌────────────────────────────────────────────────────────────────────────────┐    │
│   │  services.AddScoped<IAuditProcessor>(sp =>                                 │    │
│   │      new HashingDecorator(                     // Outer: adds hash         │    │
│   │          new PIIMaskingDecorator(              // Middle: masks PII        │    │
│   │              new CompressionDecorator(         // Inner: compresses        │    │
│   │                  new CoreAuditProcessor(       // Core: serializes         │    │
│   │                      sp.GetService<IAuditStore>())))));                    │    │
│   └────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│   Processing Flow:                                                                   │
│   ┌────────────────────────────────────────────────────────────────────────────┐    │
│   │                                                                            │    │
│   │   Input: EntityChange                                                      │    │
│   │   {                                                                        │    │
│   │     "EntityType": "Customer",                                              │    │
│   │     "NewValues": {                                                         │    │
│   │       "Name": "张三",                                                       │    │
│   │       "Phone": "13812345678",                                              │    │
│   │       "Email": "zhangsan@example.com",                                     │    │
│   │       "IdCard": "110101199001011234"                                       │    │
│   │     }                                                                      │    │
│   │   }                                                                        │    │
│   │                          │                                                 │    │
│   │                          ▼                                                 │    │
│   │   After PIIMaskingDecorator:                                               │    │
│   │   {                                                                        │    │
│   │     "EntityType": "Customer",                                              │    │
│   │     "NewValues": {                                                         │    │
│   │       "Name": "张*",                         // Masked                     │    │
│   │       "Phone": "138****5678",                // Masked                     │    │
│   │       "Email": "z***@example.com",           // Masked                     │    │
│   │       "IdCard": "1101**********1234"         // Masked                     │    │
│   │     }                                                                      │    │
│   │   }                                                                        │    │
│   │                          │                                                 │    │
│   │                          ▼                                                 │    │
│   │   After HashingDecorator:                                                  │    │
│   │   {                                                                        │    │
│   │     "EntityType": "Customer",                                              │    │
│   │     "NewValues": { ... masked ... },                                       │    │
│   │     "IntegrityHash": "sha256:a1b2c3d4e5f6...",  // Added                  │    │
│   │     "PreviousHash": "sha256:9f8e7d6c5b4a..."    // Chain link             │    │
│   │   }                                                                        │    │
│   │                          │                                                 │    │
│   │                          ▼                                                 │    │
│   │   After CompressionDecorator:                                              │    │
│   │   {                                                                        │    │
│   │     "CompressedPayload": "H4sIAAAAA...",     // GZip compressed           │    │
│   │     "OriginalSize": 512,                                                   │    │
│   │     "CompressedSize": 198                                                  │    │
│   │   }                                                                        │    │
│   │                                                                            │    │
│   └────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│   Benefits:                                                                          │
│   ✅ Each decorator has single responsibility                                       │
│   ✅ Can disable compression in dev (remove decorator)                              │
│   ✅ Can add encryption decorator later                                             │
│   ✅ Order configurable per environment                                             │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Decorator Responsibilities

| Decorator | Responsibility | When to Use |
|-----------|----------------|-------------|
| **PIIMaskingDecorator** | Mask sensitive personal data | All customer-related audits |
| **HashingDecorator** | Create integrity hash chain | Compliance-critical audits |
| **CompressionDecorator** | Reduce storage size | High-volume audit data |
| **EncryptionDecorator** | Encrypt at rest | Payment, sensitive business data |
| **TimestampDecorator** | Add certified timestamp | Legal compliance |

---

## 💻 Code Examples

### Interface and Core Implementation

```csharp
/// <summary>
/// 审计处理器接口 - 组件和装饰器共用
/// Audit Processor Interface - shared by component and decorators
/// </summary>
public interface IAuditProcessor
{
    Task<AuditEntry> ProcessAsync(EntityChange change);
}

/// <summary>
/// 核心审计处理器 - 实际的序列化和存储逻辑
/// Core Audit Processor - actual serialization and storage logic
/// </summary>
public class CoreAuditProcessor : IAuditProcessor
{
    private readonly IAuditStore _store;
    private readonly ISerializer _serializer;

    public CoreAuditProcessor(IAuditStore store, ISerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    public async Task<AuditEntry> ProcessAsync(EntityChange change)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            Action = change.Action,
            OldValues = _serializer.Serialize(change.OldValues),
            NewValues = _serializer.Serialize(change.NewValues),
            Timestamp = DateTime.UtcNow,
            UserId = change.UserId
        };

        await _store.SaveAsync(entry);
        return entry;
    }
}
```

### Base Decorator Class

```csharp
/// <summary>
/// 审计装饰器基类 - 提供默认委托行为
/// Base Audit Decorator - provides default delegation behavior
/// </summary>
public abstract class AuditProcessorDecorator : IAuditProcessor
{
    protected readonly IAuditProcessor _wrapped;

    protected AuditProcessorDecorator(IAuditProcessor wrapped)
    {
        _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
    }

    /// <summary>
    /// 默认实现直接委托给被包装对象
    /// Default implementation delegates to wrapped object
    /// </summary>
    public virtual async Task<AuditEntry> ProcessAsync(EntityChange change)
    {
        return await _wrapped.ProcessAsync(change);
    }
}
```

### PII Masking Decorator

```csharp
/// <summary>
/// PII脱敏装饰器 - 自动检测并掩码敏感数据
/// PII Masking Decorator - automatically detects and masks sensitive data
/// </summary>
public class PIIMaskingDecorator : AuditProcessorDecorator
{
    private readonly IPIIDetector _detector;
    private readonly IMaskingRules _rules;

    public PIIMaskingDecorator(
        IAuditProcessor wrapped,
        IPIIDetector detector,
        IMaskingRules rules) : base(wrapped)
    {
        _detector = detector;
        _rules = rules;
    }

    public override async Task<AuditEntry> ProcessAsync(EntityChange change)
    {
        // Pre-process: Mask PII before passing to inner processor
        var maskedChange = MaskSensitiveData(change);
        
        // Delegate to wrapped processor
        var entry = await _wrapped.ProcessAsync(maskedChange);
        
        // Post-process: Mark that masking was applied
        entry.Metadata["PIIMasked"] = true;
        entry.Metadata["MaskedFields"] = maskedChange.MaskedFields;
        
        return entry;
    }

    private EntityChange MaskSensitiveData(EntityChange original)
    {
        var masked = original.Clone();
        var maskedFields = new List<string>();

        // Mask NewValues
        if (masked.NewValues != null)
        {
            foreach (var field in _detector.DetectPII(masked.NewValues))
            {
                var value = masked.NewValues[field.Name]?.ToString();
                if (value != null)
                {
                    masked.NewValues[field.Name] = _rules.Mask(field.Type, value);
                    maskedFields.Add(field.Name);
                }
            }
        }

        // Mask OldValues
        if (masked.OldValues != null)
        {
            foreach (var field in _detector.DetectPII(masked.OldValues))
            {
                var value = masked.OldValues[field.Name]?.ToString();
                if (value != null)
                {
                    masked.OldValues[field.Name] = _rules.Mask(field.Type, value);
                }
            }
        }

        masked.MaskedFields = maskedFields;
        return masked;
    }
}

/// <summary>
/// 脱敏规则实现
/// Masking Rules Implementation
/// </summary>
public class ChineseMaskingRules : IMaskingRules
{
    public string Mask(PIIType type, string value) => type switch
    {
        PIIType.Phone => MaskPhone(value),
        PIIType.Email => MaskEmail(value),
        PIIType.IdCard => MaskIdCard(value),
        PIIType.BankCard => MaskBankCard(value),
        PIIType.Name => MaskName(value),
        PIIType.Address => MaskAddress(value),
        _ => value
    };

    private string MaskPhone(string phone)
    {
        // 13812345678 → 138****5678
        if (phone.Length >= 11)
            return phone[..3] + "****" + phone[^4..];
        return "***";
    }

    private string MaskEmail(string email)
    {
        // zhangsan@example.com → z***@example.com
        var parts = email.Split('@');
        if (parts.Length == 2 && parts[0].Length > 0)
            return parts[0][0] + "***@" + parts[1];
        return "***@***";
    }

    private string MaskIdCard(string idCard)
    {
        // 110101199001011234 → 1101**********1234
        if (idCard.Length >= 18)
            return idCard[..4] + "**********" + idCard[^4..];
        return "***";
    }

    private string MaskBankCard(string card)
    {
        // 6222021234567890123 → 6222**********0123
        if (card.Length >= 16)
            return card[..4] + "**********" + card[^4..];
        return "***";
    }

    private string MaskName(string name)
    {
        // 张三 → 张* | 张三丰 → 张*丰
        if (name.Length == 2)
            return name[0] + "*";
        if (name.Length > 2)
            return name[0] + new string('*', name.Length - 2) + name[^1];
        return "*";
    }

    private string MaskAddress(string address)
    {
        // 北京市朝阳区xxx街道xxx号 → 北京市朝阳区******
        var cityMatch = Regex.Match(address, @"^(.+?[省市区县])");
        if (cityMatch.Success)
            return cityMatch.Value + "******";
        return "******";
    }
}
```

### Hashing Decorator

```csharp
/// <summary>
/// 哈希完整性装饰器 - 创建不可篡改的审计链
/// Hashing Integrity Decorator - creates tamper-proof audit chain
/// </summary>
public class HashingDecorator : AuditProcessorDecorator
{
    private readonly IAuditStore _store;
    private string _lastHash = "GENESIS"; // 创世哈希

    public HashingDecorator(IAuditProcessor wrapped, IAuditStore store) 
        : base(wrapped)
    {
        _store = store;
    }

    public override async Task<AuditEntry> ProcessAsync(EntityChange change)
    {
        // Get previous hash for chain (获取前一个哈希用于链接)
        var previousHash = await GetPreviousHashAsync();
        
        // Delegate to get the entry
        var entry = await _wrapped.ProcessAsync(change);
        
        // Post-process: Add integrity hash
        entry.PreviousHash = previousHash;
        entry.IntegrityHash = ComputeHash(entry);
        
        // Update last hash
        _lastHash = entry.IntegrityHash;
        
        return entry;
    }

    private async Task<string> GetPreviousHashAsync()
    {
        // In production, get from database for true chain
        var lastEntry = await _store.GetLastEntryAsync();
        return lastEntry?.IntegrityHash ?? _lastHash;
    }

    private string ComputeHash(AuditEntry entry)
    {
        var content = $"{entry.PreviousHash}|" +
                      $"{entry.EntityType}|" +
                      $"{entry.EntityId}|" +
                      $"{entry.Action}|" +
                      $"{entry.NewValues}|" +
                      $"{entry.Timestamp:O}";
        
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return "sha256:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
```

### Conditional Decorator via Factory

```csharp
/// <summary>
/// 装饰器工厂 - 根据配置动态构建装饰器链
/// Decorator Factory - dynamically builds decorator chain based on config
/// </summary>
public class AuditProcessorFactory
{
    private readonly IServiceProvider _sp;
    private readonly AuditOptions _options;

    public AuditProcessorFactory(
        IServiceProvider sp, 
        IOptions<AuditOptions> options)
    {
        _sp = sp;
        _options = options.Value;
    }

    public IAuditProcessor Create()
    {
        // Start with core processor
        IAuditProcessor processor = new CoreAuditProcessor(
            _sp.GetRequiredService<IAuditStore>(),
            _sp.GetRequiredService<ISerializer>());

        // Add decorators based on configuration
        if (_options.EnableCompression)
        {
            processor = new CompressionDecorator(processor);
        }

        if (_options.EnablePIIMasking)
        {
            processor = new PIIMaskingDecorator(
                processor,
                _sp.GetRequiredService<IPIIDetector>(),
                _sp.GetRequiredService<IMaskingRules>());
        }

        if (_options.EnableIntegrityHash)
        {
            processor = new HashingDecorator(
                processor, 
                _sp.GetRequiredService<IAuditStore>());
        }

        if (_options.EnableEncryption)
        {
            processor = new EncryptionDecorator(
                processor,
                _sp.GetRequiredService<IEncryptionService>());
        }

        return processor;
    }
}

// Registration
services.AddSingleton<AuditProcessorFactory>();
services.AddScoped<IAuditProcessor>(sp => 
    sp.GetRequiredService<AuditProcessorFactory>().Create());
```

---

## ⚖️ Decorator vs Similar Patterns

### Comparison Table

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    DECORATOR vs RELATED PATTERNS                                     │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  Pattern       │ Wraps        │ Same Interface │ Intent                             │
│  ──────────────┼──────────────┼────────────────┼────────────────────────────────────│
│  Decorator     │ Object       │ Yes            │ Add responsibilities dynamically   │
│  Proxy         │ Object       │ Yes            │ Control access to object           │
│  Adapter       │ Object       │ No (converts)  │ Make incompatible interfaces work  │
│  Composite     │ Tree of objs │ Yes            │ Treat group as single object       │
│  Chain of Resp │ Request      │ Variable       │ Pass request through handlers      │
│                                                                                      │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  DECORATOR: Adds behavior, always calls wrapped                                     │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐                                       │
│  │Decorator │───▶│Decorator │───▶│  Core    │                                       │
│  │  adds A  │    │  adds B  │    │          │                                       │
│  └──────────┘    └──────────┘    └──────────┘                                       │
│                                                                                      │
│  CHAIN OF RESPONSIBILITY: May stop, may not call next                               │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐                                       │
│  │Handler 1 │─?─▶│Handler 2 │─?─▶│Handler 3 │                                       │
│  │can handle│    │can handle│    │can handle│                                       │
│  └──────────┘    └──────────┘    └──────────┘                                       │
│       │                │               │                                            │
│       └────────────────┴───────────────┘                                            │
│              Only ONE handles (or none)                                             │
│                                                                                      │
│  PROXY: Controls access, may not call real object                                   │
│  ┌──────────┐                   ┌──────────┐                                        │
│  │  Proxy   │─ ─ ─ ─ ─ ─ ─ ─ ─ ▶│  Real    │                                        │
│  │ (guards) │   (conditional)   │  Object  │                                        │
│  └──────────┘                   └──────────┘                                        │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Decision Guide

| Scenario | Best Pattern | Reason |
|----------|--------------|--------|
| Add logging + caching + validation | **Decorator** | Stack multiple behaviors |
| Lazy load heavy object | **Proxy** | Control instantiation |
| Use old API with new code | **Adapter** | Interface translation |
| Handle request by type | **Chain of Responsibility** | Conditional handling |
| Tree structure UI | **Composite** | Uniform tree operations |

---

## 🔬 Advanced Topics

### Order Matters

```csharp
// Order 1: Hash then Mask
var processor1 = new PIIMaskingDecorator(
    new HashingDecorator(core));
// Result: Hash of masked data (hash changes if masking changes)

// Order 2: Mask then Hash
var processor2 = new HashingDecorator(
    new PIIMaskingDecorator(core));
// Result: Hash of original, then mask for storage
// ✅ Better: Hash proves original data, masked for viewing

// DT-Express recommendation:
// 1. Compression (innermost - compress core output)
// 2. PII Masking (middle - mask before hashing)
// 3. Hashing (outermost - hash the final result)
```

### Decorator with State

```csharp
/// <summary>
/// 有状态的统计装饰器 - 追踪处理指标
/// Stateful Statistics Decorator - tracks processing metrics
/// </summary>
public class StatisticsDecorator : AuditProcessorDecorator
{
    private long _processedCount;
    private long _totalBytes;
    private readonly Stopwatch _totalTime = new();

    public override async Task<AuditEntry> ProcessAsync(EntityChange change)
    {
        _totalTime.Start();
        try
        {
            var entry = await _wrapped.ProcessAsync(change);
            
            Interlocked.Increment(ref _processedCount);
            Interlocked.Add(ref _totalBytes, entry.NewValues?.Length ?? 0);
            
            return entry;
        }
        finally
        {
            _totalTime.Stop();
        }
    }

    public AuditStatistics GetStatistics() => new()
    {
        ProcessedCount = _processedCount,
        TotalBytes = _totalBytes,
        AverageLatency = _processedCount > 0 
            ? _totalTime.Elapsed / _processedCount 
            : TimeSpan.Zero
    };
}
```

### Async Decorator Considerations

```csharp
/// <summary>
/// 异步装饰器最佳实践
/// Async Decorator Best Practices
/// </summary>
public class AsyncBestPracticesDecorator : AuditProcessorDecorator
{
    public override async Task<AuditEntry> ProcessAsync(EntityChange change)
    {
        // ✅ Await properly - don't block
        var entry = await _wrapped.ProcessAsync(change);
        
        // ✅ ConfigureAwait(false) for library code
        var externalData = await FetchExternalAsync()
            .ConfigureAwait(false);
        
        // ✅ Use ValueTask for hot paths if often sync
        // (but IAuditProcessor uses Task for simplicity)
        
        // ❌ Don't do this:
        // var entry = _wrapped.ProcessAsync(change).Result; // Deadlock risk!
        
        return entry;
    }
}
```

---

## ⚠️ Common Pitfalls

### 1. Forgetting to Call Wrapped

```csharp
// ❌ BAD: Forgot to delegate
public override async Task<AuditEntry> ProcessAsync(EntityChange change)
{
    var masked = MaskPII(change);
    return new AuditEntry { ... }; // Lost all inner processing!
}

// ✅ GOOD: Always delegate
public override async Task<AuditEntry> ProcessAsync(EntityChange change)
{
    var masked = MaskPII(change);
    return await _wrapped.ProcessAsync(masked); // Delegates to chain
}
```

### 2. Order-Dependent Bugs

```csharp
// ❌ BAD: Validation after modification
var processor = new ValidationDecorator(
    new ModificationDecorator(core));
// Validates the modified data, not original!

// ✅ GOOD: Validate first
var processor = new ModificationDecorator(
    new ValidationDecorator(core));
// Validates original, then modifies
```

### 3. Shared Mutable State

```csharp
// ❌ BAD: Shared state without synchronization
public class CountingDecorator : AuditProcessorDecorator
{
    private int _count; // Shared across requests!
    
    public override async Task<AuditEntry> ProcessAsync(...)
    {
        _count++; // Race condition!
    }
}

// ✅ GOOD: Thread-safe state
public class CountingDecorator : AuditProcessorDecorator
{
    private long _count;
    
    public override async Task<AuditEntry> ProcessAsync(...)
    {
        Interlocked.Increment(ref _count);
    }
}
```

### 4. Performance Overhead

```csharp
// ❌ BAD: Heavy operation in every decorator call
public override async Task<AuditEntry> ProcessAsync(...)
{
    await LoadConfigFromDatabase(); // Every time!
    var entry = await _wrapped.ProcessAsync(change);
    return entry;
}

// ✅ GOOD: Cache expensive operations
private MaskingConfig? _cachedConfig;

public override async Task<AuditEntry> ProcessAsync(...)
{
    _cachedConfig ??= await LoadConfigFromDatabase();
    var entry = await _wrapped.ProcessAsync(change);
    return entry;
}
```

---

## 🇨🇳 Chinese Tech References

### Industry Examples

| Company | Implementation | Reference |
|---------|----------------|-----------|
| **阿里巴巴** | 日志脱敏组件 | CSDN: `阿里日志脱敏` |
| **蚂蚁金服** | 数据安全脱敏 | Search: `蚂蚁金服数据脱敏` |
| **腾讯云** | 敏感数据保护 | Tencent Cloud docs |

### Search Keywords

| Topic | Search Terms |
|-------|--------------|
| C#装饰器模式 | `C# Decorator Pattern 实现 中文` |
| 日志脱敏 | `日志脱敏 最佳实践 .NET` |
| PII处理 | `个人信息保护 脱敏规则 中国` |

---

## ✅ Self-Assessment

### Knowledge Check

1. **What's the key difference between Decorator and Proxy?**
   - Decorator always delegates; Proxy may not call real object

2. **Why use Decorator over inheritance?**
   - Avoid combinatorial explosion, runtime flexibility

3. **How do you ensure correct decorator order?**
   - Think about data flow: what needs to happen first?

4. **When would you NOT use Decorator?**
   - When all instances need all behaviors (bake it in)

### Coding Challenge

Implement an `AuditThrottlingDecorator` that:
1. Limits audit writes to 100/second per entity type
2. Drops excess with logging
3. Reports throttle statistics
4. Doesn't affect unthrottled entity types

### Discussion Questions

1. How would you test a decorator chain?
2. What if decorator A depends on decorator B's output?
3. How to handle decorator failures gracefully?

---

## 🔗 Related Patterns

- **Interceptor Pattern**: For transparent cross-cutting → [INTERCEPTOR-PATTERN.md](INTERCEPTOR-PATTERN.md)
- **Strategy Pattern**: For swappable algorithms → [STRATEGY-PATTERN.md](STRATEGY-PATTERN.md)
- **Chain of Responsibility**: For conditional handling → External reference

---