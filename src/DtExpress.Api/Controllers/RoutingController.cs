using DtExpress.Api.Models;
using DtExpress.Api.Models.Routing;
using DtExpress.Application.Routing;
using DtExpress.Domain.Common;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.Routing.Models;
using DtExpress.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

using DomainRoute = DtExpress.Domain.Routing.Models.Route;

namespace DtExpress.Api.Controllers;

/// <summary>
/// Route calculation and strategy comparison endpoints.
/// Delegates to RouteCalculationService and RouteComparisonService (Application layer).
/// </summary>
[ApiController]
[Route("api/routing")]
[Produces("application/json")]
[Tags("Routing")]
public sealed class RoutingController : ControllerBase
{
    private readonly RouteCalculationService _calculationService;
    private readonly RouteComparisonService _comparisonService;
    private readonly ICorrelationIdProvider _correlationId;

    public RoutingController(
        RouteCalculationService calculationService,
        RouteComparisonService comparisonService,
        ICorrelationIdProvider correlationId)
    {
        _calculationService = calculationService;
        _comparisonService = comparisonService;
        _correlationId = correlationId;
    }

    /// <summary>Calculate a route using a specific strategy.</summary>
    /// <remarks>
    /// Strategies: **Fastest** (A* pathfinding), **Cheapest** (Dijkstra by cost), **Balanced** (60% time / 40% cost).
    /// The request specifies origin/destination coordinates, package weight, service level, and strategy name.
    /// </remarks>
    /// <response code="200">Route calculated successfully.</response>
    /// <response code="400">Invalid strategy name or request data.</response>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(ApiResponse<RouteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public IActionResult Calculate([FromBody] CalculateRouteRequest request)
    {
        var routeRequest = MapToRouteRequest(request.Origin, request.Destination,
            request.PackageWeight, request.ServiceLevel);

        var route = _calculationService.Calculate(request.Strategy, routeRequest);

        return Ok(ApiResponse<RouteResponse>.Ok(MapToRouteResponse(route), _correlationId.GetCorrelationId()));
    }

    /// <summary>Compare all registered strategies for the same request.</summary>
    /// <remarks>
    /// Runs Fastest, Cheapest, and Balanced strategies in sequence and returns all results
    /// so the client can compare distance, duration, and cost trade-offs.
    /// </remarks>
    /// <response code="200">Comparison results for all strategies.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RouteResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public IActionResult Compare([FromBody] CompareRoutesRequest request)
    {
        var routeRequest = MapToRouteRequest(request.Origin, request.Destination,
            request.PackageWeight, request.ServiceLevel);

        var routes = _comparisonService.CompareAll(routeRequest);
        var response = routes.Select(MapToRouteResponse).ToList();

        return Ok(ApiResponse<IReadOnlyList<RouteResponse>>.Ok(response, _correlationId.GetCorrelationId()));
    }

    /// <summary>List all available routing strategy names.</summary>
    /// <remarks>Returns the names of all registered strategies for use in the calculate endpoint.</remarks>
    /// <response code="200">List of strategy names.</response>
    [HttpGet("strategies")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
    public IActionResult GetStrategies()
    {
        var strategies = _calculationService.GetAvailableStrategies();
        return Ok(ApiResponse<IReadOnlyList<string>>.Ok(strategies, _correlationId.GetCorrelationId()));
    }

    // ── Mapping helpers ──────────────────────────────────────────

    private static RouteRequest MapToRouteRequest(
        GeoCoordinateDto origin, GeoCoordinateDto destination,
        WeightDto weight, string serviceLevel)
    {
        return new RouteRequest(
            new GeoCoordinate(origin.Latitude, origin.Longitude),
            new GeoCoordinate(destination.Latitude, destination.Longitude),
            new Weight(weight.Value, Enum.Parse<WeightUnit>(weight.Unit, ignoreCase: true)),
            Enum.Parse<ServiceLevel>(serviceLevel, ignoreCase: true));
    }

    private static RouteResponse MapToRouteResponse(DomainRoute route)
    {
        return new RouteResponse
        {
            StrategyUsed = route.StrategyUsed,
            WaypointNodeIds = route.WaypointNodeIds,
            DistanceKm = Math.Round(route.DistanceKm, 1),
            EstimatedDuration = route.EstimatedDuration.ToString(@"hh\:mm\:ss"),
            EstimatedCost = new MoneyDto(route.EstimatedCost.Amount, route.EstimatedCost.Currency)
        };
    }
}
