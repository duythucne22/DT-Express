namespace DtExpress.Domain.ValueObjects;

/// <summary>
/// Chinese-format postal address. Immutable value object.
/// Defaults to China ("CN") with 6-digit postal code and province validation.
/// </summary>
public sealed record Address
{
    /// <summary>Valid Chinese provinces, municipalities, and autonomous regions.</summary>
    private static readonly HashSet<string> ValidProvinces = new(StringComparer.OrdinalIgnoreCase)
    {
        // 4 Municipalities
        "北京", "天津", "上海", "重庆",
        "Beijing", "Tianjin", "Shanghai", "Chongqing",
        // 23 Provinces
        "河北", "山西", "辽宁", "吉林", "黑龙江", "江苏", "浙江", "安徽",
        "福建", "江西", "山东", "河南", "湖北", "湖南", "广东", "海南",
        "四川", "贵州", "云南", "陕西", "甘肃", "青海", "台湾",
        "Hebei", "Shanxi", "Liaoning", "Jilin", "Heilongjiang", "Jiangsu", "Zhejiang", "Anhui",
        "Fujian", "Jiangxi", "Shandong", "Henan", "Hubei", "Hunan", "Guangdong", "Hainan",
        "Sichuan", "Guizhou", "Yunnan", "Shaanxi", "Gansu", "Qinghai", "Taiwan",
        // 5 Autonomous regions
        "广西", "内蒙古", "西藏", "宁夏", "新疆",
        "Guangxi", "InnerMongolia", "Tibet", "Ningxia", "Xinjiang",
        // 2 SARs
        "香港", "澳门",
        "HongKong", "Macau"
    };

    public string Street { get; init; }
    public string City { get; init; }
    public string Province { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }

    public Address(string street, string city, string province, string postalCode, string country = "CN")
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required.", nameof(street));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(province))
            throw new ArgumentException("Province is required.", nameof(province));
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("PostalCode is required.", nameof(postalCode));

        if (country.Equals("CN", StringComparison.OrdinalIgnoreCase))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(postalCode, @"^\d{6}$"))
                throw new ArgumentException("Chinese postal code must be exactly 6 digits.", nameof(postalCode));
            if (!ValidProvinces.Contains(province))
                throw new ArgumentException($"Invalid Chinese province: '{province}'.", nameof(province));
        }

        Street = street;
        City = city;
        Province = province;
        PostalCode = postalCode;
        Country = country.ToUpperInvariant();
    }

    /// <summary>Short display: "City, Province PostalCode"</summary>
    public string ToShortString() => $"{City}, {Province} {PostalCode}";

    /// <summary>Full Chinese-format address: "Province City Street, PostalCode"</summary>
    public string ToFullString() => $"{Province}{City}{Street}, {PostalCode}";
}
