using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankStock.Web.Models;

public class Wallet
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal BlockedBalance { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
