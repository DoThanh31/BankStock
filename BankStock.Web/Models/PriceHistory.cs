using System.ComponentModel.DataAnnotations.Schema;

namespace BankStock.Web.Models;

public class PriceHistory
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public Stock? Stock { get; set; }
    
    public DateTime TradeDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal RefPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CeilingPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal FloorPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Open { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal High { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Low { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Close { get; set; }
    
    public long Volume { get; set; }
}
