using DtExpress.Domain.ValueObjects;

namespace DtExpress.Domain.Orders.Models;

/// <summary>
/// A line item within an order — description, quantity, and physical attributes.
/// </summary>
public sealed record OrderItem(
    string Description,
    int Quantity,
    Weight Weight,
    Dimension? Dimensions);
