using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Admin;

public class BankFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "ValRequired")]
    [RegularExpression("^[A-Z0-9]{2,20}$", ErrorMessage = "ValBankCode")]
    [Display(Name = "FieldBankCode")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "ValStringLength")]
    [Display(Name = "FieldBankFullName")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "ValRequired")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "ValStringLength")]
    [Display(Name = "FieldBankShortName")]
    public string ShortName { get; set; } = string.Empty;

    [Range(1800, 2100, ErrorMessage = "ValRange")]
    [Display(Name = "BankFounded")]
    public int? Founded { get; set; }

    [StringLength(200, ErrorMessage = "ValMaxLength")]
    [Display(Name = "BankHeadquarters")]
    public string? Headquarters { get; set; }

    [Url(ErrorMessage = "ValUrl")]
    [StringLength(200, ErrorMessage = "ValMaxLength")]
    [DataType(DataType.Url)]
    [Display(Name = "FieldWebsite")]
    public string? Website { get; set; }

    [Url(ErrorMessage = "ValUrl")]
    [StringLength(500, ErrorMessage = "ValMaxLength")]
    [DataType(DataType.ImageUrl)]
    [Display(Name = "FieldLogoUrl")]
    public string? LogoUrl { get; set; }

    [StringLength(2000, ErrorMessage = "ValMaxLength")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "FieldDescription")]
    public string? Description { get; set; }

    public bool IsNew => Id == 0;
}
