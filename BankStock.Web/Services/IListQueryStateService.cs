namespace BankStock.Web.Services;

/// <summary>
/// Bộ lọc / sắp xếp / trang hiện tại của từng trang danh sách, lưu trong Session để
/// rời trang rồi quay lại không mất trạng thái tra cứu.
/// </summary>
public interface IListQueryStateService
{
    /// <summary>Query string đã lưu (dạng "?q=...&amp;page=2") hoặc null.</summary>
    string? Get(string listKey);

    void Save(string listKey, string queryString);

    void Clear(string listKey);
}
