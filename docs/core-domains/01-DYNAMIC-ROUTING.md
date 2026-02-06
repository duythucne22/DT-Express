# 🚀 01-DYNAMIC-ROUTING — Multi-Pattern Design Spec

> **Domain**: Package Routing — Route Calculation with Clean Architecture  
> **Patterns**: Strategy · Decorator · Factory · Dependency Inversion  
> **Goal**: Demonstrate clean architectural thinking — algorithms + patterns, properly separated  
> **Status**: 📐 Design Complete → Ready for Implementation

---

## 📋 Table of Contents

1. [Domain Overview](#-domain-overview)
2. [Business Context](#-business-context)
3. [Architecture Layers](#-architecture-layers)
4. [Feature Specification](#-feature-specification)
5. [Design Pattern Application](#-design-pattern-application)
6. [Interface Contracts](#-interface-contracts)
7. [Data Models](#-data-models)
8. [Algorithm Layer](#-algorithm-layer)
9. [Infrastructure Layer](#-infrastructure-layer)
10. [Acceptance Criteria](#-acceptance-criteria)
11. [Project Structure](#-project-structure)
12. [Interview Guide](#-interview-guide)
13. [Study Resources](#-study-resources)
14. [Related Documents](#-related-documents)

---

## 🎯 Domain Overview

### Elevator Pitch

> "A route calculation system demonstrating multiple design patterns with clean architecture.  
> The core uses **Strategy Pattern** for interchangeable routing behaviors.  
> Each strategy delegates to specialized **algorithm services**, keeping concerns separated.  
> **Decorator Pattern** handles cross-cutting concerns like caching.  
> **Factory Pattern** creates strategies from configuration.  
> Every layer is abstracted, testable, and independently replaceable."

### Purpose

Calculate delivery routes between warehouse → customer. Different delivery types need different optimization:

| Strategy | Internal Algorithm | Optimizes For | Use Case |
|----------|-------------------|---------------|----------|
| **Fastest Route** | A* pathfinding | Minimum travel time | Express / urgent |
| **Cheapest Route** | Dijkstra shortest path | Minimum cost | Economy / budget |
| **Balanced Route** | Weighted multi-objective | Best time-to-cost ratio | Standard |

### The Key Insight

> Strategies handle **business decisions** (what to optimize).  
> Algorithms handle **computation** (how to find the path).  
> These are **separate responsibilities** — mixing them is an SRP violation.

### Scope

| In Scope | Out of Scope |
|----------|--------------|
| Strategy Pattern — interchangeable route behaviors | GPS tracking (→ 03-REALTIME-TRACKING) |
| Decorator Pattern — caching, logging, validation | Carrier assignment (→ 02-MULTI-CARRIER) |
| Factory Pattern — strategy creation from config | Order management (→ 04-ORDER-PROCESSING) |
| Algorithm services — A*, Dijkstra as separate layer | Driver / fleet management |
| Map service abstraction — `IMapService` interface | Production-grade map API integration |

---

## 💼 Business Context

### User Story

> **As a** dispatcher,  
> **I want to** calculate package routes using different algorithms,  
> **So that** I can optimize for speed vs. cost depending on the delivery type.

### Business Rules

| # | Rule | How It's Enforced |
|---|------|-------------------|
| 1 | Packages have a weight and delivery type (Express / Standard / Economy) | `RouteRequest.Package.Priority` enum |
| 2 | Each strategy optimizes a different metric via its internal algorithm | Strategy delegates to `IPathfinder` |
| 3 | Algorithms can be switched at runtime without recompilation | `RouteCalculator.SetStrategy()` |
| 4 | Adding a new algorithm must not require changes to existing code | New class implementing `IRouteStrategy` |
| 5 | Cross-cutting concerns (caching, logging) don't pollute business logic | Decorator wraps strategy |

### Use Case: Calculate & Compare Routes

```
Actor:      Dispatcher (or System via auto-selection)
Precondition: Valid origin, destination, and package info

Flow:
  1. Factory creates the appropriate strategy (from config or request)
  2. Decorator wraps it with caching + validation
  3. RouteCalculator holds the wrapped strategy
  4. Call calculator.CalculateRoute(request)
  5. Strategy builds a graph → delegates to pathfinder algorithm → returns Route
  6. (Optional) Compare all strategies on the same request

Postcondition: One or more Route results ready for display / selection
```

---

## 🏗 Architecture Layers

### The Separation Principle

```
┌────────────────────────────────────────────────────────────────┐
│                      CLEAN ARCHITECTURE                        │
├────────────────────────────────────────────────────────────────┤
│   ┌───────────────────────────────────────────────────────┐    │
│   │  CORE LAYER (pure C#, zero dependencies)              │    │
│   │                                                       │    │
│   │  • IRouteStrategy + 3 concrete strategies             │    │
│   │  • RouteCalculator (context)                          │    │
│   │  • Domain models (Route, Location, Package)           │    │
│   │  • Strategy focuses on BUSINESS LOGIC only            │    │
│   │  • Delegates computation to algorithm interfaces      │    │
│   └───────────────────────┬───────────────────────────────┘    │
│                           │ depends on                         │
│   ┌───────────────────────▼───────────────────────────────┐    │
│   │  ALGORITHM LAYER (pure computation, no business)      │    │
│   │                                                       │    │
│   │  • IPathfinder, IGraph, IHeuristic interfaces         │    │
│   │  • AStarPathfinder — heuristic search                 │    │
│   │  • DijkstraPathfinder — shortest path                 │    │
│   │  • WeightedScoreCalculator — multi-objective          │    │
│   │  • Graph, Node, Edge data structures                  │    │
│   │  • Can be tested with pure math, no mocking needed    │    │
│   └───────────────────────┬───────────────────────────────┘    │
│                           │ depends on                         │
│   ┌───────────────────────▼───────────────────────────────┐    │
│   │  INFRASTRUCTURE LAYER (external world)                │    │
│   │                                                       │    │
│   │  • IMapService → GoogleMapsService                    │    │
│   │  • ICache → InMemoryCache (ConcurrentDictionary)      │    │
│   │  • CachingRouteDecorator (Decorator Pattern)          │    │
│   │  • LoggingRouteDecorator (Decorator Pattern)          │    │
│   │  • ValidationRouteDecorator (Decorator Pattern)       │    │
│   └───────────────────────┬───────────────────────────────┘    │
│                           │ used by                            │
│   ┌───────────────────────▼───────────────────────────────┐    │
│   │  DEMO LAYER (composition root)                        │    │
│   │                                                       │    │
│   │  • Program.cs — wires everything together             │    │
│   │  • StrategyFactory — creates strategies from config   │    │
│   │  • Console output showing all patterns in action      │    │
│   └───────────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────────────┘
```

### Why This Layering Matters

| Layer | What Goes Here | What Does NOT Go Here |
|-------|---------------|----------------------|
| **Core** | Business rules, strategy interface, domain models | HTTP calls, caching logic, file I/O |
| **Algorithm** | Pure pathfinding math, graph structures | Business rules, API keys, JSON parsing |
| **Infrastructure** | API clients, cache, decorators | Domain decisions, algorithm logic |
| **Demo** | DI wiring, console output, config | Business logic, algorithms |

---

## 📝 Feature Specification

Three **power features** — each demonstrates a different pattern.

| # | Feature | Pattern Demonstrated | Interview Value |
|---|---------|---------------------|----------------|
| **F1** | Strategy with Algorithm Delegation | Strategy + Dependency Inversion | Core pattern + SRP |
| **F2** | Decorator for Cross-Cutting Concerns | Decorator Pattern | Shows deep pattern knowledge |
| **F3** | Factory for Strategy Creation | Factory Pattern | Shows object creation patterns |

### F1 — Strategy Pattern with Algorithm Delegation (MUST HAVE)

**Description**: Three strategies, each delegating to a specialized pathfinding algorithm. Strategies handle business logic; algorithms handle computation.

**The Wrong Way vs. The Right Way:**

```
❌ WRONG: Algorithm logic INSIDE the strategy (SRP violation)
┌─────────────────────────────────────────────┐
│  FastestRouteStrategy                       │
│  {                                          │
│      Calculate(request)                     │
│      {                                      │
│          // 150 lines of A* implementation  │
│          // Business + algorithm = MIXED    │
│      }                                      │
│  }                                          │
└─────────────────────────────────────────────┘

✅ RIGHT: Strategy DELEGATES to algorithm service
┌─────────────────────────────────────────────┐
│  FastestRouteStrategy                       │
│  {                                          │
│      private IPathfinder _pathfinder;  // injected          
│                                             │
│      Calculate(request)                     │
│      {                                      │
│          graph = BuildGraph(request);  // business logic    │
│          path = _pathfinder.FindPath(graph); // delegation  │
│          return ConvertToRoute(path); // business logic     │
│      }                                      │
│  }                                          │
└─────────────────────────────────────────────┘
```

**Acceptance**:
- [ ] Each strategy receives its algorithm via constructor injection
- [ ] `FastestRouteStrategy` uses `AStarPathfinder`
- [ ] `CheapestRouteStrategy` uses `DijkstraPathfinder`
- [ ] `BalancedRouteStrategy` uses `WeightedScoreCalculator`
- [ ] Strategies contain zero pathfinding math — only business logic
- [ ] Algorithms contain zero business rules — only computation

### F2 — Decorator Pattern for Cross-Cutting Concerns (INTERVIEW GOLD)

**Description**: Wrap any `IRouteStrategy` with decorators that add caching, validation, or logging — without modifying the strategy itself.

```
How decorators compose (like Russian nesting dolls):

┌─ ValidationDecorator ───────────────────────────────────┐
│  validates request → passes to inner                    │
│  ┌─ CachingDecorator ─────────────────────────────────┐ │
│  │  checks cache → if miss, passes to inner           │ │
│  │  ┌─ LoggingDecorator ────────────────────────────┐ │ │
│  │  │  logs start → passes to inner → logs result   │ │ │
│  │  │  ┌─ FastestRouteStrategy ───────────────────┐ │ │ │
│  │  │  │  actual business logic + algorithm call  │ │ │ │
│  │  │  └──────────────────────────────────────────┘ │ │ │
│  │  └───────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Acceptance**:
- [ ] `CachingRouteDecorator` implements `IRouteStrategy` (same interface!)
- [ ] Cache hit → returns cached route, skips inner strategy
- [ ] Cache miss → delegates to inner, caches result
- [ ] `LoggingRouteDecorator` logs strategy name + execution time
- [ ] `ValidationRouteDecorator` validates request before delegating
- [ ] Decorators can be composed in any order
- [ ] Inner strategy is completely unaware it's being decorated

### F3 — Factory Pattern for Strategy Creation (NICE TO HAVE)

**Description**: Create the right strategy (with its decorator stack) based on configuration or the request's service level.

```
Factory logic:

  Input: ServiceLevel.Express
    → Creates: FastestRouteStrategy(aStarPathfinder)
    → Wraps:   CachingDecorator(LoggingDecorator(strategy))

  Input: ServiceLevel.Economy
    → Creates: CheapestRouteStrategy(dijkstraPathfinder)
    → Wraps:   CachingDecorator(LoggingDecorator(strategy))

  Input: "fastest" (string from config)
    → Same as Express
```

**Acceptance**:
- [ ] `RouteStrategyFactory.Create(serviceLevelOrName)` returns a fully decorated strategy
- [ ] Factory hides the complexity of wiring strategies + algorithms + decorators
- [ ] New strategy can be added by updating factory — no other code changes

---

## 🎨 Design Pattern Application

### Multi-Pattern Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     PATTERN COLLABORATION                               │
├─────────────────────────────────────────────────────────────────────────┤
│  FACTORY PATTERN (creates)         STRATEGY PATTERN (selects behavior)  │
│  ─────────────────────────         ──────────────────────────────────   │
│  ┌───────────────────────┐         ┌──────────────────────────────┐     │
│  │ RouteStrategyFactory  │────────>│  «interface»                 │     │
│  │                       │ creates │  IRouteStrategy              │     │
│  │ + Create(name)        │         ├──────────────────────────────┤     │
│  │ + Create(serviceLevel)│         │ + Name : string              │     │
│  └───────────────────────┘         │ + Calculate(req) : Route     │     │
│                                    └──────────────┬───────────────┘     │
│                                                   │                     │
│          DECORATOR PATTERN                        │ implements          │
│          (wraps behavior)                         │                     │
│    ┌──────────────┬─────────────┬─────────────────┤                     │
│    │              │             │                 │                     │
│    ▼              ▼             ▼                 ▼                     │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐   ┌──────────────┐               │
│ │ Caching  │ │ Logging  │ │Validation│   │ Fastest      │               │
│ │ Decorator│ │ Decorator│ │ Decorator│   │ Strategy     │               │
│ │          │ │          │ │          │   ├──────────────┤               │
│ │ wraps    │ │ wraps    │ │ wraps    │   │ uses A*      │               │
│ │ inner    │ │ inner    │ │ inner    │   │ via injection│               │
│ └──────────┘ └──────────┘ └──────────┘   └──────┬───────┘               │
│                                                 │                       │
│          DEPENDENCY INVERSION                   │ delegates to          │
│          (algorithms as services)               │                       │
│                                                 ▼                       │
│                                    ┌──────────────────────────┐         │
│                                    │  «interface»             │         │
│                                    │  IPathfinder             │         │
│                                    ├──────────────────────────┤         │
│                                    │ + FindPath(graph) : Path │         │
│                                    └──────────────┬───────────┘         │
│                                                   │                     │
│                                    ┌──────────────┼──────────┐          │
│                                    ▼              ▼          ▼          │
│                               ┌─────────┐  ┌──────────┐ ┌────────┐      │
│                               │ A*      │  │ Dijkstra │ │Weighted│      │
│                               │Pathfind │  │Pathfinder│ │Score   │      │
│                               └─────────┘  └──────────┘ └────────┘      │
└─────────────────────────────────────────────────────────────────────────┘
```

### Pattern Roles Summary

| Pattern | Role in This System | What It Enables |
|---------|-------------------|-----------------|
| **Strategy** | Swap routing behaviors at runtime | Open/Closed Principle — new algorithms without changing existing code |
| **Decorator** | Add caching/logging/validation as wrappers | Cross-cutting concerns without polluting business logic |
| **Factory** | Create strategy + decorator stacks from config | Hides wiring complexity, single point of creation |
| **Dependency Inversion** | Strategies depend on `IPathfinder`, not concrete algorithms | Algorithm is testable and swappable independently |

### Strategy vs. If-Else (Why Not Just Switch?)

| Approach | Adding New Algorithm | Risk to Existing Code | Testability |
|----------|---------------------|-----------------------|-------------|
| `if-else` / `switch` | Modify existing class ❌ | High — can break other branches | Hard to isolate |
| **Strategy Pattern** | Add new class only ✅ | Zero — existing code untouched | Each strategy tested alone |

---

## 📜 Interface Contracts

### Core Layer

#### IRouteStrategy — Business Behavior Contract

```
Interface: IRouteStrategy
Layer:     Core
Purpose:   Contract for all routing behaviors (strategies AND decorators)

┌────────────────────────────────────────────────────────────┐
│ string Name { get; }                                       │
│   → Strategy identifier (e.g., "fastest", "cheapest")      │
├────────────────────────────────────────────────────────────┤
│ Route Calculate(RouteRequest request)                      │
│   → Input:  RouteRequest with origin, destination, package │
│   → Output: Route with path, cost, time, distance          │
│   → Throws: ArgumentException on invalid input             │
└────────────────────────────────────────────────────────────┘

Key: Decorators ALSO implement this interface — that's
     what makes them composable with strategies.
```

#### RouteCalculator — Context

```
Class: RouteCalculator
Layer: Core
Purpose: Holds the active strategy, delegates route calculation

┌────────────────────────────────────────────────────────────┐
│ void SetStrategy(IRouteStrategy strategy)                  │
│   → Swaps the active routing algorithm (or decorated stack)│
│   → Throws: ArgumentNullException if strategy is null      │
├────────────────────────────────────────────────────────────┤
│ Route CalculateRoute(RouteRequest request)                 │
│   → Delegates to _strategy.Calculate(request)              │
│   → Throws: InvalidOperationException if no strategy set   │
├────────────────────────────────────────────────────────────┤
│ List<(string Name, Route Result)> CompareAll(              │
│     RouteRequest request,                                  │
│     IEnumerable<IRouteStrategy> strategies)                │
│   → Runs request against all provided strategies           │
│   → Returns named results for comparison                   │
└────────────────────────────────────────────────────────────┘
```

### Algorithm Layer

#### IPathfinder — Pure Computation Contract

```
Interface: IPathfinder
Layer:     Algorithms
Purpose:   Find a path through a graph (no business logic)

┌────────────────────────────────────────────────────────────┐
│ Path FindPath(IGraph graph, Node start, Node end)          │
│   → Input:  Graph structure + start/end nodes              │
│   → Output: Ordered path with total weight                 │
│   → Throws: PathNotFoundException if no path exists        │
└────────────────────────────────────────────────────────────┘

Implementations:
  • AStarPathfinder  — uses heuristic for faster search
  • DijkstraPathfinder — guarantees shortest path, no heuristic
```

#### IGraph — Graph Data Structure Contract

```
Interface: IGraph
Layer:     Algorithms
Purpose:   Abstract graph for pathfinding algorithms

┌────────────────────────────────────────────────────────────┐
│ IEnumerable<Node> Nodes { get; }                           │
│ IEnumerable<Edge> GetEdges(Node from)                      │
│ double GetWeight(Node from, Node to)                       │
└────────────────────────────────────────────────────────────┘
```

### Infrastructure Layer

#### IMapService — External Map Abstraction

```
Interface: IMapService
Layer:     Infrastructure
Purpose:   Abstract away external map APIs (Google Maps, Mapbox, etc.)

┌────────────────────────────────────────────────────────────┐
│ Task<Coordinates> GeocodeAsync(string address)             │
│   → Converts address string to lat/lng coordinates         │
├────────────────────────────────────────────────────────────┤
│ Task<ExternalRoute> GetRouteAsync(                         │
│     Coordinates from, Coordinates to, RoutePreference pref)│
│   → Gets route from external map provider                  │
└────────────────────────────────────────────────────────────┘

Implementations:
  • GoogleMapsService — actual API calls (can be mocked)
  • StubMapService — returns fake data for testing/demo
```

#### ICache — Simple Cache Abstraction

```
Interface: ICache
Layer:     Infrastructure
Purpose:   Abstract cache for decorator to use

┌────────────────────────────────────────────────────────────┐
│ bool TryGet<T>(string key, out T value)                    │
│ void Set<T>(string key, T value, TimeSpan expiry)          │
└────────────────────────────────────────────────────────────┘

Implementation:
  • InMemoryCache — ConcurrentDictionary (no Redis needed)
```

---

## 📊 Data Models

### Core Models (Domain)

#### RouteRequest (Input)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| From | Location | ✅ | Starting point (warehouse) |
| To | Location | ✅ | Destination (customer) |
| Package | Package | ✅ | Package details (weight, priority) |

#### Route (Output)

| Property | Type | Description |
|----------|------|-------------|
| StrategyUsed | string | Which strategy produced this route |
| Path | List\<Location\> | Ordered waypoints from origin → destination |
| Distance | double | Total distance in km |
| Cost | double | Estimated cost in currency |
| EstimatedTime | double | Estimated travel time in hours |

#### Location

| Property | Type | Description |
|----------|------|-------------|
| Name | string | Human-readable label (e.g., "Warehouse A") |
| Lat | double | Latitude |
| Lng | double | Longitude |

#### Package

| Property | Type | Description |
|----------|------|-------------|
| Weight | double | Weight in kg |
| Priority | Priority (enum) | Express / Standard / Economy |

### Algorithm Models (Pure Math)

#### Node

| Property | Type | Description |
|----------|------|-------------|
| Id | string | Unique node identifier |
| Lat | double | Latitude |
| Lng | double | Longitude |

#### Edge

| Property | Type | Description |
|----------|------|-------------|
| From | Node | Source node |
| To | Node | Target node |
| Weight | double | Edge weight (distance, time, or cost depending on strategy) |

#### Path (Algorithm Output)

| Property | Type | Description |
|----------|------|-------------|
| Nodes | List\<Node\> | Ordered sequence of nodes |
| TotalWeight | double | Sum of edge weights along path |

---

## 🔬 Algorithm Layer

### Design Philosophy

Algorithms live in their own layer. They know **nothing** about routes, packages, or business rules. They solve **graph problems** — period. Strategies translate between business domain and graph domain.

### Algorithm 1: A* Pathfinder (used by FastestRouteStrategy)

**What it does**: Heuristic search — uses straight-line distance to guide the search toward the goal. Faster than Dijkstra for point-to-point with a good heuristic.

**Why Fastest uses it**: A* explores fewer nodes → computes faster → suitable for time-critical Express routing where we want quick responses.

```
A* Algorithm (pseudocode):

  openSet = priority queue with start node
  gScore[start] = 0
  fScore[start] = heuristic(start, goal)

  while openSet is not empty:
      current = node in openSet with lowest fScore
      if current == goal: return reconstruct_path()

      for each neighbor of current:
          tentative_g = gScore[current] + edge_weight(current, neighbor)
          if tentative_g < gScore[neighbor]:
              cameFrom[neighbor] = current
              gScore[neighbor] = tentative_g
              fScore[neighbor] = tentative_g + heuristic(neighbor, goal)
              add neighbor to openSet

  throw PathNotFoundException
```

**Heuristic**: Euclidean distance (straight-line) between nodes — admissible and consistent, guarantees optimal path.

### Algorithm 2: Dijkstra Pathfinder (used by CheapestRouteStrategy)

**What it does**: Finds the guaranteed shortest path by exploring all directions equally. No heuristic — explores more nodes but never misses the optimal path.

**Why Cheapest uses it**: Dijkstra explores the full neighborhood → finds the true minimum-cost path, even if it's counterintuitive. Perfect for cost optimization where the cheapest route may not be the most direct.

```
Dijkstra Algorithm (pseudocode):

  distances[start] = 0
  distances[all others] = infinity
  priorityQueue = [start]

  while priorityQueue is not empty:
      current = node with smallest distance
      if current == goal: return reconstruct_path()

      for each neighbor of current:
          newDist = distances[current] + edge_weight(current, neighbor)
          if newDist < distances[neighbor]:
              distances[neighbor] = newDist
              previous[neighbor] = current
              add neighbor to priorityQueue

  throw PathNotFoundException
```

### Algorithm 3: Weighted Score Calculator (used by BalancedRouteStrategy)

**What it does**: Runs both A* (for time) and Dijkstra (for cost) on the same graph, then scores each candidate path with a weighted formula.

**Why Balanced uses it**: Business wants "best of both" — this calculator finds the path where `(0.5 × timeScore) + (0.5 × costScore)` is highest.

```
Weighted Score (pseudocode):

  timePath  = aStarPathfinder.FindPath(graph_weighted_by_time)
  costPath  = dijkstraPathfinder.FindPath(graph_weighted_by_cost)
  candidates = [timePath, costPath, ...additional paths]

  for each candidate:
      timeScore = 1.0 / candidate.totalTime
      costScore = 1.0 / candidate.totalCost
      candidate.score = (timeWeight * timeScore) + (costWeight * costScore)

  return candidate with highest score
```

### How Strategy Delegates to Algorithm

```
┌─ FastestRouteStrategy ──────────────────────────────────────────┐
│  Calculate(RouteRequest request):                               │
│                                                                 │
│    1. BUSINESS LOGIC: Build graph from request                  │
│       → Convert locations to nodes                              │
│       → Create edges weighted by TRAVEL TIME (time priority)    │
│       → Apply business rules (weight limits, road types)        │
│                                                                 │
│    2. DELEGATE: Call algorithm                                  │
│       → path = _aStarPathfinder.FindPath(graph, start, end)     │
│                                                                 │
│    3. BUSINESS LOGIC: Convert result back to domain             │
│       → Map nodes back to Locations                             │
│       → Calculate cost based on distance + premium rate         │
│       → Return Route object                                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─ CheapestRouteStrategy ─────────────────────────────────────────┐
│  Calculate(RouteRequest request):                               │
│                                                                 │
│    1. BUSINESS LOGIC: Build graph from request                  │
│       → Same locations, but edges weighted by COST ($$)         │
│       → Avoid toll roads, prefer economy routes                 │
│                                                                 │
│    2. DELEGATE: Call algorithm                                  │
│       → path = _dijkstraPathfinder.FindPath(graph, start, end)  │
│                                                                 │
│    3. BUSINESS LOGIC: Convert result back to domain             │
│       → Calculate cost at economy rate                          │
│       → Return Route object                                     │
└─────────────────────────────────────────────────────────────────┘
```

> **The pattern is clear**: Strategy owns the *what* (business decisions). Algorithm owns the *how* (computation). Each can be tested alone.

---

## 🛡 Infrastructure Layer

### Decorator Pattern — Cross-Cutting Concerns

Decorators implement `IRouteStrategy` and wrap an inner `IRouteStrategy`. They add behavior **around** the call without the inner strategy knowing.

#### CachingRouteDecorator

```
Class: CachingRouteDecorator : IRouteStrategy
Purpose: Cache route results, skip computation on cache hit

┌──────────────────────────────────────────────────────────────┐
│  Calculate(RouteRequest request):                            │
│                                                              │
│    cacheKey = GenerateKey(request)                           │
│                                                              │
│    if cache.TryGet(cacheKey, out cached):                    │
│        return cached          // ← skip inner strategy!      │
│                                                              │
│    result = _innerStrategy.Calculate(request)  // ← delegate │
│    cache.Set(cacheKey, result, 5 minutes)                    │
│    return result                                             │
└──────────────────────────────────────────────────────────────┘
```

#### LoggingRouteDecorator

```
Class: LoggingRouteDecorator : IRouteStrategy
Purpose: Log strategy execution for observability

┌──────────────────────────────────────────────────────────────┐
│  Calculate(RouteRequest request):                            │
│                                                              │
│    log($"Starting {_innerStrategy.Name}...")                 │
│    stopwatch.Start()                                         │
│                                                              │
│    result = _innerStrategy.Calculate(request)  // ← delegate │
│                                                              │
│    stopwatch.Stop()                                          │
│    log($"Completed in {stopwatch.ElapsedMilliseconds}ms")    │
│    return result                                             │
└──────────────────────────────────────────────────────────────┘
```

#### ValidationRouteDecorator

```
Class: ValidationRouteDecorator : IRouteStrategy
Purpose: Validate input before delegating to inner strategy

┌──────────────────────────────────────────────────────────────┐
│  Calculate(RouteRequest request):                            │
│                                                              │
│    if request is null:     throw ArgumentNullException       │
│    if request.From is null: throw ArgumentException          │
│    if request.To is null:   throw ArgumentException          │
│    if request.Package.Weight <= 0: throw ArgumentException   │
│                                                              │
│    return _innerStrategy.Calculate(request)  // ← delegate   │
└──────────────────────────────────────────────────────────────┘
```

#### Composition Example

```
// How decorators compose in Program.cs (composition root):

IPathfinder pathfinder = new AStarPathfinder();
ICache cache = new InMemoryCache();

// 1. Create the raw strategy
IRouteStrategy fastest = new FastestRouteStrategy(pathfinder);

// 2. Wrap with decorators (inside → outside)
IRouteStrategy decorated =
    new ValidationRouteDecorator(
        new CachingRouteDecorator(
            new LoggingRouteDecorator(fastest),
            cache));

// 3. Use it — caller has no idea about decoration layers
calculator.SetStrategy(decorated);
var route = calculator.CalculateRoute(request);
```

### Map Service Abstraction

```
// Strategy DOESN'T know about HTTP, API keys, or JSON.
// It depends on IMapService — which can be mocked for tests.

IMapService mapService = new GoogleMapsService(apiKey);
// OR for testing / demo:
IMapService mapService = new StubMapService();  // returns fake data
```

---

## ✅ Acceptance Criteria

### Feature 1: Strategy + Algorithm Delegation

| ID | Criteria | Test Type |
|----|----------|-----------|
| AC-01 | `RouteCalculator.SetStrategy()` swaps the active algorithm | Unit test |
| AC-02 | `CalculateRoute()` delegates to current strategy | Unit test |
| AC-03 | `FastestRouteStrategy` uses `AStarPathfinder` (injected) | Unit test (mock pathfinder) |
| AC-04 | `CheapestRouteStrategy` uses `DijkstraPathfinder` (injected) | Unit test (mock pathfinder) |
| AC-05 | Strategies contain **zero** pathfinding math | Code review |
| AC-06 | Algorithms contain **zero** business logic | Code review |
| AC-07 | `FastestRouteStrategy` returns lowest time of all strategies | Integration test |
| AC-08 | `CheapestRouteStrategy` returns lowest cost of all strategies | Integration test |
| AC-09 | Adding a new strategy requires ZERO changes to `RouteCalculator` | Design review |

### Feature 2: Decorator Pattern

| ID | Criteria | Test Type |
|----|----------|-----------|
| AC-10 | `CachingRouteDecorator` returns cached result on second call | Unit test |
| AC-11 | `CachingRouteDecorator` calls inner strategy only on cache miss | Unit test (verify mock) |
| AC-12 | `LoggingRouteDecorator` logs execution time | Unit test |
| AC-13 | `ValidationRouteDecorator` throws on null request | Unit test |
| AC-14 | Decorators compose in any order | Integration test |
| AC-15 | Inner strategy is unaware of decoration | Design review |

### Feature 3: Factory Pattern

| ID | Criteria | Test Type |
|----|----------|-----------|
| AC-16 | `RouteStrategyFactory.Create("fastest")` returns correct strategy | Unit test |
| AC-17 | Factory returns fully decorated strategy stack | Unit test |
| AC-18 | Factory throws on unknown strategy name | Unit test |

### Non-Functional

| ID | Criteria | Target |
|----|----------|--------|
| NF-01 | Strategy swap time | < 1ms |
| NF-02 | Route calculation time (without cache) | < 100ms |
| NF-03 | Cache hit response time | < 5ms |
| NF-04 | Code coverage | > 90% on all layers |

---

## 📁 Project Structure

```
src/
├── DT.Express.Routing.Core/              # CORE LAYER (zero dependencies)
│   ├── Strategies/
│   │   ├── IRouteStrategy.cs              # The interface (used by strategies AND decorators)
│   │   ├── FastestRouteStrategy.cs        # Delegates to A* pathfinder
│   │   ├── CheapestRouteStrategy.cs       # Delegates to Dijkstra pathfinder
│   │   └── BalancedRouteStrategy.cs       # Delegates to weighted calculator
│   ├── Models/
│   │   ├── RouteRequest.cs                # Input: from, to, package
│   │   ├── Route.cs                       # Output: path, cost, time
│   │   ├── Location.cs                    # Name + lat/lng
│   │   └── Package.cs                     # Weight + priority enum
│   └── Services/
│       └── RouteCalculator.cs             # Context class
│
├── DT.Express.Routing.Algorithms/         # ALGORITHM LAYER (pure computation)
│   ├── Interfaces/
│   │   ├── IPathfinder.cs                 # Path FindPath(graph, start, end)
│   │   ├── IGraph.cs                      # Graph abstraction
│   │   └── IHeuristic.cs                  # Heuristic function for A*
│   ├── Pathfinding/
│   │   ├── AStarPathfinder.cs             # A* implementation
│   │   └── DijkstraPathfinder.cs          # Dijkstra implementation
│   ├── Scoring/
│   │   └── WeightedScoreCalculator.cs     # Multi-objective scoring
│   └── DataStructures/
│       ├── Graph.cs                       # Adjacency list graph
│       ├── Node.cs                        # Graph node
│       ├── Edge.cs                        # Weighted edge
│       └── Path.cs                        # Algorithm result
│
├── DT.Express.Routing.Infrastructure/     # INFRASTRUCTURE LAYER
│   ├── Decorators/
│   │   ├── CachingRouteDecorator.cs       # Cache wrapper (Decorator Pattern)
│   │   ├── LoggingRouteDecorator.cs       # Logging wrapper
│   │   └── ValidationRouteDecorator.cs    # Validation wrapper
│   ├── Caching/
│   │   ├── ICache.cs                      # Cache abstraction
│   │   └── InMemoryCache.cs              # ConcurrentDictionary impl
│   ├── Maps/
│   │   ├── IMapService.cs                 # External map abstraction
│   │   ├── GoogleMapsService.cs           # Real API (future)
│   │   └── StubMapService.cs             # Fake data for demo/tests
│   └── Factory/
│       └── RouteStrategyFactory.cs        # Creates strategy + decorator stacks
│
└── DT.Express.Routing.Demo/              # DEMO LAYER (composition root)
    └── Program.cs                         # Wires everything, runs demo

tests/
├── DT.Express.Routing.Core.Tests/
│   ├── FastestRouteStrategyTests.cs       # Mock pathfinder, test business logic
│   ├── CheapestRouteStrategyTests.cs
│   ├── BalancedRouteStrategyTests.cs
│   └── RouteCalculatorTests.cs
│
├── DT.Express.Routing.Algorithms.Tests/
│   ├── AStarPathfinderTests.cs            # Pure math tests, no mocking
│   └── DijkstraPathfinderTests.cs
│
└── DT.Express.Routing.Infrastructure.Tests/
    ├── CachingRouteDecoratorTests.cs      # Mock inner strategy + cache
    ├── LoggingRouteDecoratorTests.cs
    └── RouteStrategyFactoryTests.cs
```

### Demo Output (Program.cs)

```
=== Route Strategy Demo: Multi-Pattern Architecture ===

Request: Warehouse A → Customer X | 5kg Express

[1] Strategy Pattern — Interchangeable Behaviors
────────────────────────────────────────────────

  [Fastest Route Strategy] (algorithm: A*)
    Distance: 45 km | Cost: $12.00 | Time: 1.5 hours

  [Cheapest Route Strategy] (algorithm: Dijkstra)
    Distance: 38 km | Cost: $5.50  | Time: 3.2 hours

  [Balanced Route Strategy] (algorithm: Weighted Score)
    Distance: 42 km | Cost: $8.00  | Time: 2.0 hours

  Comparison:
    → Fastest saves 1.7 hours vs Cheapest
    → Cheapest saves $6.50 vs Fastest
    → Balanced is the middle ground

[2] Decorator Pattern — Cross-Cutting Concerns
───────────────────────────────────────────────

  First call:  [CACHE MISS] Calculated in 45ms
  Second call: [CACHE HIT]  Returned in 1ms  ← 45x faster!

  Validation:  null request → ArgumentException ✅

[3] Factory Pattern — Strategy Creation
───────────────────────────────────────

  Factory.Create("fastest")  → FastestRouteStrategy (A*, cached, validated)
  Factory.Create("cheapest") → CheapestRouteStrategy (Dijkstra, cached, validated)
  Factory.Create("unknown")  → StrategyNotFoundException ✅
```

---

## 🎤 Interview Guide

### 2-Minute Walkthrough (Simple)

> _"I built this to demonstrate Strategy Pattern. The key insight is that routing algorithms change (fastest vs cheapest), but the routing process doesn't. So I created an interface `IRouteStrategy` with a `Calculate` method, then implemented three strategy classes. The `RouteCalculator` can switch between them at runtime. This follows Open/Closed Principle — adding a new algorithm means adding a new class, zero changes to existing code."_

### 2-Minute Walkthrough (Senior-Level)

> _"I built a route calculation system demonstrating multiple patterns with clean architecture. The core uses **Strategy Pattern** for different routing behaviors. Each strategy **delegates** to specialized algorithm services — A\* for fastest, Dijkstra for cheapest — keeping concerns separated. I abstracted external map APIs behind `IMapService` for testability, and used **Decorator Pattern** for cross-cutting concerns like caching and logging. A **Factory** creates fully-decorated strategy stacks from configuration. Every layer is independently testable — strategies are tested by mocking the pathfinder, algorithms are tested with pure math, decorators are tested by mocking the inner strategy."_

### Anticipated Questions & Answers

| Question | Answer |
|----------|--------|
| **"Why not put A\* directly in the strategy?"** | SRP violation — strategy handles business logic (what to optimize), algorithm handles computation (how to find path). Separate concerns = separate tests. |
| **"Why Decorator instead of just adding caching to the strategy?"** | Caching is a cross-cutting concern. With Decorator, I can add/remove caching without touching any strategy. Same strategy can be cached or uncached depending on context. |
| **"How would you add a new algorithm?"** | Create a new class implementing `IRouteStrategy`, inject the appropriate pathfinder, register in Factory. Zero changes to existing code. Open/Closed Principle. |
| **"How do you test this?"** | Three levels: (1) Strategy tests mock `IPathfinder` to verify business logic, (2) Algorithm tests use known graphs to verify correctness, (3) Decorator tests mock inner strategy to verify wrapping behavior. |
| **"What SOLID principles does this demonstrate?"** | **S**RP — each class has one job. **O**CP — open for extension, closed for modification. **L**SP — decorators and strategies are interchangeable via interface. **I**SP — lean interfaces (one method each). **D**IP — strategies depend on abstractions, not concrete algorithms. |

### Magic Words ✅

- "Separation of concerns"
- "Dependency inversion"
- "Decorator pattern for cross-cutting concerns"
- "Strategy pattern for interchangeable behaviors"
- "Testability through abstraction"
- "Each layer is independently testable"

### Death Words ❌

- "I implemented A* inside my strategy class"
- "My strategy calls Google Maps API directly"
- "I have 8 features"
- "I modeled Chinese logistics business rules"

---

## 📚 Study Resources

| Resource | What You Get | Time |
|----------|-------------|------|
| [Refactoring Guru: Strategy](https://refactoring.guru/design-patterns/strategy) | Strategy Pattern visual guide | 15 min |
| [Refactoring Guru: Decorator](https://refactoring.guru/design-patterns/decorator) | Decorator Pattern visual guide | 15 min |
| [STRATEGY-PATTERN.md](../design-patterns/STRATEGY-PATTERN.md) | Project-specific pattern doc | 10 min |
| [DECORATOR-PATTERN.md](../design-patterns/DECORATOR-PATTERN.md) | Project-specific pattern doc | 10 min |
| [FACTORY-PATTERN.md](../design-patterns/FACTORY-PATTERN.md) | Project-specific pattern doc | 10 min |
| **Your own code** | The best learning is building | 4-6 hours |

### Interview Prep Checklist

- [ ] Can you explain Strategy Pattern in under 2 minutes?
- [ ] Can you explain why algorithms are separated from strategies (SRP)?
- [ ] Can you draw the multi-pattern diagram from memory?
- [ ] Can you explain Decorator Pattern for caching with a code example?
- [ ] Can you explain why `IMapService` exists (testability, swappability)?
- [ ] Can you name all 5 SOLID principles this demonstrates?
- [ ] Can you explain how to add a new algorithm without changing existing code?
- [ ] Can you describe the 3 testing levels (strategy / algorithm / decorator)?

---

## 🔗 Related Documents

| Document | Relationship |
|----------|-------------|
| [02-MULTI-CARRIER.md](02-MULTI-CARRIER.md) | Carrier assignment uses route output |
| [STRATEGY-PATTERN.md](../design-patterns/STRATEGY-PATTERN.md) | Strategy Pattern reference |
| [DECORATOR-PATTERN.md](../design-patterns/DECORATOR-PATTERN.md) | Decorator Pattern reference |
| [FACTORY-PATTERN.md](../design-patterns/FACTORY-PATTERN.md) | Factory Pattern reference |
| [SHIPMENT-AGGREGATE.md](../data-models/SHIPMENT-AGGREGATE.md) | Data model reference |
| [00-INDEX.md](../00-INDEX.md) | Project index |

---

## 📝 Redesign Changelog

### v3 — Multi-Pattern Architecture (current)

**Trigger**: Senior lead review — _"Keep the complexity, but ARCHITECT it properly."_

**Added** ✅
- Algorithm layer with A*, Dijkstra as separate services (`IPathfinder`)
- Strategies delegate to algorithms via dependency injection (SRP)
- Decorator Pattern for caching, logging, validation (cross-cutting concerns)
- Factory Pattern for strategy creation from config
- `IMapService` abstraction for external map APIs
- `ICache` abstraction with in-memory implementation
- 4-layer architecture (Core → Algorithms → Infrastructure → Demo)
- Multi-pattern collaboration diagram
- Interview guide with simple + senior-level walkthroughs
- Anticipated interview Q&A table
- Three testing levels (strategy / algorithm / decorator)

**Changed** 🔄
- 3 simple features → 3 power features (each demonstrating a pattern)
- Strategies now receive `IPathfinder` via constructor injection
- Acceptance criteria expanded to cover all 3 patterns
- Project structure expanded to multi-project solution

**Kept** ♻️
- Strategy Pattern as the core pattern
- Clean interface (`IRouteStrategy` — one property, one method)
- Lean domain models (RouteRequest, Route, Location, Package)
- Open/Closed Principle emphasis
- Strategy vs. if-else comparison

### v2 — Simplified Design

- Stripped enterprise bloat from v1
- Reduced to 3 features, 1 interface, 4 models

### v1 — Original Enterprise Design

- 2,171 lines, Chinese logistics theme, 8 features, PhD algorithms
- Over-engineered for a learning project

---

*Status: 📐 Design Complete — Ready for Implementation*  
*Next Step: Create solution structure and start with Core layer*
