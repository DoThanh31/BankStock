using System.ComponentModel.DataAnnotations;
using BankStock.Web.Services;

namespace BankStock.Web.Dtos;

/// <summary>Body của POST /api/v1/wallets/transactions: nạp hoặc rút tiền giả lập.</summary>
public record CashTransferRequest(
    [Display(Name = "AdminTxType"), EnumDataType(typeof(CashTransferType), ErrorMessage = "ValSelectRequired")] CashTransferType Type,
    [Display(Name = "FieldAmount"), Range(TradingRules.MinCashTransfer, TradingRules.MaxCashTransfer, ErrorMessage = "ValRange")] decimal Amount);
