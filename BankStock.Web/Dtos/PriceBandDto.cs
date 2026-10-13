namespace BankStock.Web.Dtos;

/// <summary>Giá trần/sàn gợi ý theo biên độ sàn, trả về cho nút "Tính trần/sàn" trong form giá.</summary>
public record PriceBandDto(decimal CeilingPrice, decimal FloorPrice, decimal BandPercent);
