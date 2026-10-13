using System.ComponentModel.DataAnnotations;
using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Dtos;

/// <summary>Body của POST / PUT /api/v1/banks. Id lấy từ route, không nhận từ body (chống over-posting).</summary>
public record BankRequest(
    [Required(ErrorMessage = "ValRequired"), RegularExpression("^[A-Z0-9]{2,20}$", ErrorMessage = "ValBankCode")] string Code,
    [Required(ErrorMessage = "ValRequired"), StringLength(200, MinimumLength = 2, ErrorMessage = "ValStringLength")] string Name,
    [Required(ErrorMessage = "ValRequired"), StringLength(100, MinimumLength = 2, ErrorMessage = "ValStringLength")] string ShortName,
    [Range(1800, 2100, ErrorMessage = "ValRange")] int? Founded,
    [StringLength(200, ErrorMessage = "ValMaxLength")] string? Headquarters,
    [Url(ErrorMessage = "ValUrl"), StringLength(200, ErrorMessage = "ValMaxLength")] string? Website,
    [Url(ErrorMessage = "ValUrl"), StringLength(500, ErrorMessage = "ValMaxLength")] string? LogoUrl,
    [StringLength(2000, ErrorMessage = "ValMaxLength")] string? Description)
{
    public BankFormViewModel ToFormModel(int id = 0) => new()
    {
        Id = id,
        Code = Code,
        Name = Name,
        ShortName = ShortName,
        Founded = Founded,
        Headquarters = Headquarters,
        Website = Website,
        LogoUrl = LogoUrl,
        Description = Description
    };
}
