namespace BankStock.Web.Localization;

/// <summary>
/// Culture trên URL: <c>/vi/...</c> → vi-VN, <c>/en/...</c> → en-US. Không có tiền tố thì dùng cookie / mặc định.
/// </summary>
public static class UrlCulture
{
    public const string DefaultCulture = "vi-VN";

    /// <summary>Khóa HttpContext.Items chứa culture lấy từ URL của request hiện tại.</summary>
    internal const string ItemKey = "BankStock.UrlCulture";

    private static readonly Dictionary<string, string> CultureBySegment = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vi"] = "vi-VN",
        ["en"] = "en-US"
    };

    public static IReadOnlyCollection<string> SupportedCultures => CultureBySegment.Values;

    public static bool TryGetCulture(string? segment, out string culture)
    {
        culture = string.Empty;
        return segment is not null && CultureBySegment.TryGetValue(segment, out culture!);
    }

    /// <summary>Tiền tố URL ("vi"/"en") của một culture được hỗ trợ; null nếu không hỗ trợ.</summary>
    public static string? SegmentOf(string? culture) =>
        CultureBySegment.FirstOrDefault(pair => string.Equals(pair.Value, culture, StringComparison.OrdinalIgnoreCase)).Key;

    /// <summary>Tên chuẩn ("vi-VN"/"en-US") của culture được hỗ trợ (không phân biệt hoa thường); null nếu không hỗ trợ.</summary>
    public static string? Canonical(string? culture) =>
        SupportedCultures.FirstOrDefault(c => string.Equals(c, culture, StringComparison.OrdinalIgnoreCase));

    /// <summary>Culture lấy từ URL của request (null nếu URL không có tiền tố culture).</summary>
    public static string? FromRequest(HttpContext context) => context.Items[ItemKey] as string;
}
