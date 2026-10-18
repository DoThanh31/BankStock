using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Account;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "ValRequired")]
    [EmailAddress(ErrorMessage = "ValEmail")]
    [StringLength(256, ErrorMessage = "ValMaxLength")]
    [Display(Name = "FieldEmail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "ValStringLength")]
    [RegularExpression(RegisterViewModel.PasswordPattern, ErrorMessage = "ValPasswordComplexity")]
    [DataType(DataType.Password)]
    [Display(Name = "FieldNewPassword")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "ValPasswordMismatch")]
    [Display(Name = "FieldConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    public string Code { get; set; } = string.Empty;
}
