namespace BankStock.Web.ViewModels;

public class MarketOverviewViewModel
{
    public decimal BankIndex { get; set; }
    public decimal IndexChange { get; set; }
    public decimal IndexChangePercent { get; set; }
    public long TotalVolume { get; set; }

    public int TotalListedStocks { get; set; }
    public int GainersCount { get; set; }
    public int LosersCount { get; set; }
    public int UnchangedCount { get; set; }
    public int CeilingCount { get; set; }
    public int FloorCount { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string IndexChangeDisplay => IndexChange >= 0 
        ? $"+{IndexChange:0.00} (+{IndexChangePercent:0.00}%)" 
        : $"{IndexChange:0.00} ({IndexChangePercent:0.00}%)";

    public string IndexStatusClass => IndexChange > 0 
        ? "stock-up" 
        : (IndexChange < 0 ? "stock-down" : "stock-ref");
}
