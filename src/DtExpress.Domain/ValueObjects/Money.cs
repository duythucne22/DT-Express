namespace DtExpress.Domain.ValueObjects;

/// <summary>
/// Monetary amount with currency. Immutable value object.
/// Supports CNY and USD. All arithmetic enforces same-currency rule.
/// </summary>
public sealed record Money
{
    /// <summary>Supported currency codes.</summary>
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "CNY", "USD"
    };

    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        var normalized = currency.ToUpperInvariant();
        if (!SupportedCurrencies.Contains(normalized))
            throw new ArgumentException($"Unsupported currency: '{currency}'. Supported: {string.Join(", ", SupportedCurrencies)}.", nameof(currency));

        Amount = Math.Round(amount, 2);
        Currency = normalized;
    }

    /// <summary>Create a CNY money value.</summary>
    public static Money CNY(decimal amount) => new(amount, "CNY");

    /// <summary>Create a USD money value.</summary>
    public static Money USD(decimal amount) => new(amount, "USD");

    /// <summary>Shorthand for zero CNY.</summary>
    public static Money Zero => new(0m, "CNY");

    /// <summary>Add two amounts. Currencies must match.</summary>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency} — currency mismatch.");
        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>Multiply amount by a factor (e.g., quantity).</summary>
    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative.", nameof(factor));
        return new Money(Amount * factor, Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
