using DtExpress.Application.Auth.Services;
using DtExpress.Domain.Orders.Enums;
using DtExpress.Domain.Orders.Interfaces;
using DtExpress.Domain.Orders.Models;
using DtExpress.Domain.Routing.Enums;
using DtExpress.Domain.ValueObjects;
using DtExpress.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DtExpress.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/> (CQRS write side).
/// Maps between the domain <see cref="Order"/> aggregate and <see cref="OrderEntity"/>.
/// Requires a factory to reconstruct the IOrderState from the stored status string.
/// </summary>
public sealed class EfOrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    private readonly Func<OrderStatus, IOrderState> _stateFactory;
    private readonly ICurrentUserService _currentUser;

    /// <param name="db">EF Core database context.</param>
    /// <param name="stateFactory">
    /// Factory that maps an <see cref="OrderStatus"/> enum back to the correct
    /// <see cref="IOrderState"/> implementation. Provided by DI registration.
    /// </param>
    /// <param name="currentUser">Current authenticated user context.</param>
    public EfOrderRepository(
        AppDbContext db,
        Func<OrderStatus, IOrderState> stateFactory,
        ICurrentUserService currentUser)
    {
        _db = db;
        _stateFactory = stateFactory;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var entity = await _db.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return entity is null ? null : MapToDomain(entity);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Order order, CancellationToken ct = default)
    {
        var existing = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id, ct);

        if (existing is null)
        {
            // INSERT: new order
            var entity = MapToEntity(order);
            _db.Orders.Add(entity);

            // Save domain events as order_events
            foreach (var domainEvent in order.DomainEvents)
            {
                _db.OrderEvents.Add(new OrderEventEntity
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PreviousStatus = domainEvent.PreviousStatus.ToString(),
                    NewStatus = domainEvent.NewStatus.ToString(),
                    Action = domainEvent.Action.ToString(),
                    OccurredAt = domainEvent.OccurredAt,
                });
            }
        }
        else
        {
            // UPDATE: existing order — update scalar fields
            existing.CustomerName = order.Customer.Name;
            existing.CustomerPhone = order.Customer.Phone;
            existing.CustomerEmail = order.Customer.Email;

            existing.OriginStreet = order.Origin.Street;
            existing.OriginCity = order.Origin.City;
            existing.OriginProvince = order.Origin.Province;
            existing.OriginPostalCode = order.Origin.PostalCode;
            existing.OriginCountry = order.Origin.Country;

            existing.DestStreet = order.Destination.Street;
            existing.DestCity = order.Destination.City;
            existing.DestProvince = order.Destination.Province;
            existing.DestPostalCode = order.Destination.PostalCode;
            existing.DestCountry = order.Destination.Country;

            existing.ServiceLevel = order.ServiceLevel.ToString();
            existing.Status = order.Status.ToString();
            existing.TrackingNumber = order.TrackingNumber;
            existing.CarrierCode = order.CarrierCode;
            existing.UpdatedAt = order.UpdatedAt;

            // Sync items: remove old, add new (simplest for immutable value-object collections)
            _db.OrderItems.RemoveRange(existing.Items);
            foreach (var item in order.Items)
            {
                _db.OrderItems.Add(MapItemToEntity(order.Id, item));
            }

            // Append new domain events
            foreach (var domainEvent in order.DomainEvents)
            {
                _db.OrderEvents.Add(new OrderEventEntity
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PreviousStatus = domainEvent.PreviousStatus.ToString(),
                    NewStatus = domainEvent.NewStatus.ToString(),
                    Action = domainEvent.Action.ToString(),
                    OccurredAt = domainEvent.OccurredAt,
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Entity → Domain mapping ─────────────────────────────────

    private Order MapToDomain(OrderEntity e)
    {
        var customer = new ContactInfo(e.CustomerName, e.CustomerPhone, e.CustomerEmail);
        var origin = new Address(e.OriginStreet, e.OriginCity, e.OriginProvince, e.OriginPostalCode, e.OriginCountry);
        var destination = new Address(e.DestStreet, e.DestCity, e.DestProvince, e.DestPostalCode, e.DestCountry);

        var items = e.Items.Select(MapItemToDomain).ToList();
        var serviceLevel = Enum.Parse<ServiceLevel>(e.ServiceLevel);
        var status = Enum.Parse<OrderStatus>(e.Status);
        var state = _stateFactory(status);

        var order = new Order(
            id: e.Id,
            orderNumber: e.OrderNumber,
            customer: customer,
            origin: origin,
            destination: destination,
            items: items,
            serviceLevel: serviceLevel,
            initialState: state,
            createdAt: e.CreatedAt);

        // Restore tracking info if present
        if (e.CarrierCode is not null && e.TrackingNumber is not null)
            order.SetTrackingInfo(e.CarrierCode, e.TrackingNumber);

        return order;
    }

    private static OrderItem MapItemToDomain(OrderItemEntity e)
    {
        var weight = new Weight(e.WeightValue, Enum.Parse<WeightUnit>(e.WeightUnit));
        Dimension? dimension = e.DimLengthCm.HasValue
            ? new Dimension(e.DimLengthCm.Value, e.DimWidthCm!.Value, e.DimHeightCm!.Value)
            : null;
        return new OrderItem(e.Description, e.Quantity, weight, dimension);
    }

    // ── Domain → Entity mapping ─────────────────────────────────

    private OrderEntity MapToEntity(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        UserId = _currentUser.UserId,  // Stamped from JWT claims
        CustomerName = o.Customer.Name,
        CustomerPhone = o.Customer.Phone,
        CustomerEmail = o.Customer.Email,
        OriginStreet = o.Origin.Street,
        OriginCity = o.Origin.City,
        OriginProvince = o.Origin.Province,
        OriginPostalCode = o.Origin.PostalCode,
        OriginCountry = o.Origin.Country,
        DestStreet = o.Destination.Street,
        DestCity = o.Destination.City,
        DestProvince = o.Destination.Province,
        DestPostalCode = o.Destination.PostalCode,
        DestCountry = o.Destination.Country,
        ServiceLevel = o.ServiceLevel.ToString(),
        Status = o.Status.ToString(),
        TrackingNumber = o.TrackingNumber,
        CarrierCode = o.CarrierCode,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.Items.Select(i => MapItemToEntity(o.Id, i)).ToList(),
    };

    private static OrderItemEntity MapItemToEntity(Guid orderId, OrderItem item) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        Description = item.Description,
        Quantity = item.Quantity,
        WeightValue = item.Weight.Value,
        WeightUnit = item.Weight.Unit.ToString(),
        DimLengthCm = item.Dimensions?.LengthCm,
        DimWidthCm = item.Dimensions?.WidthCm,
        DimHeightCm = item.Dimensions?.HeightCm,
    };
}
