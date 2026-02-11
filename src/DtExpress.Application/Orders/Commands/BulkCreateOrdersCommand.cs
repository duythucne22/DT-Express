using DtExpress.Application.Common;
using DtExpress.Domain.ValueObjects;

namespace DtExpress.Application.Orders.Commands;

/// <summary>
/// Batch-create multiple orders in a single transaction.
/// Validates all orders before processing any (fail-fast).
/// </summary>
public sealed record BulkCreateOrdersCommand(
    IReadOnlyList<BulkOrderItem> Orders) : ICommand<BulkCreateResult>;

/// <summary>Individual order within a bulk create request.</summary>
public sealed record BulkOrderItem(
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    Address Origin,
    Address Destination,
    DtExpress.Domain.Routing.Enums.ServiceLevel ServiceLevel,
    IReadOnlyList<DtExpress.Domain.Orders.Models.OrderItem> Items);

/// <summary>Batch result with per-order success/failure detail.</summary>
public sealed record BulkCreateResult(
    IReadOnlyList<BulkCreateItemResult> Results,
    int SuccessCount,
    int FailureCount);

/// <summary>Result for a single order in the batch.</summary>
public sealed record BulkCreateItemResult(
    int Index,
    bool Success,
    Guid? OrderId,
    string? OrderNumber,
    string? Error);
