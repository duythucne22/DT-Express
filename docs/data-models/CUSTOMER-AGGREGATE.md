# 👤 CUSTOMER Aggregate

## 🎯 Responsibility

> **Single Purpose**: Represent **business relationships** and customer-specific configurations.

Customer is the anchor point for orders, contracts, and service preferences.

---

## 🔗 Relationship Context

```
                                 ┌─────────────┐
                                 │   SERVICE   │
                                 │    LEVEL    │
                                 │(subscribed) │
                                 └──────┬──────┘
                                        │ N
                                        │
                                        ▼ M
┌─────────────────────────────────────────────────────┐
│                     CUSTOMER                        │
│                   (Aggregate)                       │
└─────────────────────────┬───────────────────────────┘
                          │
       ┌──────────────────┼──────────────────┐
       │                  │                  │
       ▼ N                ▼ N                ▼ N:M
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│    ORDER    │    │   ADDRESS   │    │   CARRIER   │
│  (placed)   │    │    BOOK     │    │ (preferred) │
└─────────────┘    │  (owned)    │    └─────────────┘
                   └─────────────┘
```

---

## 📋 Core Structure

```
Customer (Aggregate Root)
│
├── Identity
│   ├── customerId: CustomerId
│   └── customerCode: string (business identifier)
│
├── Basic Info
│   ├── name: string
│   ├── type: CustomerType (INDIVIDUAL | BUSINESS | ENTERPRISE)
│   └── status: CustomerStatus (ACTIVE | SUSPENDED | CLOSED)
│
├── Contact
│   ├── primaryContact: ContactInfo (Value Object)
│   │   ├── name: string
│   │   ├── phone: string
│   │   └── email: string
│   └── billingAddress: Address
│
├── Address Book (Owned Entities)
│   └── addresses: SavedAddress[]
│       ├── addressId: AddressId
│       ├── label: string ("Home", "Office", etc.)
│       ├── address: Address
│       └── isDefault: bool
│
├── Service Configuration
│   ├── subscribedServiceIds: ServiceLevelId[]
│   ├── preferredCarrierIds: CarrierId[]
│   └── defaultServiceLevelId: ServiceLevelId?
│
├── Business Rules
│   ├── tier: CustomerTier (STANDARD | PREMIUM | VIP)
│   ├── creditLimit: Money?
│   └── paymentTerms: PaymentTerms? (NET_30, PREPAID, etc.)
│
└── Timestamps
    └── createdAt: DateTime
```

---

## 🎭 Customer Tiers

```
┌───────────────────────────────────────────────────────────────────┐
│                       CUSTOMER TIER BENEFITS                      │
├───────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐  │
│  │    STANDARD     │   │     PREMIUM     │   │       VIP       │  │
│  │                 │   │                 │   │                 │  │
│  │ • Base pricing  │   │ • 5% discount   │   │ • 15% discount  │  │
│  │ • Standard SLA  │   │ • Priority pick │   │ • Dedicated rep │  │
│  │ • Self-service  │   │ • Phone support │   │ • Custom SLAs   │  │
│  │                 │   │ • Credit terms  │   │ • API access    │  │
│  └─────────────────┘   └─────────────────┘   └─────────────────┘  │
│  Tier affects: Pricing, SLA commitments, Support level, Features  │
└───────────────────────────────────────────────────────────────────┘
```

---

## 🔑 Key Business Rules

### Invariants
```
1. CustomerCode MUST be unique
2. ACTIVE customer MUST have valid primaryContact
3. subscribedServiceIds can only contain active ServiceLevels
4. preferredCarrierIds can only contain active Carriers
5. VIP customers MUST have creditLimit defined
```

### Validation Rules
```
CreateOrder Validation:
├── Check: customer.status == ACTIVE
├── Check: order.serviceLevelId IN customer.subscribedServiceIds
├── Check: order.total <= customer.creditLimit (if credit)
└── Check: customer has valid billingAddress
```

---

## 🔗 Integration Points

| Connects To | Relationship | Purpose |
|-------------|--------------|---------|
| Order | 1:N | Customer places orders |
| ServiceLevel | N:M | Customer subscribes to services |
| Carrier | N:M | Customer may have preferred carriers |
| Audit | - | All changes tracked by customerId |

---

## 💡 Design Decisions

### Why Address Book in Aggregate?
```
Frequently used together:
- Customer places order → selects from saved addresses
- No cross-aggregate consistency needed
- Performance: load once, use many times

AddressBook lifecycle = Customer lifecycle
```

### Why Service Subscription Model?
```
Not all customers can use all services:

Enterprise Customer:
└── subscribedServiceIds: [STANDARD, EXPRESS, SAME_DAY, WHITE_GLOVE]

Individual Customer:
└── subscribedServiceIds: [STANDARD, EXPRESS]

Enables:
- Tiered product offerings
- Custom pricing per customer
- Service availability validation
```

### Why Preferred Carriers?
```
Business scenarios:
- Enterprise contract specifies "Only SF Express"
- Customer had bad experience with ZTO → exclude
- Regional customers prefer local carriers

Order Processing checks:
CarrierSelection.Filter(preferredCarrierIds)
```

---

## 📊 Customer Type Differences

| Aspect | INDIVIDUAL | BUSINESS | ENTERPRISE |
|--------|------------|----------|------------|
| Payment | Prepaid | NET_15 | NET_30/60 |
| Credit Limit | None | Low | High |
| Support | Self-service | Phone | Dedicated |
| API Access | No | Limited | Full |
| Volume Pricing | No | Yes | Custom |

---
