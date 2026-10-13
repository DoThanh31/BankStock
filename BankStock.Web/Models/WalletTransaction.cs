using System.ComponentModel.DataAnnotations.Schema;

namespace BankStock.Web.Models;

public class WalletTransaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public Wallet? Wallet { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }
    
    public WalletTransactionType Type { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
