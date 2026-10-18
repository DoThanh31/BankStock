using BankStock.Web.Data;
using BankStock.Web.Models;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BankStock.Tests.Infrastructure;

/// <summary>
/// CSDL InMemory riêng cho từng test. Service chạy trên một context, còn kiểm tra kết quả dùng context mới
/// (<see cref="CreateContext"/>) để chắc chắn dữ liệu đã thật sự được lưu, không chỉ nằm trong ChangeTracker.
/// InMemory không hỗ trợ transaction nên cảnh báo TransactionIgnored được bỏ qua; tính nguyên tử của
/// transaction trên SQL Server đã được kiểm thử thủ công (xem docs/progress.md).
/// </summary>
public sealed class TestDatabase
{
    public const string InvestorId = "investor-1";
    public const string OtherInvestorId = "investor-2";
    public const string StockCode = "VCB";
    public const string InactiveStockCode = "OLD";
    public const decimal InitialBalance = 500_000_000m;
    public const decimal FeePercentage = 0.15m;

    // Phiên giá mẫu: tham chiếu 90.000, biên độ ±7% (sàn 83.700, trần 96.300), giá khớp 90.000.
    public const decimal RefPrice = 90_000m;
    public const decimal FloorPrice = 83_700m;
    public const decimal CeilingPrice = 96_300m;
    public const decimal MatchPrice = 90_000m;

    private readonly string _name = Guid.NewGuid().ToString();
    // Dùng chung một root (tránh EF tạo service provider mới cho mỗi test); tên CSDL riêng đảm bảo cô lập.
    private static readonly InMemoryDatabaseRoot Root = new();

    public int StockId { get; private set; }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_name, Root)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    public static async Task<TestDatabase> CreateAsync()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();

        context.Users.AddRange(
            new ApplicationUser { Id = InvestorId, UserName = "investor1@test.local", Email = "investor1@test.local", FullName = "Investor One" },
            new ApplicationUser { Id = OtherInvestorId, UserName = "investor2@test.local", Email = "investor2@test.local", FullName = "Investor Two" });
        context.Wallets.AddRange(
            new Wallet { UserId = InvestorId, Balance = InitialBalance },
            new Wallet { UserId = OtherInvestorId, Balance = InitialBalance });

        var bank = new Bank { Code = "VCB", Name = "Ngân hàng TMCP Ngoại thương Việt Nam", ShortName = "Vietcombank" };
        var stock = new Stock { StockCode = StockCode, Bank = bank, Exchange = "HOSE", BankGroup = BankGroup.Big4, FaceValue = 10_000m };
        var inactive = new Stock { StockCode = InactiveStockCode, Bank = bank, Exchange = "HOSE", BankGroup = BankGroup.PrivateJSC, IsActive = false };
        context.Stocks.AddRange(stock, inactive);
        context.PriceHistories.AddRange(
            NewSession(stock, DateTime.UtcNow.Date.AddDays(-1), 88_000m, 88_000m * 0.93m, 88_000m * 1.07m, 89_000m),
            NewSession(stock, DateTime.UtcNow.Date, RefPrice, FloorPrice, CeilingPrice, MatchPrice),
            NewSession(inactive, DateTime.UtcNow.Date, 20_000m, 18_600m, 21_400m, 20_000m));

        await context.SaveChangesAsync();
        database.StockId = stock.Id;
        return database;
    }

    public OrderService CreateOrderService(AppDbContext context) =>
        new(context, CreateAuditLog(context), CreateConfiguration(), NullLogger<OrderService>.Instance);

    public WalletService CreateWalletService(AppDbContext context) =>
        new(context, CreateAuditLog(context), NullLogger<WalletService>.Instance);

    /// <summary>Thêm sẵn cổ phiếu vào danh mục của nhà đầu tư (giả lập đã mua trước đó).</summary>
    public async Task AddHoldingAsync(string userId, int quantity, decimal avgCost)
    {
        await using var context = CreateContext();
        context.Holdings.Add(new Holding { UserId = userId, StockId = StockId, Quantity = quantity, AvgCost = avgCost });
        await context.SaveChangesAsync();
    }

    public async Task UpdateWalletAsync(string userId, Action<Wallet> update)
    {
        await using var context = CreateContext();
        var wallet = await context.Wallets.SingleAsync(w => w.UserId == userId);
        update(wallet);
        await context.SaveChangesAsync();
    }

    public async Task<Wallet> GetWalletAsync(string userId)
    {
        await using var context = CreateContext();
        return await context.Wallets.AsNoTracking().SingleAsync(w => w.UserId == userId);
    }

    public async Task<Holding?> GetHoldingAsync(string userId)
    {
        await using var context = CreateContext();
        return await context.Holdings.AsNoTracking().SingleOrDefaultAsync(h => h.UserId == userId && h.StockId == StockId);
    }

    private static PriceHistory NewSession(Stock stock, DateTime date, decimal refPrice, decimal floor, decimal ceiling, decimal close) => new()
    {
        Stock = stock,
        TradeDate = date,
        RefPrice = refPrice,
        FloorPrice = Math.Round(floor, 0),
        CeilingPrice = Math.Round(ceiling, 0),
        Open = refPrice,
        High = Math.Max(refPrice, close),
        Low = Math.Min(refPrice, close),
        Close = close,
        Volume = 1_000_000
    };

    private static AuditLogService CreateAuditLog(AppDbContext context) => new(context, new HttpContextAccessor());

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TradingFeePercentage"] = FeePercentage.ToString(System.Globalization.CultureInfo.InvariantCulture)
        })
        .Build();
}
