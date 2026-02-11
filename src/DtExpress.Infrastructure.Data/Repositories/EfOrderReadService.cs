using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;
using DtExpress.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DtExpress.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrderReadService"/> (CQRS read side).
/// Projects directly from the database to <see cref="OrderDetail"/> and <see cref="OrderSummary"/>
/// using EF queryable projections — no domain model reconstruction needed.
/// </summary>
public sealed class EfOrderReadService : IOrderReadService
{
    private readonly AppDbContext _db;

    public EfOrderReadService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<OrderDetail?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var entity = await _db.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (entity is null) return null;

        return new OrderDetail(
            Id: entity.Id,
            OrderNumber: entity.OrderNumber,
            CustomerName: entity.CustomerName,
            Origin: FormatAddress(entity.OriginCity, entity.OriginProvince),
            Destination: FormatAddress(entity.DestCity, entity.DestProvince),
            Status: Enum.Parse<OrderStatus>(entity.Status),
            ServiceLevel: Enum.Parse<ServiceLevel>(entity.ServiceLevel),
            TrackingNumber: entity.TrackingNumber,
            CarrierCode: entity.CarrierCode,
            Items: entity.Items.Select(MapItemToDomain).ToList(),
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderSummary>> ListAsync(OrderFilter filter, CancellationToken ct = default)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (filter.Status.HasValue)
        {
            var statusStr = filter.Status.Value.ToString();
            query = query.Where(o => o.Status == statusStr);
        }

        if (filter.ServiceLevel.HasValue)
        {
            var slStr = filter.ServiceLevel.Value.ToString();
            query = query.Where(o => o.ServiceLevel == slStr);
        }

        if (filter.FromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);

        // Materialize to list first, then project (Enum.Parse can't be translated to SQL)
        var entities = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                o.Status,
                o.ServiceLevel,
                o.CreatedAt,
            })
            .ToListAsync(ct);

        return entities
            .Select(o => new OrderSummary(
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                Enum.Parse<OrderStatus>(o.Status),
                Enum.Parse<ServiceLevel>(o.ServiceLevel),
                o.CreatedAt))
            .ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string FormatAddress(string city, string province)
        => $"{city}, {province}";

    private static OrderItem MapItemToDomain(OrderItemEntity e)
    {
        var weight = new Weight(e.WeightValue, Enum.Parse<WeightUnit>(e.WeightUnit));
        Dimension? dimension = e.DimLengthCm.HasValue
            ? new Dimension(e.DimLengthCm.Value, e.DimWidthCm!.Value, e.DimHeightCm!.Value)
            : null;
        return new OrderItem(e.Description, e.Quantity, weight, dimension);
    }
}
