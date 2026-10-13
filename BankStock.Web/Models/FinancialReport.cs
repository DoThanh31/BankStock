using System.ComponentModel.DataAnnotations.Schema;

namespace BankStock.Web.Models;

public class FinancialReport
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public Stock? Stock { get; set; }
    
    public int Year { get; set; }
    public int Quarter { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Revenue { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetProfit { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAssets { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLiabilities { get; set; }
}
