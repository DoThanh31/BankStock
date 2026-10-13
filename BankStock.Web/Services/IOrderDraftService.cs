using BankStock.Web.ViewModels.Investor;

namespace BankStock.Web.Services;

/// <summary>Giỏ lệnh nháp của người dùng, lưu trong Session (mất khi đăng xuất / hết phiên).</summary>
public interface IOrderDraftService
{
    IReadOnlyList<OrderDraftItem> GetAll(string userId);

    ServiceResult Add(string userId, PlaceOrderViewModel order);

    bool Remove(string userId, Guid draftId);

    void Clear(string userId);
}
