using System.Text.RegularExpressions;

namespace DtExpress.Domain.ValueObjects;

/// <summary>
/// Contact information with Chinese mobile phone validation.
/// Phone: 1[3-9]XXXXXXXXX (11-digit Chinese mobile). Email is optional.
/// </summary>
public sealed record ContactInfo
{
    /// <summary>Chinese mobile phone pattern: starts with 1, second digit 3-9, then 9 digits.</summary>
    private static readonly Regex ChinesePhoneRegex = new(@"^1[3-9]\d{9}$", RegexOptions.Compiled);

    /// <summary>Basic email format validation.</summary>
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string Name { get; init; }
    public string Phone { get; init; }
    public string? Email { get; init; }

    public ContactInfo(string name, string phone, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.", nameof(phone));
        if (!ChinesePhoneRegex.IsMatch(phone))
            throw new ArgumentException($"Invalid Chinese mobile number: '{phone}'. Expected format: 1[3-9]XXXXXXXXX.", nameof(phone));
        if (email is not null && !EmailRegex.IsMatch(email))
            throw new ArgumentException($"Invalid email format: '{email}'.", nameof(email));

        Name = name;
        Phone = phone;
        Email = email;
    }

    public override string ToString() => Email is not null
        ? $"{Name} ({Phone}, {Email})"
        : $"{Name} ({Phone})";
}
