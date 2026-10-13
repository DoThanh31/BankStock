using System.ComponentModel.DataAnnotations.Schema;

namespace BankStock.Web.Models;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int StockId { get; set; }
    public Stock? Stock { get; set; }
    
    public OrderType Type { get; set; }
    public OrderMode Mode { get; set; }
    public OrderStatus Status { get; set; }
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Fee { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal BlockedAmount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
