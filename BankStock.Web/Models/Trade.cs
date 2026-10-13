using System.ComponentModel.DataAnnotations.Schema;

namespace BankStock.Web.Models;

public class Trade
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int StockId { get; set; }
    public Stock? Stock { get; set; }
    
    public OrderType Type { get; set; }
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Fee { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
