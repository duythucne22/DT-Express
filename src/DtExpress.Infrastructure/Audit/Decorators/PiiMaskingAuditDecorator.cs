using System.Text.RegularExpressions;
using DtExpress.Domain.Audit.Interfaces;
using DtExpress.Domain.Audit.Models;

namespace DtExpress.Infrastructure.Audit.Decorators;

/// <summary>
/// Decorator Pattern: masks Personally Identifiable Information (PII) in audit
/// records <b>before</b> they reach the inner <see cref="IAuditSink"/>.
/// <para>
/// Chinese PII rules:
/// <list type="bullet">
///   <item>Phone: <c>13812345678</c> → <c>138****5678</c></item>
///   <item>Email: <c>zhang@example.com</c> → <c>z***@example.com</c></item>
///   <item>Address street: masked, province/city preserved</item>
/// </list>
/// </para>
/// </summary>
public sealed class PiiMaskingAuditDecorator : IAuditSink
{
    private readonly IAuditSink _inner;

    /// <summary>Chinese mobile: starts with 1, second digit 3-9, then 9 digits (11 total).</summary>
    private static readonly Regex PhoneRegex = new(
        @"1[3-9]\d{9}",
        RegexOptions.Compiled);

    /// <summary>Basic email pattern: local@domain.</summary>
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    /// <summary>
    /// Payload keys whose string values should have street-level details masked.
    /// Matches keys like "street", "address", "origin", "destination".
    /// </summary>
    private static readonly HashSet<string> AddressKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "street", "address", "origin", "destination"
    };

    public PiiMaskingAuditDecorator(IAuditSink inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public Task AppendAsync(AuditRecord record, CancellationToken ct = default)
    {
        var masked = MaskRecord(record);
        return _inner.AppendAsync(masked, ct);
    }

    // ── Masking Pipeline ─────────────────────────────────────────

    private static AuditRecord MaskRecord(AuditRecord record)
    {
        return record with
        {
            Description = record.Description is not null ? MaskString(record.Description) : null,
            Payload = record.Payload is not null ? MaskPayload(record.Payload) : null,
        };
    }

    /// <summary>Mask phone and email in a plain string.</summary>
    private static string MaskString(string value)
    {
        var result = PhoneRegex.Replace(value, MaskPhone);
        result = EmailRegex.Replace(result, MaskEmail);
        return result;
    }

    /// <summary>
    /// <c>13812345678</c> → <c>138****5678</c> (keep first 3, mask middle 4, keep last 4).
    /// </summary>
    private static string MaskPhone(Match m)
    {
        var digits = m.Value;
        return $"{digits[..3]}****{digits[^4..]}";
    }

    /// <summary>
    /// <c>zhang@example.com</c> → <c>z***@example.com</c> (keep first char of local part).
    /// </summary>
    private static string MaskEmail(Match m)
    {
        var email = m.Value;
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return email;

        return $"{email[0]}***{email[atIndex..]}";
    }

    // ── Payload Dictionary Masking ───────────────────────────────

    private static Dictionary<string, object?> MaskPayload(Dictionary<string, object?> payload)
    {
        var masked = new Dictionary<string, object?>(payload.Count);

        foreach (var (key, value) in payload)
        {
            masked[key] = MaskValue(key, value);
        }

        return masked;
    }

    private static object? MaskValue(string key, object? value) => value switch
    {
        string s when AddressKeys.Contains(key) => MaskAddress(s),
        string s => MaskString(s),
        Dictionary<string, object?> dict => MaskPayload(dict),
        _ => value,
    };

    /// <summary>
    /// Mask street-level address details while preserving city/province.
    /// Keeps the last segment (typically "PostalCode" after comma) and masks the rest
    /// beyond province+city level.
    /// <para>Example: "广东深圳南山区科技路1号, 518000" → "广东深圳***, 518000"</para>
    /// </summary>
    private static string MaskAddress(string address)
    {
        // Also mask any embedded phone/email
        var result = MaskString(address);

        // Split on comma — Chinese format is "ProvinceCity Street, PostalCode"
        var commaIndex = result.IndexOf(',');
        if (commaIndex < 0) commaIndex = result.IndexOf('，'); // Chinese comma

        if (commaIndex <= 0) return result;

        var mainPart = result[..commaIndex];
        var suffix = result[commaIndex..]; // ", 518000"

        // Keep first 4 characters (province + city approximation), mask the rest
        if (mainPart.Length <= 4) return result;

        return $"{mainPart[..4]}***{suffix}";
    }
}
