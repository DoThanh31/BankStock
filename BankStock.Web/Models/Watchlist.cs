namespace BankStock.Web.Models;

public class Watchlist
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    public int StockId { get; set; }
    public Stock? Stock { get; set; }
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
