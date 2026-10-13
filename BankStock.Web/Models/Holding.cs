using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankStock.Web.Models;

public class Holding
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int StockId { get; set; }
    public Stock? Stock { get; set; }
    
    public int Quantity { get; set; }
    public int BlockedQuantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal AvgCost { get; set; }
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
