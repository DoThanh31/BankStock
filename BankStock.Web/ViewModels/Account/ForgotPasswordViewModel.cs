using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "ValRequired")]
    [EmailAddress(ErrorMessage = "ValEmail")]
    [StringLength(256, ErrorMessage = "ValMaxLength")]
    [Display(Name = "FieldEmail")]
    public string Email { get; set; } = string.Empty;
}
