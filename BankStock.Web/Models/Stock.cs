using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankStock.Web.Models;

public class Stock
{
    public int Id { get; set; }
    
    [Required, RegularExpression("^[A-Z]{3}$")]
    public string StockCode { get; set; } = string.Empty;
    
    public int BankId { get; set; }
    public Bank? Bank { get; set; }
    
    [Required, StringLength(20)]
    public string Exchange { get; set; } = string.Empty;
    
    public BankGroup BankGroup { get; set; }
    public long ListedShares { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal FaceValue { get; set; }
    public bool IsActive { get; set; } = true;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PE { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PB { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? EPS { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? CASA { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NPL { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? CharterCapital { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DividendYield { get; set; }
    
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    public ICollection<FinancialReport> FinancialReports { get; set; } = new List<FinancialReport>();
}
