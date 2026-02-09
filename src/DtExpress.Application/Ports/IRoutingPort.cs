using DtExpress.Domain.Routing.Models;

namespace DtExpress.Application.Ports;

/// <summary>Cross-domain port: Orders → Routing. Used by ShipOrderHandler.</summary>
public interface IRoutingPort
{
    Task<Route> CalculateRouteAsync(RouteRequest request, CancellationToken ct = default);
}
