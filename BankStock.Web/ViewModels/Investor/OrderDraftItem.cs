using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Investor;

/// <summary>Một lệnh nháp trong "giỏ lệnh" (Session); chưa giam tiền/cổ phiếu cho tới khi người dùng đặt lệnh thật.</summary>
public record OrderDraftItem(
    Guid Id,
    string StockCode,
    OrderType Type,
    OrderMode Mode,
    int Quantity,
    decimal? Price,
    DateTime CreatedAt);
