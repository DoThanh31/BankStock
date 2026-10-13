using Microsoft.AspNetCore.Identity;

namespace BankStock.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    public Wallet? Wallet { get; set; }
}
