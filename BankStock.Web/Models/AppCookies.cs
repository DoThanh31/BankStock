namespace BankStock.Web.Models;

/// <summary>
/// Tên và thời hạn các cookie của ứng dụng. Mọi cookie đặt Path "/" để dùng chung giữa URL có và không có
/// tiền tố culture (/vi/..., /en/...).
/// </summary>
public static class AppCookies
{
    public const string Session = ".BankStock.Session";

    /// <summary>Dark/Light mode (theme.js ghi phía client, layout đọc phía server để chống nhấp nháy).</summary>
    public const string Theme = "theme";

    /// <summary>Số dòng/trang người dùng chọn lần gần nhất ở các trang danh sách.</summary>
    public const string PageSize = "bs.pageSize";

    /// <summary>Email đăng nhập được ghi nhớ khi chọn "Ghi nhớ đăng nhập".</summary>
    public const string RememberEmail = "bs.rememberEmail";

    public const string RootPath = "/";

    /// <summary>Phiên đăng nhập thường (không ghi nhớ): hết hạn sau khoảng thời gian không hoạt động này.</summary>
    public static readonly TimeSpan AuthSessionLifetime = TimeSpan.FromHours(2);

    /// <summary>Phiên đăng nhập có "Ghi nhớ đăng nhập": cookie bền vững, ExpiresUtc = lúc đăng nhập + khoảng này.</summary>
    public static readonly TimeSpan RememberMeLifetime = TimeSpan.FromDays(14);

    public static readonly TimeSpan PreferenceLifetime = TimeSpan.FromDays(365);

    /// <summary>Tùy chọn chung cho cookie sở thích (không phải cookie xác thực).</summary>
    public static CookieOptions Preference(HttpRequest request, bool httpOnly = true) => new()
    {
        Path = RootPath,
        Expires = DateTimeOffset.UtcNow.Add(PreferenceLifetime),
        HttpOnly = httpOnly,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Secure = request.IsHttps
    };
}
