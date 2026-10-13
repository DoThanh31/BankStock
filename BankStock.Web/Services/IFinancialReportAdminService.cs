using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Services;

/// <summary>CRUD báo cáo tài chính quý cho khu Admin.</summary>
public interface IFinancialReportAdminService
{
    Task<PaginatedList<FinancialReportListItem>> GetPagedAsync(FinancialReportListQuery query, CancellationToken cancellationToken = default);
    Task<FinancialReportFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(FinancialReportFormViewModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(FinancialReportFormViewModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
