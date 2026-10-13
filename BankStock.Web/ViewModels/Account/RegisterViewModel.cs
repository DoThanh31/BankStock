using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Account;

public class RegisterViewModel
{
    public const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

    [Required(ErrorMessage = "ValRequired")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "ValStringLength")]
    [Display(Name = "FieldFullName")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    [EmailAddress(ErrorMessage = "ValEmail")]
    [StringLength(256, ErrorMessage = "ValMaxLength")]
    [DataType(DataType.EmailAddress)]
    [Display(Name = "FieldEmail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "ValStringLength")]
    [RegularExpression(PasswordPattern, ErrorMessage = "ValPasswordComplexity")]
    [DataType(DataType.Password)]
    [Display(Name = "FieldPassword")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "ValPasswordMismatch")]
    [Display(Name = "FieldConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
