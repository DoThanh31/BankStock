using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "ValRequired")]
    [EmailAddress(ErrorMessage = "ValEmail")]
    [StringLength(256, ErrorMessage = "ValMaxLength")]
    [Display(Name = "FieldEmail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "ValMaxLength")]
    [Display(Name = "FieldPassword")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "FieldRememberMe")]
    public bool RememberMe { get; set; }
}
