using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using BankStock.Web.ViewModels.Investor;

namespace BankStock.Web.Services;

/// <summary>Sổ lệnh toàn hệ thống cho Admin (chỉ đọc).</summary>
public interface IOrderAdminService
{
    /// <summary>Lọc theo mã, trạng thái, loại lệnh; ô tìm kiếm khớp email người đặt hoặc mã CK.</summary>
    Task<PaginatedList<AdminOrderListItem>> GetPagedAsync(OrderListQuery query, CancellationToken cancellationToken = default);
}
