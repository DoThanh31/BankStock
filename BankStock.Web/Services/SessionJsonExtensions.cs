using System.Text.Json;

namespace BankStock.Web.Services;

/// <summary>Lưu / đọc object dạng JSON trong Session (Session chỉ hỗ trợ sẵn string, int, byte[]).</summary>
public static class SessionJsonExtensions
{
    public static T? GetJson<T>(this ISession session, string key, ILogger? logger = null)
    {
        var json = session.GetString(key);
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException ex)
        {
            // Dữ liệu session hỏng (đổi cấu trúc giữa các phiên bản...) thì bỏ đi, coi như chưa có.
            logger?.LogWarning(ex, "Invalid {SessionKey} payload in session, resetting.", key);
            session.Remove(key);
            return default;
        }
    }

    public static void SetJson<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));
}
