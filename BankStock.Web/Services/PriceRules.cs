namespace BankStock.Web.Services;

/// <summary>
/// Quy tắc giá theo sàn Việt Nam: biên độ dao động và bước giá.
/// </summary>
public static class PriceRules
{
    public static readonly string[] Exchanges = ["HOSE", "HNX", "UPCOM"];

    /// <summary>Biên độ dao động: HOSE 7%, HNX 10%, UPCOM 15%.</summary>
    public static decimal GetBandPercent(string exchange) => exchange.ToUpperInvariant() switch
    {
        "HNX" => 0.10m,
        "UPCOM" => 0.15m,
        _ => 0.07m
    };

    /// <summary>Bước giá HOSE: &lt;10.000 → 10đ; 10.000–49.950 → 50đ; ≥50.000 → 100đ. HNX/UPCOM: 100đ.</summary>
    public static decimal GetTickSize(string exchange, decimal price)
    {
        if (!string.Equals(exchange, "HOSE", StringComparison.OrdinalIgnoreCase))
        {
            return 100m;
        }

        return price switch
        {
            < 10_000m => 10m,
            < 50_000m => 50m,
            _ => 100m
        };
    }

    /// <summary>Giá trần làm tròn xuống, giá sàn làm tròn lên theo bước giá để không vượt biên độ.</summary>
    public static (decimal Ceiling, decimal Floor) CalculateBand(string exchange, decimal refPrice)
    {
        var band = GetBandPercent(exchange);
        var rawCeiling = refPrice * (1 + band);
        var rawFloor = refPrice * (1 - band);

        var ceilingTick = GetTickSize(exchange, rawCeiling);
        var floorTick = GetTickSize(exchange, rawFloor);

        var ceiling = Math.Floor(rawCeiling / ceilingTick) * ceilingTick;
        var floor = Math.Ceiling(rawFloor / floorTick) * floorTick;
        return (ceiling, floor);
    }
}
