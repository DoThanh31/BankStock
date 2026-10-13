using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Admin;

/// <summary>Báo cáo tài chính quý; các khoản tiền nhập theo đơn vị tỷ VNĐ (DB lưu VNĐ).</summary>
public class FinancialReportFormViewModel
{
    public const decimal Billion = 1_000_000_000m;

    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ValSelectRequired")]
    [Display(Name = "FieldStockCode")]
    public int StockId { get; set; }

    [Range(2000, 2100, ErrorMessage = "ValRange")]
    [Display(Name = "FieldYear")]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [Range(1, 4, ErrorMessage = "ValRange")]
    [Display(Name = "FieldQuarter")]
    public int Quarter { get; set; } = 1;

    [Range(0, 9_000_000, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N2}")]
    [Display(Name = "FieldRevenueBillion")]
    public decimal RevenueBillion { get; set; }

    [Range(-9_000_000, 9_000_000, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N2}")]
    [Display(Name = "FieldNetProfitBillion")]
    public decimal NetProfitBillion { get; set; }

    [Range(0, 9_000_000, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N2}")]
    [Display(Name = "FieldTotalAssetsBillion")]
    public decimal TotalAssetsBillion { get; set; }

    [Range(0, 9_000_000, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N2}")]
    [Display(Name = "FieldTotalLiabilitiesBillion")]
    public decimal TotalLiabilitiesBillion { get; set; }

    public bool IsNew => Id == 0;
}
