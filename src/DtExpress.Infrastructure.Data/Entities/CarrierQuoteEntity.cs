namespace DtExpress.Infrastructure.Data.Entities;

/// <summary>
/// EF entity mapping to the <c>carrier_quotes</c> table.
/// Quote comparison history from carrier rate requests.
/// </summary>
public sealed class CarrierQuoteEntity
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string CarrierCode { get; set; } = null!;

    // ── Money value object (flattened) ──────────────────────────
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = null!; // CNY, USD

    public int EstimatedDays { get; set; }
    public string ServiceLevel { get; set; } = null!; // Express, Standard, Economy

    public DateTimeOffset QuotedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────
    public OrderEntity? Order { get; set; }
    public CarrierEntity Carrier { get; set; } = null!;
}
