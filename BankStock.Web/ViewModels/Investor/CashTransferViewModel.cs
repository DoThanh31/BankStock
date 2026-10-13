using System.ComponentModel.DataAnnotations;
using BankStock.Web.Services;

namespace BankStock.Web.ViewModels.Investor;

/// <summary>Số tiền nạp / rút giả lập.</summary>
public class CashTransferViewModel
{
    [Range(TradingRules.MinCashTransfer, TradingRules.MaxCashTransfer, ErrorMessage = "ValRange")]
    [DataType(DataType.Currency)]
    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    [Display(Name = "FieldAmount")]
    public decimal Amount { get; set; }
}
