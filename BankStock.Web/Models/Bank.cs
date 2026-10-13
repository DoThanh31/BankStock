using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.Models;

public class Bank
{
    public int Id { get; set; }
    
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;
    
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required, StringLength(100)]
    public string ShortName { get; set; } = string.Empty;
    
    public int? Founded { get; set; }
    
    [StringLength(200)]
    public string? Headquarters { get; set; }
    
    [StringLength(200)]
    public string? Website { get; set; }
    
    [StringLength(500)]
    public string? LogoUrl { get; set; }
    
    public string? Description { get; set; }
    
    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
