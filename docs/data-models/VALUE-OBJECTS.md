# 🎯 VALUE OBJECTS

## 🎯 Purpose

> **Value Objects** are immutable domain concepts with no identity.  
> Two value objects are equal if all their properties are equal.

They capture **domain meaning** and enforce **invariants** at the type level.

---

## 📋 Core Value Objects

### Address
```
Address (Value Object)
│
├── street: string
├── city: string
├── district: string
├── province: string
├── postalCode: string
├── country: string (default: "CN")
└── formattedAddress: string (computed)

Invariants:
- postalCode matches country format
- province must be valid for country

Used by: Order, Customer, Warehouse, NetworkNode
```

---

### GeoCoordinate
```
GeoCoordinate (Value Object)
│
├── latitude: decimal (-90 to 90)
├── longitude: decimal (-180 to 180)
└── altitude: decimal? (meters, optional)

Operations:
- DistanceTo(other: GeoCoordinate): Distance
- IsWithinRadius(center: GeoCoordinate, radius: Distance): bool

Invariants:
- latitude in valid range
- longitude in valid range

Used by: Warehouse, NetworkNode, Shipment (current location)
```

---

### Money
```
Money (Value Object)
│
├── amount: decimal
└── currency: CurrencyCode (enum: CNY, USD, EUR, etc.)

Operations:
- Add(other: Money): Money (same currency only)
- Multiply(factor: decimal): Money
- ConvertTo(targetCurrency: CurrencyCode, rate: decimal): Money

Invariants:
- amount >= 0 (or allow negative for refunds)
- currency must be supported

Used by: Order (total), ServiceLevel (pricing), Carrier (rates)
```

---

### Weight
```
Weight (Value Object)
│
├── value: decimal
└── unit: WeightUnit (enum: KG, G, LB)

Operations:
- ToKilograms(): Weight
- Add(other: Weight): Weight
- IsGreaterThan(other: Weight): bool

Invariants:
- value > 0
- unit must be valid

Used by: Order (item weight), Shipment, ServiceLevel (max weight)
```

---

### Dimension
```
Dimension (Value Object)
│
├── length: decimal
├── width: decimal
├── height: decimal
└── unit: DimensionUnit (enum: CM, M, IN)

Operations:
- Volume(): decimal
- VolumetricWeight(factor: int = 5000): Weight  // length×width×height/5000
- FitsWithin(container: Dimension): bool

Invariants:
- all dimensions > 0
- unit must be valid

Used by: Order (item dimensions), Shipment, ServiceLevel (max size)
```

---

### Distance
```
Distance (Value Object)
│
├── value: decimal
└── unit: DistanceUnit (enum: KM, M, MI)

Operations:
- ToKilometers(): Distance
- Add(other: Distance): Distance

Invariants:
- value >= 0
- unit must be valid

Used by: NetworkNode (connection distance), Routing calculations
```

---

### TimeWindow
```
TimeWindow (Value Object)
│
├── start: DateTime
├── end: DateTime
└── timezone: string

Operations:
- Duration(): TimeSpan
- Contains(datetime: DateTime): bool
- Overlaps(other: TimeWindow): bool

Invariants:
- end > start
- timezone must be valid IANA timezone

Used by: Order (requested delivery), Warehouse (operating hours)
```

---

### Duration
```
Duration (Value Object)
│
├── hours: int
├── minutes: int

Operations:
- TotalHours(): decimal
- TotalMinutes(): decimal
- Add(other: Duration): Duration

Invariants:
- All components >= 0
- Total duration >= 0

Used by: NetworkNode (transit time), ServiceLevel (max delivery hours)
```

---

### ContactInfo
```
ContactInfo (Value Object)
│
├── name: string
├── phone: string
└── email: string?

Invariants:
- name not empty
- phone matches valid format (CN mobile: 1[3-9]XXXXXXXXX)
- email matches valid format if provided

Used by: Customer (primary contact), Order (recipient contact)
```

---

## 🔗 Value Object Relationship Map

```
┌──────────────────────────────────────────────────────────────────────┐
│                   VALUE OBJECT USAGE MATRIX                          │
├──────────────────────────────────────────────────────────────────────┤
│               │Order│Shipment│Carrier│Customer│Warehouse│Node│Service│
│  ─────────────┼─────┼────────┼───────┼────────┼─────────┼────┼───────│
│  Address      │  ✓  │        │       │   ✓   │    ✓   │ ✓  │       │
│  GeoCoordinate│     │   ✓    │       │        │    ✓    │ ✓  │      │
│  Money        │  ✓  │        │   ✓   │   ✓   │         │    │   ✓   │
│  Weight       │  ✓  │   ✓    │       │       │         │    │   ✓   │
│  Dimension    │  ✓  │   ✓    │       │       │         │    │   ✓   │
│  Distance     │     │        │       │        │         │ ✓  │       │
│  TimeWindow   │  ✓  │        │       │       │    ✓    │    │       │
│  Duration     │     │        │       │        │         │ ✓  │   ✓  │
│  ContactInfo  │  ✓  │        │       │   ✓   │         │    │       │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 💡 Design Principles

### Why Value Objects?

```
❌ Primitive Obsession:
   order.weight = 5.5;        // 5.5 what? kg? lbs?
   order.price = 100;         // 100 what? CNY? USD?

✅ Domain-Rich Types:
   order.weight = Weight.Kilograms(5.5);
   order.price = Money.CNY(100);
```

### Immutability Rule

```
// Value Objects are IMMUTABLE
// Operations return NEW instances

Weight w1 = Weight.Kilograms(5);
Weight w2 = Weight.Kilograms(3);
Weight w3 = w1.Add(w2);  // Returns new Weight(8, KG)
                         // w1 and w2 unchanged
```

### Equality by Value

```
Address a1 = new Address("Beijing", "Chaoyang", "123 Street");
Address a2 = new Address("Beijing", "Chaoyang", "123 Street");

a1.Equals(a2);  // TRUE - same values
a1 == a2;       // TRUE - value equality
```

---

## 📊 Enum Types (Supporting Value Objects)

```
WeightUnit       = { KG, G, LB }
DimensionUnit    = { CM, M, IN }
DistanceUnit     = { KM, M, MI }
CurrencyCode     = { CNY, USD, EUR, HKD, JPY }
TransportMode    = { TRUCK, AIR, RAIL, SEA }
```

---

## 🔧 Common Operations Pattern

```
Value Object Base Operations:
│
├── Equality
│   ├── Equals(other): bool
│   └── GetHashCode(): int
│
├── Comparison (where applicable)
│   ├── CompareTo(other): int
│   ├── IsGreaterThan(other): bool
│   └── IsLessThan(other): bool
│
├── Arithmetic (where applicable)
│   ├── Add(other): Self
│   ├── Subtract(other): Self
│   └── Multiply(factor): Self
│
├── Conversion
│   ├── ToUnit(targetUnit): Self
│   └── ToString(): string
│
└── Validation
    └── (enforced in constructor)
```

---
