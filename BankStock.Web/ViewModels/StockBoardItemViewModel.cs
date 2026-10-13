using BankStock.Web.Models;

namespace BankStock.Web.ViewModels;

public enum StockPriceStatus
{
    Ceiling, // Kịch trần (+7% HOSE) - Tím
    Floor,   // Chạm sàn (-7% HOSE) - Xanh lơ
    Up,      // Tăng giá - Xanh lá
    Down,    // Giảm giá - Đỏ
    Ref      // Tham chiếu - Vàng
}

public class StockBoardItemViewModel
{
    public int StockId { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankShortName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public BankGroup BankGroup { get; set; }

    public decimal RefPrice { get; set; }
    public decimal CeilingPrice { get; set; }
    public decimal FloorPrice { get; set; }
    public decimal CurrentPrice { get; set; }

    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }

    public decimal High { get; set; }
    public decimal Low { get; set; }

    public StockPriceStatus Status { get; set; }

    public string StatusClass => Status switch
    {
        StockPriceStatus.Ceiling => "stock-ceil",
        StockPriceStatus.Floor => "stock-floor",
        StockPriceStatus.Up => "stock-up",
        StockPriceStatus.Down => "stock-down",
        _ => "stock-ref"
    };

    public string ChangeDisplay
    {
        get
        {
            if (Change > 0) return $"+{Change:N0}";
            if (Change < 0) return $"{Change:N0}";
            return "0";
        }
    }

    public string ChangePercentDisplay
    {
        get
        {
            if (ChangePercent > 0) return $"+{ChangePercent:0.00}%";
            if (ChangePercent < 0) return $"{ChangePercent:0.00}%";
            return "0.00%";
        }
    }
}
