using BankStock.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

/// <summary>
/// Thử lại một thao tác ghi khi Wallet/Holding bị cập nhật đồng thời (RowVersion → DbUpdateConcurrencyException).
/// Mỗi lần thử phải tự đọc lại dữ liệu; ChangeTracker được xóa trước lần thử kế tiếp.
/// </summary>
public static class ConcurrencyRetry
{
    public static async Task<T> ExecuteAsync<T>(AppDbContext context, ILogger logger, string operation, Func<Task<T>> action, Func<T> onConflict)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                context.ChangeTracker.Clear();
                if (attempt >= TradingRules.MaxConcurrencyRetries)
                {
                    logger.LogWarning(ex, "{Operation} still conflicting after {Attempt} attempts", operation, attempt);
                    return onConflict();
                }
                logger.LogInformation("{Operation} hit a concurrency conflict, retrying (attempt {Attempt})", operation, attempt);
            }
        }
    }
}
