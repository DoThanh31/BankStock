namespace BankStock.Web.Services;

public class RecentlyViewedStocksService : IRecentlyViewedStocksService
{
    public const string SessionKey = "RecentlyViewedStocks";
    public const int MaxItems = 5;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RecentlyViewedStocksService> _logger;

    public RecentlyViewedStocksService(IHttpContextAccessor httpContextAccessor, ILogger<RecentlyViewedStocksService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    public IReadOnlyList<string> GetCodes() => Session?.GetJson<List<string>>(SessionKey, _logger) ?? [];

    public void Add(string stockCode)
    {
        var session = Session;
        if (session is null || string.IsNullOrWhiteSpace(stockCode))
        {
            return;
        }

        var code = stockCode.Trim().ToUpperInvariant();
        var codes = GetCodes()
            .Where(c => !string.Equals(c, code, StringComparison.OrdinalIgnoreCase))
            .Prepend(code)
            .Take(MaxItems)
            .ToList();

        session.SetJson(SessionKey, codes);
    }
}
