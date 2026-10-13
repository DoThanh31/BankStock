using BankStock.Web.Models;

namespace BankStock.Web.Dtos;

/// <summary>Kết quả đặt lệnh: trạng thái (khớp ngay hay chờ), giá khớp, phí và số dư ví sau lệnh.</summary>
public record OrderPlacedDto(
    int OrderId,
    string StockCode,
    OrderType Type,
    OrderMode Mode,
    OrderStatus Status,
    int Quantity,
    decimal Price,
    decimal? MatchedPrice,
    decimal Fee,
    WalletSummaryDto Wallet);
