using BankStock.Web.ViewModels;

namespace BankStock.Web.Services;

public interface IAdminDashboardService
{
    /// <summary>Số liệu Dashboard: thống kê toàn sàn, mã giao dịch nhiều nhất, giao dịch nội bộ, ví, audit gần đây.</summary>
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
