namespace BankStock.Web.Services;

public class ListQueryStateService : IListQueryStateService
{
    private const string KeyPrefix = "ListQuery:";

    /// <summary>Query string dài bất thường không lưu, tránh làm phình Session.</summary>
    public const int MaxQueryLength = 1024;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ListQueryStateService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    public string? Get(string listKey)
    {
        var value = Session?.GetString(KeyPrefix + listKey);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public void Save(string listKey, string queryString)
    {
        if (Session is not { } session)
        {
            return;
        }

        if (string.IsNullOrEmpty(queryString) || queryString.Length > MaxQueryLength || !queryString.StartsWith('?'))
        {
            session.Remove(KeyPrefix + listKey);
            return;
        }

        session.SetString(KeyPrefix + listKey, queryString);
    }

    public void Clear(string listKey) => Session?.Remove(KeyPrefix + listKey);
}
