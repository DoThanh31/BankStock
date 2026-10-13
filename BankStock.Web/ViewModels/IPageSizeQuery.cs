namespace BankStock.Web.ViewModels;

/// <summary>
/// Query danh sách có ô chọn số dòng/trang; <see cref="Filters.PageSizeCookieFilter"/> dùng để
/// nhớ lựa chọn vào cookie và áp lại khi URL không có <c>pageSize</c>.
/// </summary>
public interface IPageSizeQuery
{
    int PageSize { get; set; }

    /// <summary>Kích thước có trong ô chọn của trang (cookie ngoài danh sách này bị bỏ qua).</summary>
    bool IsSelectablePageSize(int pageSize);
}
