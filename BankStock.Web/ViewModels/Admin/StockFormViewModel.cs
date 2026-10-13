using System.ComponentModel.DataAnnotations;
using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Admin;

public class StockFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "ValRequired")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "ValStockCode")]
    [Display(Name = "FieldStockCode")]
    public string StockCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "ValSelectRequired")]
    [Display(Name = "FieldBank")]
    public int BankId { get; set; }

    [Required(ErrorMessage = "ValRequired")]
    [RegularExpression("^(HOSE|HNX|UPCOM)$", ErrorMessage = "ValExchange")]
    [Display(Name = "FieldExchange")]
    public string Exchange { get; set; } = "HOSE";

    [EnumDataType(typeof(BankGroup), ErrorMessage = "ValSelectRequired")]
    [Display(Name = "FieldBankGroup")]
    public BankGroup BankGroup { get; set; } = BankGroup.PrivateJSC;

    [Range(0, 100_000_000_000, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "MetricListedShares")]
    public long ListedShares { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    [Display(Name = "MetricFaceValue")]
    public decimal FaceValue { get; set; } = 10_000m;

    [Display(Name = "FieldIsActive")]
    public bool IsActive { get; set; } = true;

    [Range(0, 1000, ErrorMessage = "ValRange")]
    [Display(Name = "FieldPE")]
    public decimal? PE { get; set; }

    [Range(0, 100, ErrorMessage = "ValRange")]
    [Display(Name = "FieldPB")]
    public decimal? PB { get; set; }

    [Range(-1_000_000, 1_000_000, ErrorMessage = "ValRange")]
    [Display(Name = "FieldEPS")]
    public decimal? EPS { get; set; }

    [Range(0, 100, ErrorMessage = "ValRange")]
    [Display(Name = "FieldCASA")]
    public decimal? CASA { get; set; }

    [Range(0, 100, ErrorMessage = "ValRange")]
    [Display(Name = "MetricNpl")]
    public decimal? NPL { get; set; }

    [Range(0, 100, ErrorMessage = "ValRange")]
    [Display(Name = "ThDividend")]
    public decimal? DividendYield { get; set; }

    /// <summary>Vốn điều lệ nhập theo tỷ VNĐ; để trống thì tính = KL niêm yết × mệnh giá.</summary>
    [Range(0, 9_000_000, ErrorMessage = "ValRange")]
    [Display(Name = "FieldCharterCapitalBillion")]
    public decimal? CharterCapitalBillion { get; set; }

    public bool IsNew => Id == 0;
}
