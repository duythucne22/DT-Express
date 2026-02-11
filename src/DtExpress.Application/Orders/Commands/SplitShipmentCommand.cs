using DtExpress.Application.Common;

namespace DtExpress.Application.Orders.Commands;

/// <summary>
/// Split an existing order into multiple shipments based on item groupings.
/// The original order is cancelled and new orders are created per group.
/// Only valid for orders in Created or Confirmed state.
/// </summary>
public sealed record SplitShipmentCommand(
    Guid OrderId,
    IReadOnlyList<IReadOnlyList<int>> ItemGroups) : ICommand<SplitShipmentResult>;

/// <summary>Result of a split shipment operation.</summary>
public sealed record SplitShipmentResult(
    Guid OriginalOrderId,
    IReadOnlyList<SplitOrderInfo> NewOrders);

/// <summary>Info about one of the new orders created from the split.</summary>
public sealed record SplitOrderInfo(
    Guid OrderId,
    string OrderNumber,
    int ItemCount);
