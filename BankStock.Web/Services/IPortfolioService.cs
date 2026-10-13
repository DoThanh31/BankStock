using BankStock.Web.Dtos;

namespace BankStock.Web.Services;

public interface IPortfolioService
{
    /// <summary>Tóm tắt tài sản + danh mục của người dùng theo giá thị trường mới nhất; null nếu chưa có ví.</summary>
    Task<PortfolioSummaryDto?> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);
}
