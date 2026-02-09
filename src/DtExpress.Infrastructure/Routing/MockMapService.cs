using DtExpress.Domain.Routing.Interfaces;
using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Infrastructure.Routing;

/// <summary>
/// Mock implementation of <see cref="IMapService"/> that builds a realistic Chinese logistics
/// network graph. Contains 18 predefined cities/hubs with real coordinates.
/// <para>
/// <strong>Deterministic</strong>: Same (origin, destination) always produces the same graph.
/// Uses a fixed-seed <see cref="Random"/> for reproducible edge generation.
/// </para>
/// </summary>
public sealed class MockMapService : IMapService
{
    /// <summary>
    /// Predefined Chinese logistics hub data: ID → (Chinese name, latitude, longitude).
    /// Covers major hubs across eastern, central, and western China.
    /// </summary>
    private static readonly Dictionary<string, (string Name, decimal Lat, decimal Lng)> CityData = new()
    {
        // East China — core logistics corridor
        ["SH-01"]  = ("上海转运中心",  31.2304m, 121.4737m),
        ["NJ-01"]  = ("南京分拨中心",  32.0603m, 118.7969m),
        ["HZ-01"]  = ("杭州集散中心",  30.2741m, 120.1551m),
        ["SZ-02"]  = ("苏州中转站",    31.2990m, 120.5853m),

        // North China
        ["BJ-01"]  = ("北京总仓",      39.9042m, 116.4074m),
        ["TJ-01"]  = ("天津港",        39.3434m, 117.3616m),
        ["SJZ-01"] = ("石家庄中心",    38.0428m, 114.5149m),
        ["JN-01"]  = ("济南节点",      36.6512m, 117.1201m),

        // Central China
        ["WH-01"]  = ("武汉枢纽",      30.5928m, 114.3055m),
        ["ZZ-01"]  = ("郑州中转站",    34.7466m, 113.6253m),
        ["CS-01"]  = ("长沙分拨中心",  28.2282m, 112.9388m),

        // Northeast China
        ["SY-01"]  = ("沈阳枢纽",      41.8057m, 123.4315m),
        ["CC-01"]  = ("长春站点",      43.8171m, 125.3235m),
        ["HEB-01"] = ("哈尔滨中心",    45.8038m, 126.5340m),

        // West / Southwest China
        ["XA-01"]  = ("西安分拨",      34.3416m, 108.9398m),
        ["CD-01"]  = ("成都枢纽",      30.5728m, 104.0668m),
        ["CQ-01"]  = ("重庆中心",      29.5630m, 106.5516m),

        // South China
        ["GZ-01"]  = ("广州总仓",      23.1291m, 113.2644m),
        ["SZ-01"]  = ("深圳站点",      22.5431m, 114.0579m),
    };

    /// <summary>
    /// Predefined realistic connections between logistics hubs.
    /// Each entry: (FromId, ToId) — bidirectional edges will be created.
    /// These reflect actual highway/rail corridors in China.
    /// </summary>
    private static readonly (string From, string To)[] Corridors =
    [
        // East China coastal corridor
        ("SH-01", "NJ-01"),   // Shanghai ↔ Nanjing (~300km)
        ("SH-01", "HZ-01"),   // Shanghai ↔ Hangzhou (~180km)
        ("SH-01", "SZ-02"),   // Shanghai ↔ Suzhou (~100km)
        ("NJ-01", "SZ-02"),   // Nanjing ↔ Suzhou (~220km)
        ("HZ-01", "NJ-01"),   // Hangzhou ↔ Nanjing (~270km)

        // East → Central corridor (Yangtze River)
        ("NJ-01", "WH-01"),   // Nanjing ↔ Wuhan (~530km)
        ("WH-01", "CS-01"),   // Wuhan ↔ Changsha (~350km)
        ("NJ-01", "ZZ-01"),   // Nanjing ↔ Zhengzhou (~590km)

        // North China corridor (Jinghu line)
        ("BJ-01", "TJ-01"),   // Beijing ↔ Tianjin (~120km)
        ("BJ-01", "SJZ-01"),  // Beijing ↔ Shijiazhuang (~280km)
        ("SJZ-01", "ZZ-01"),  // Shijiazhuang ↔ Zhengzhou (~420km)
        ("TJ-01", "JN-01"),   // Tianjin ↔ Jinan (~350km)
        ("JN-01", "NJ-01"),   // Jinan ↔ Nanjing (~620km)
        ("JN-01", "ZZ-01"),   // Jinan ↔ Zhengzhou (~470km)

        // Northeast corridor
        ("BJ-01", "SY-01"),   // Beijing ↔ Shenyang (~690km)
        ("SY-01", "CC-01"),   // Shenyang ↔ Changchun (~300km)
        ("CC-01", "HEB-01"),  // Changchun ↔ Harbin (~240km)
        ("TJ-01", "SY-01"),   // Tianjin ↔ Shenyang (~700km)

        // Central → West corridor
        ("ZZ-01", "XA-01"),   // Zhengzhou ↔ Xi'an (~480km)
        ("XA-01", "CD-01"),   // Xi'an ↔ Chengdu (~700km)
        ("CD-01", "CQ-01"),   // Chengdu ↔ Chongqing (~330km)
        ("WH-01", "CQ-01"),   // Wuhan ↔ Chongqing (~850km)
        ("WH-01", "ZZ-01"),   // Wuhan ↔ Zhengzhou (~520km)

        // South China corridor
        ("CS-01", "GZ-01"),   // Changsha ↔ Guangzhou (~620km)
        ("GZ-01", "SZ-01"),   // Guangzhou ↔ Shenzhen (~110km)
        ("HZ-01", "CS-01"),   // Hangzhou ↔ Changsha (~760km)
        ("WH-01", "GZ-01"),   // Wuhan ↔ Guangzhou (~970km)
    ];

    /// <inheritdoc />
    public Graph BuildGraph(GeoCoordinate origin, GeoCoordinate destination)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);

        // Deterministic seed derived from coordinates (same inputs → same graph)
        var seed = HashCode.Combine(
            origin.Latitude.GetHashCode(), origin.Longitude.GetHashCode(),
            destination.Latitude.GetHashCode(), destination.Longitude.GetHashCode());
        var random = new Random(seed);

        // 1. Create all city nodes
        var nodes = new Dictionary<string, GraphNode>();
        foreach (var (id, (name, lat, lng)) in CityData)
        {
            nodes[id] = new GraphNode(id, name, new GeoCoordinate(lat, lng));
        }

        // 2. Add origin and destination as special nodes
        nodes["ORIGIN"] = new GraphNode("ORIGIN", "起点", origin);
        nodes["DESTINATION"] = new GraphNode("DESTINATION", "终点", destination);

        // 3. Build edges from predefined corridors (bidirectional)
        var edges = new List<GraphEdge>();

        foreach (var (fromId, toId) in Corridors)
        {
            if (!nodes.ContainsKey(fromId) || !nodes.ContainsKey(toId))
                continue;

            // Forward edge
            edges.Add(CreateEdge(nodes[fromId], nodes[toId], random));
            // Reverse edge (slightly different cost/duration for realism)
            edges.Add(CreateEdge(nodes[toId], nodes[fromId], random));
        }

        // 4. Connect ORIGIN to the 3 closest city nodes
        ConnectSpecialNode("ORIGIN", nodes, edges, random, connectionCount: 3);

        // 5. Connect the 3 closest city nodes to DESTINATION
        ConnectSpecialNode("DESTINATION", nodes, edges, random, connectionCount: 3, inbound: true);

        return new Graph(nodes, edges.AsReadOnly());
    }

    /// <summary>
    /// Connect a special node (ORIGIN/DESTINATION) to the N closest city hubs.
    /// </summary>
    private static void ConnectSpecialNode(
        string specialNodeId,
        Dictionary<string, GraphNode> nodes,
        List<GraphEdge> edges,
        Random random,
        int connectionCount,
        bool inbound = false)
    {
        var specialNode = nodes[specialNodeId];

        var closestCities = nodes
            .Where(n => n.Key != "ORIGIN" && n.Key != "DESTINATION")
            .OrderBy(n => specialNode.Coordinate.DistanceToKm(n.Value.Coordinate))
            .Take(connectionCount)
            .Select(n => n.Value)
            .ToList();

        foreach (var city in closestCities)
        {
            if (inbound)
            {
                // City → DESTINATION
                edges.Add(CreateEdge(city, specialNode, random));
                edges.Add(CreateEdge(specialNode, city, random));
            }
            else
            {
                // ORIGIN → City
                edges.Add(CreateEdge(specialNode, city, random));
                edges.Add(CreateEdge(city, specialNode, random));
            }
        }
    }

    /// <summary>
    /// Create a single directed edge between two nodes with realistic logistics metrics.
    /// </summary>
    private static GraphEdge CreateEdge(GraphNode from, GraphNode to, Random random)
    {
        var distance = from.Coordinate.DistanceToKm(to.Coordinate);

        // Duration: 60–80 km/h base speed + 0–2 hours transfer delay
        var avgSpeedKmH = distance > 500m ? 80m : 65m;
        var baseHours = (double)(distance / avgSpeedKmH);
        var transferDelay = random.NextDouble() * 2.0; // 0–2 hours
        var duration = TimeSpan.FromHours(baseHours + transferDelay);

        // Cost: ~1.5 CNY/km ±20% random variation
        var baseRate = 1.5m;
        var variation = 0.8m + (decimal)(random.NextDouble() * 0.4); // 0.8–1.2 multiplier
        var cost = Money.CNY(Math.Round(distance * baseRate * variation, 2));

        return new GraphEdge(from.Id, to.Id, Math.Round(distance, 2), duration, cost);
    }
}
