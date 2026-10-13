using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Admin;

/// <summary>Form gán quyền: chỉ hai role cố định của hệ thống.</summary>
public class UserRolesViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    [Display(Name = "RoleAdmin")]
    public bool IsAdmin { get; set; }

    [Display(Name = "RoleInvestor")]
    public bool IsInvestor { get; set; }
}
