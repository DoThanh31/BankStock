namespace BankStock.Web.Dtos;

/// <summary>Khoảng thời gian của biểu đồ lịch sử giá: 1D, 1W, 1M, 1Y.</summary>
public enum PriceTimeframe { OneDay, OneWeek, OneMonth, OneYear }

public static class PriceTimeframes
{
    public const string Default = "1M";
    public static readonly string[] Codes = ["1D", "1W", "1M", "1Y"];

    public static bool TryParse(string? value, out PriceTimeframe timeframe)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case "1D": timeframe = PriceTimeframe.OneDay; return true;
            case "1W": timeframe = PriceTimeframe.OneWeek; return true;
            case "1M": timeframe = PriceTimeframe.OneMonth; return true;
            case "1Y": timeframe = PriceTimeframe.OneYear; return true;
            default: timeframe = PriceTimeframe.OneMonth; return false;
        }
    }

    public static string ToCode(this PriceTimeframe timeframe) => timeframe switch
    {
        PriceTimeframe.OneDay => "1D",
        PriceTimeframe.OneWeek => "1W",
        PriceTimeframe.OneYear => "1Y",
        _ => "1M"
    };
}
