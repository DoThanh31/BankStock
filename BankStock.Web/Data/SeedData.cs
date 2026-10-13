using BankStock.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!await roleManager.RoleExistsAsync("Investor"))
            await roleManager.CreateAsync(new IdentityRole("Investor"));

        if (!context.Users.Any())
        {
            var admin = new ApplicationUser { UserName = "admin@bankstock.local", Email = "admin@bankstock.local", FullName = "System Admin", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "Admin@12345");
            await userManager.AddToRoleAsync(admin, "Admin");

            var inv1 = new ApplicationUser { UserName = "investor1@bankstock.local", Email = "investor1@bankstock.local", FullName = "Investor One", EmailConfirmed = true };
            await userManager.CreateAsync(inv1, "Investor@12345");
            await userManager.AddToRoleAsync(inv1, "Investor");
            
            var inv2 = new ApplicationUser { UserName = "investor2@bankstock.local", Email = "investor2@bankstock.local", FullName = "Investor Two", EmailConfirmed = true };
            await userManager.CreateAsync(inv2, "Investor@12345");
            await userManager.AddToRoleAsync(inv2, "Investor");

            var wallet1 = new Wallet { UserId = inv1.Id, Balance = 500000000 };
            wallet1.Transactions.Add(new WalletTransaction { Amount = 500000000, BalanceAfter = 500000000, Type = WalletTransactionType.InitialGrant, Description = "Khởi tạo tài khoản" });
            context.Wallets.Add(wallet1);

            var wallet2 = new Wallet { UserId = inv2.Id, Balance = 500000000 };
            wallet2.Transactions.Add(new WalletTransaction { Amount = 500000000, BalanceAfter = 500000000, Type = WalletTransactionType.InitialGrant, Description = "Khởi tạo tài khoản" });
            context.Wallets.Add(wallet2);

            await context.SaveChangesAsync();
        }

        if (!context.Banks.Any())
        {
            var banks = new List<Bank>
            {
                new Bank { Code = "VCB", Name = "NHTMCP Ngoại thương VN", ShortName = "Vietcombank", Founded = 1963 },
                new Bank { Code = "BID", Name = "NHTMCP Đầu tư và Phát triển VN", ShortName = "BIDV", Founded = 1957 },
                new Bank { Code = "CTG", Name = "NHTMCP Công thương VN", ShortName = "VietinBank", Founded = 1988 },
                new Bank { Code = "TCB", Name = "NHTMCP Kỹ thương VN", ShortName = "Techcombank", Founded = 1993 },
                new Bank { Code = "MBB", Name = "NHTMCP Quân đội", ShortName = "MBBank", Founded = 1994 },
                new Bank { Code = "VPB", Name = "NHTMCP Việt Nam Thịnh Vượng", ShortName = "VPBank", Founded = 1993 },
                new Bank { Code = "ACB", Name = "NHTMCP Á Châu", ShortName = "ACB", Founded = 1993 },
                new Bank { Code = "STB", Name = "NHTMCP Sài Gòn Thương Tín", ShortName = "Sacombank", Founded = 1991 },
                new Bank { Code = "HDB", Name = "NHTMCP Phát triển TP.HCM", ShortName = "HDBank", Founded = 1990 },
                new Bank { Code = "TPB", Name = "NHTMCP Tiên Phong", ShortName = "TPBank", Founded = 2008 }
            };
            context.Banks.AddRange(banks);
            await context.SaveChangesAsync();

            var stocks = new List<Stock>
            {
                new Stock { StockCode = "VCB", BankId = banks[0].Id, Exchange = "HOSE", BankGroup = BankGroup.Big4, FaceValue = 10000, PE = 15.2m, PB = 3.1m },
                new Stock { StockCode = "BID", BankId = banks[1].Id, Exchange = "HOSE", BankGroup = BankGroup.Big4, FaceValue = 10000, PE = 13.5m, PB = 2.4m },
                new Stock { StockCode = "CTG", BankId = banks[2].Id, Exchange = "HOSE", BankGroup = BankGroup.Big4, FaceValue = 10000, PE = 12.0m, PB = 1.8m },
                new Stock { StockCode = "TCB", BankId = banks[3].Id, Exchange = "HOSE", BankGroup = BankGroup.PrivateJSC, FaceValue = 10000, PE = 9.8m, PB = 1.2m },
                new Stock { StockCode = "MBB", BankId = banks[4].Id, Exchange = "HOSE", BankGroup = BankGroup.PrivateJSC, FaceValue = 10000, PE = 8.5m, PB = 1.3m },
                new Stock { StockCode = "VPB", BankId = banks[5].Id, Exchange = "HOSE", BankGroup = BankGroup.PrivateJSC, FaceValue = 10000, PE = 10.2m, PB = 1.1m },
                new Stock { StockCode = "ACB", BankId = banks[6].Id, Exchange = "HOSE", BankGroup = BankGroup.PrivateJSC, FaceValue = 10000, PE = 8.9m, PB = 1.5m },
                new Stock { StockCode = "STB", BankId = banks[7].Id, Exchange = "HOSE", BankGroup = BankGroup.PrivateJSC, FaceValue = 10000, PE = 11.4m, PB = 1.4m },
                new Stock { StockCode = "HDB", BankId = banks[8].Id, Exchange = "HOSE", BankGroup = BankGroup.PrivateJSC, FaceValue = 10000, PE = 9.5m, PB = 1.3m },
                new Stock { StockCode = "TPB", BankId = banks[9].Id, Exchange = "HOSE", BankGroup = BankGroup.PrivateJSC, FaceValue = 10000, PE = 8.2m, PB = 1.1m }
            };
            context.Stocks.AddRange(stocks);
            await context.SaveChangesAsync();

            var random = new Random();
            var priceHistories = new List<PriceHistory>();
            var startDate = DateTime.UtcNow.Date.AddDays(-60);
            
            foreach (var stock in stocks)
            {
                decimal currentPrice = random.Next(15000, 90000);
                for (int i = 0; i < 60; i++)
                {
                    var date = startDate.AddDays(i);
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        continue; 

                    decimal refPrice = currentPrice;
                    decimal ceiling = refPrice * 1.07m;
                    decimal floor = refPrice * 0.93m;

                    decimal open = refPrice + random.Next(-500, 500);
                    if (open > ceiling) open = ceiling;
                    if (open < floor) open = floor;

                    decimal close = open + random.Next(-1000, 1000);
                    if (close > ceiling) close = ceiling;
                    if (close < floor) close = floor;

                    decimal high = Math.Max(open, close) + random.Next(0, 500);
                    if (high > ceiling) high = ceiling;

                    decimal low = Math.Min(open, close) - random.Next(0, 500);
                    if (low < floor) low = floor;

                    priceHistories.Add(new PriceHistory
                    {
                        StockId = stock.Id,
                        TradeDate = date,
                        RefPrice = refPrice,
                        CeilingPrice = ceiling,
                        FloorPrice = floor,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close,
                        Volume = random.Next(1000000, 10000000)
                    });

                    currentPrice = close;
                }
            }
            context.PriceHistories.AddRange(priceHistories);
            await context.SaveChangesAsync();
        }

        await SeedStockMetricsAsync(context);
        await SeedFinancialReportsAsync(context);
    }

    // Chỉ số mô phỏng (gần với số liệu công bố 2024) để phục vụ bộ lọc P/E, CASA, cổ tức và vốn hóa.
    private static readonly Dictionary<string, (long ListedShares, decimal Eps, decimal Casa, decimal Npl, decimal Dividend, decimal TotalAssets)> StockMetrics = new()
    {
        ["VCB"] = (5_589_091_262, 5_960m, 33.5m, 1.0m, 0.7m, 2_000_000_000_000_000m),
        ["BID"] = (6_830_000_000, 3_700m, 18.5m, 1.4m, 1.0m, 2_800_000_000_000_000m),
        ["CTG"] = (5_369_991_748, 4_400m, 23.5m, 1.2m, 1.4m, 2_400_000_000_000_000m),
        ["TCB"] = (7_064_851_739, 3_100m, 37.0m, 1.2m, 3.9m, 1_000_000_000_000_000m),
        ["MBB"] = (6_102_272_659, 3_800m, 39.5m, 1.6m, 2.1m, 1_100_000_000_000_000m),
        ["VPB"] = (7_933_923_601, 1_900m, 14.0m, 4.8m, 5.2m, 900_000_000_000_000m),
        ["ACB"] = (4_466_657_912, 3_800m, 22.0m, 1.5m, 4.0m, 800_000_000_000_000m),
        ["STB"] = (1_885_215_716, 5_100m, 19.0m, 2.4m, 0.0m, 750_000_000_000_000m),
        ["HDB"] = (3_494_402_538, 3_400m, 11.0m, 1.7m, 4.0m, 650_000_000_000_000m),
        ["TPB"] = (2_641_700_000, 2_300m, 20.0m, 2.0m, 5.5m, 380_000_000_000_000m)
    };

    private static async Task SeedStockMetricsAsync(AppDbContext context)
    {
        var stocks = await context.Stocks.Where(s => s.CASA == null).ToListAsync();
        if (stocks.Count == 0) return;

        foreach (var stock in stocks)
        {
            if (!StockMetrics.TryGetValue(stock.StockCode, out var m)) continue;

            stock.ListedShares = m.ListedShares;
            stock.CharterCapital = m.ListedShares * stock.FaceValue;
            stock.EPS = m.Eps;
            stock.CASA = m.Casa;
            stock.NPL = m.Npl;
            stock.DividendYield = m.Dividend;
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedFinancialReportsAsync(AppDbContext context)
    {
        if (await context.FinancialReports.AnyAsync()) return;

        var stocks = await context.Stocks.Select(s => new { s.Id, s.StockCode }).ToListAsync();
        var random = new Random(2024);
        var today = DateTime.UtcNow;
        // Quý gần nhất đã kết thúc.
        var lastQuarterStart = new DateTime(today.Year, (today.Month - 1) / 3 * 3 + 1, 1).AddMonths(-3);

        foreach (var stock in stocks)
        {
            if (!StockMetrics.TryGetValue(stock.StockCode, out var m)) continue;

            for (int i = 7; i >= 0; i--)
            {
                var period = lastQuarterStart.AddMonths(-3 * i);
                // Tổng tài sản tăng ~2.5%/quý, doanh thu và lợi nhuận dao động ±10%.
                var assets = Math.Round(m.TotalAssets * (decimal)Math.Pow(1.025, -i), 0);
                var noise = 0.9m + (decimal)random.NextDouble() * 0.2m;

                context.FinancialReports.Add(new FinancialReport
                {
                    StockId = stock.Id,
                    Year = period.Year,
                    Quarter = (period.Month - 1) / 3 + 1,
                    TotalAssets = assets,
                    TotalLiabilities = Math.Round(assets * 0.915m, 0),
                    Revenue = Math.Round(assets * 0.011m * noise, 0),
                    NetProfit = Math.Round(assets * 0.0045m * noise, 0)
                });
            }
        }
        await context.SaveChangesAsync();
    }
}
