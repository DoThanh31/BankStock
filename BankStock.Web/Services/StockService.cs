using System.Globalization;
using BankStock.Web.Data;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class StockService : IStockService
{
    private const int FinancialReportQuarters = 8;

    private readonly AppDbContext _context;

    public StockService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy bảng giá các cổ phiếu ngân hàng kèm giá mới nhất trong 1 truy vấn duy nhất (AsNoTracking, chống N+1).
    /// </summary>
    public async Task<List<StockBoardItemViewModel>> GetTopStockBoardAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = await QueryActiveSnapshots()
            .OrderBy(s => s.StockCode)
            .ToListAsync(cancellationToken);

        return snapshots.Select(s => FillQuote(new StockBoardItemViewModel(), s)).ToList();
    }

    /// <summary>
    /// Thống kê tổng quan thị trường (VN-Bank Index, Tổng KL, số mã Tăng/Giảm/TC/Trần/Sàn).
    /// </summary>
    public async Task<MarketOverviewViewModel> GetMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        var board = await GetTopStockBoardAsync(cancellationToken);

        long totalVolume = board.Sum(b => b.Volume);
        int gainers = board.Count(b => b.Status == StockPriceStatus.Up);
        int losers = board.Count(b => b.Status == StockPriceStatus.Down);
        int unchanged = board.Count(b => b.Status == StockPriceStatus.Ref);
        int ceilings = board.Count(b => b.Status == StockPriceStatus.Ceiling);
        int floors = board.Count(b => b.Status == StockPriceStatus.Floor);

        decimal avgChangePercent = board.Count > 0 ? board.Average(b => b.ChangePercent) : 0;
        decimal baseIndex = 1250.00m;
        decimal indexChange = Math.Round(baseIndex * (avgChangePercent / 100m), 2);
        decimal currentIndex = baseIndex + indexChange;

        return new MarketOverviewViewModel
        {
            BankIndex = currentIndex,
            IndexChange = indexChange,
            IndexChangePercent = Math.Round(avgChangePercent, 2),
            TotalVolume = totalVolume,
            TotalListedStocks = board.Count,
            GainersCount = gainers,
            LosersCount = losers,
            UnchangedCount = unchanged,
            CeilingCount = ceilings,
            FloorCount = floors,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<PaginatedList<StockListItemViewModel>> GetStockListAsync(StockListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();

        var source = ApplySort(ApplyFilters(QueryActiveSnapshots(), query), query.Sort, query.Dir);

        int totalCount = await source.CountAsync(cancellationToken);
        int totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        if (totalPages > 0 && query.Page > totalPages)
        {
            query.Page = totalPages;
        }

        var snapshots = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = snapshots.Select(ToListItem).ToList();
        return new PaginatedList<StockListItemViewModel>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<StockDetailViewModel?> GetStockDetailAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(stockCode);

        var snapshot = await QueryActiveSnapshots()
            .FirstOrDefaultAsync(s => s.StockCode == code, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        var info = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.Id == snapshot.StockId)
            .Select(s => new
            {
                s.FaceValue,
                s.EPS,
                s.NPL,
                s.CharterCapital,
                Founded = s.Bank != null ? s.Bank.Founded : null,
                Headquarters = s.Bank != null ? s.Bank.Headquarters : null,
                Website = s.Bank != null ? s.Bank.Website : null,
                Description = s.Bank != null ? s.Bank.Description : null
            })
            .FirstAsync(cancellationToken);

        var reports = await _context.FinancialReports
            .AsNoTracking()
            .Where(r => r.StockId == snapshot.StockId)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Quarter)
            .Take(FinancialReportQuarters)
            .Select(r => new FinancialReportItemViewModel
            {
                Year = r.Year,
                Quarter = r.Quarter,
                Revenue = r.Revenue,
                NetProfit = r.NetProfit,
                TotalAssets = r.TotalAssets,
                TotalLiabilities = r.TotalLiabilities
            })
            .ToListAsync(cancellationToken);

        return new StockDetailViewModel
        {
            Quote = ToListItem(snapshot),
            LastTradeDate = snapshot.TradeDate,
            FaceValue = info.FaceValue,
            EPS = info.EPS,
            NPL = info.NPL,
            CharterCapital = info.CharterCapital,
            Founded = info.Founded,
            Headquarters = info.Headquarters,
            Website = info.Website,
            Description = info.Description,
            FinancialReports = reports
        };
    }

    public async Task<StockHistoryDto?> GetPriceHistoryAsync(string stockCode, PriceTimeframe timeframe, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(stockCode);

        var stockId = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.StockCode == code && s.IsActive)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (stockId is null)
        {
            return null;
        }

        var history = _context.PriceHistories.AsNoTracking().Where(p => p.StockId == stockId);

        // Mốc thời gian tính theo phiên giao dịch gần nhất có dữ liệu, không theo ngày hiện tại.
        var latestDate = await history.MaxAsync(p => (DateTime?)p.TradeDate, cancellationToken);
        if (latestDate is null)
        {
            return new StockHistoryDto(code, timeframe.ToCode(), []);
        }

        var latest = latestDate.Value;
        var range = timeframe switch
        {
            PriceTimeframe.OneDay => history.Where(p => p.TradeDate == latest),
            PriceTimeframe.OneWeek => history.Where(p => p.TradeDate > latest.AddDays(-7)),
            PriceTimeframe.OneYear => history.Where(p => p.TradeDate > latest.AddYears(-1)),
            _ => history.Where(p => p.TradeDate > latest.AddMonths(-1))
        };

        var rows = await range
            .OrderBy(p => p.TradeDate)
            .Select(p => new { p.TradeDate, p.Open, p.High, p.Low, p.Close, p.Volume })
            .ToListAsync(cancellationToken);

        var candles = rows
            .Select(p => new PriceCandleDto(
                p.TradeDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                p.Open, p.High, p.Low, p.Close, p.Volume))
            .ToList();

        return new StockHistoryDto(code, timeframe.ToCode(), candles);
    }

    public async Task<List<StockBoardItemViewModel>> GetBoardItemsByCodesAsync(IReadOnlyList<string> stockCodes, CancellationToken cancellationToken = default)
    {
        if (stockCodes.Count == 0)
        {
            return new List<StockBoardItemViewModel>();
        }

        var codes = stockCodes.Select(NormalizeCode).Distinct().ToList();
        var snapshots = await QueryActiveSnapshots()
            .Where(s => codes.Contains(s.StockCode))
            .ToListAsync(cancellationToken);

        return snapshots
            .OrderBy(s => codes.IndexOf(s.StockCode))
            .Select(s => FillQuote(new StockBoardItemViewModel(), s))
            .ToList();
    }

    /// <summary>
    /// Mỗi mã đang giao dịch kèm phiên giá mới nhất. EF Core dịch "let latest" thành một phép join
    /// (ROW_NUMBER theo StockId) nên lọc/sắp xếp theo giá vẫn chạy trong SQL.
    /// </summary>
    private IQueryable<StockSnapshot> QueryActiveSnapshots() =>
        from s in _context.Stocks.AsNoTracking()
        where s.IsActive
        let latest = s.PriceHistories
            .OrderByDescending(p => p.TradeDate)
            .ThenByDescending(p => p.Id)
            .FirstOrDefault()
        select new StockSnapshot
        {
            StockId = s.Id,
            StockCode = s.StockCode,
            BankName = s.Bank != null ? s.Bank.Name : string.Empty,
            BankShortName = s.Bank != null ? s.Bank.ShortName : string.Empty,
            Exchange = s.Exchange,
            BankGroup = s.BankGroup,
            ListedShares = s.ListedShares,
            PE = s.PE,
            PB = s.PB,
            CASA = s.CASA,
            DividendYield = s.DividendYield,
            TradeDate = latest != null ? latest.TradeDate : null,
            RefPrice = latest != null ? latest.RefPrice : 0m,
            CeilingPrice = latest != null ? latest.CeilingPrice : 0m,
            FloorPrice = latest != null ? latest.FloorPrice : 0m,
            Close = latest != null ? latest.Close : 0m,
            High = latest != null ? latest.High : 0m,
            Low = latest != null ? latest.Low : 0m,
            Volume = latest != null ? latest.Volume : 0L
        };

    private static IQueryable<StockSnapshot> ApplyFilters(IQueryable<StockSnapshot> source, StockListQuery query)
    {
        if (!string.IsNullOrEmpty(query.Q))
        {
            var keyword = query.Q;
            source = source.Where(s => s.StockCode.Contains(keyword)
                || s.BankName.Contains(keyword)
                || s.BankShortName.Contains(keyword));
        }

        if (query.Group.HasValue)
        {
            var group = query.Group.Value;
            source = source.Where(s => s.BankGroup == group);
        }

        if (query.PeUnder10)
        {
            source = source.Where(s => s.PE != null && s.PE < 10m);
        }

        if (query.CasaOver30)
        {
            source = source.Where(s => s.CASA != null && s.CASA > 30m);
        }

        if (query.MinDividend.HasValue)
        {
            var minDividend = query.MinDividend.Value;
            source = source.Where(s => s.DividendYield != null && s.DividendYield >= minDividend);
        }

        return source;
    }

    private static IQueryable<StockSnapshot> ApplySort(IQueryable<StockSnapshot> source, StockSortField sort, SortDirection dir)
    {
        bool desc = dir == SortDirection.Desc;

        IOrderedQueryable<StockSnapshot> ordered = sort switch
        {
            StockSortField.Price => desc ? source.OrderByDescending(s => s.Close) : source.OrderBy(s => s.Close),
            StockSortField.Change => desc
                ? source.OrderByDescending(s => s.RefPrice == 0m ? 0m : (s.Close - s.RefPrice) / s.RefPrice)
                : source.OrderBy(s => s.RefPrice == 0m ? 0m : (s.Close - s.RefPrice) / s.RefPrice),
            StockSortField.Volume => desc ? source.OrderByDescending(s => s.Volume) : source.OrderBy(s => s.Volume),
            StockSortField.PE => desc ? source.OrderByDescending(s => s.PE) : source.OrderBy(s => s.PE),
            StockSortField.CASA => desc ? source.OrderByDescending(s => s.CASA) : source.OrderBy(s => s.CASA),
            StockSortField.Dividend => desc ? source.OrderByDescending(s => s.DividendYield) : source.OrderBy(s => s.DividendYield),
            StockSortField.MarketCap => desc
                ? source.OrderByDescending(s => (decimal)s.ListedShares * s.Close)
                : source.OrderBy(s => (decimal)s.ListedShares * s.Close),
            _ => desc ? source.OrderByDescending(s => s.StockCode) : source.OrderBy(s => s.StockCode)
        };

        // Khóa phụ để phân trang ổn định khi giá trị cột sắp xếp bằng nhau.
        return sort == StockSortField.Code ? ordered : ordered.ThenBy(s => s.StockCode);
    }

    private static StockListItemViewModel ToListItem(StockSnapshot snapshot)
    {
        var item = new StockListItemViewModel
        {
            PE = snapshot.PE,
            PB = snapshot.PB,
            CASA = snapshot.CASA,
            DividendYield = snapshot.DividendYield,
            ListedShares = snapshot.ListedShares
        };
        FillQuote(item, snapshot);
        return item;
    }

    private static T FillQuote<T>(T target, StockSnapshot s) where T : StockBoardItemViewModel
    {
        decimal refPrice = s.RefPrice;
        decimal ceilingPrice = s.CeilingPrice > 0 ? s.CeilingPrice : Math.Round(refPrice * 1.07m, 0);
        decimal floorPrice = s.FloorPrice > 0 ? s.FloorPrice : Math.Round(refPrice * 0.93m, 0);
        decimal currentPrice = s.TradeDate.HasValue ? s.Close : refPrice;
        decimal change = currentPrice - refPrice;

        target.StockId = s.StockId;
        target.StockCode = s.StockCode;
        target.BankName = s.BankName;
        target.BankShortName = s.BankShortName;
        target.Exchange = s.Exchange;
        target.BankGroup = s.BankGroup;
        target.RefPrice = refPrice;
        target.CeilingPrice = ceilingPrice;
        target.FloorPrice = floorPrice;
        target.CurrentPrice = currentPrice;
        target.Change = change;
        target.ChangePercent = refPrice > 0 ? Math.Round(change / refPrice * 100, 2) : 0;
        target.Volume = s.Volume;
        target.High = s.TradeDate.HasValue ? s.High : currentPrice;
        target.Low = s.TradeDate.HasValue ? s.Low : currentPrice;
        target.Status = GetStatus(currentPrice, refPrice, ceilingPrice, floorPrice);
        return target;
    }

    /// <summary>
    /// Trạng thái màu theo quy ước chứng khoán Việt Nam:
    /// Trần: Tím | Sàn: Xanh lơ | Tăng: Xanh lá | Giảm: Đỏ | Tham chiếu: Vàng
    /// </summary>
    private static StockPriceStatus GetStatus(decimal currentPrice, decimal refPrice, decimal ceilingPrice, decimal floorPrice)
    {
        if (ceilingPrice > 0 && currentPrice >= ceilingPrice) return StockPriceStatus.Ceiling;
        if (floorPrice > 0 && currentPrice <= floorPrice) return StockPriceStatus.Floor;
        if (currentPrice > refPrice) return StockPriceStatus.Up;
        if (currentPrice < refPrice) return StockPriceStatus.Down;
        return StockPriceStatus.Ref;
    }

    private static string NormalizeCode(string stockCode) => stockCode.Trim().ToUpperInvariant();

    private sealed class StockSnapshot
    {
        public int StockId { get; init; }
        public string StockCode { get; init; } = string.Empty;
        public string BankName { get; init; } = string.Empty;
        public string BankShortName { get; init; } = string.Empty;
        public string Exchange { get; init; } = string.Empty;
        public BankGroup BankGroup { get; init; }
        public long ListedShares { get; init; }
        public decimal? PE { get; init; }
        public decimal? PB { get; init; }
        public decimal? CASA { get; init; }
        public decimal? DividendYield { get; init; }
        public DateTime? TradeDate { get; init; }
        public decimal RefPrice { get; init; }
        public decimal CeilingPrice { get; init; }
        public decimal FloorPrice { get; init; }
        public decimal Close { get; init; }
        public decimal High { get; init; }
        public decimal Low { get; init; }
        public long Volume { get; init; }
    }
}
