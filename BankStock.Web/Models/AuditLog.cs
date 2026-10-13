namespace BankStock.Web.Models;

public class AuditLog
{
    public int Id { get; set; }
    
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public string Action { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? Detail { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
