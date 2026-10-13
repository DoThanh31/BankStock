namespace BankStock.Web.Dtos;

/// <summary>Một cây nến ngày. <c>Time</c> dạng yyyy-MM-dd để Lightweight Charts đọc trực tiếp.</summary>
public record PriceCandleDto(string Time, decimal Open, decimal High, decimal Low, decimal Close, long Volume);
