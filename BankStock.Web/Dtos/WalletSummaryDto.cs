namespace BankStock.Web.Dtos;

/// <summary>Số dư ví: khả dụng (Balance) + phong tỏa cho lệnh mua chờ khớp (BlockedBalance).</summary>
public record WalletSummaryDto(decimal Balance, decimal BlockedBalance, bool IsActive)
{
    public decimal Total => Balance + BlockedBalance;
}
