using BankStock.Web.Models;

namespace BankStock.Web.Dtos;

/// <summary>Yêu cầu đặt lệnh. <c>Price</c> chỉ dùng cho lệnh Limit; lệnh Market khớp theo giá khớp hiện tại.</summary>
public record PlaceOrderCommand(string StockCode, OrderType Type, OrderMode Mode, int Quantity, decimal? Price);
