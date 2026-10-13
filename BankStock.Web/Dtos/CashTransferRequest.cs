using System.ComponentModel.DataAnnotations;
using BankStock.Web.Services;

namespace BankStock.Web.Dtos;

/// <summary>Body của POST /api/v1/wallets/transactions: nạp hoặc rút tiền giả lập.</summary>
public record CashTransferRequest(
    [EnumDataType(typeof(CashTransferType), ErrorMessage = "ValSelectRequired")] CashTransferType Type,
    [Range(TradingRules.MinCashTransfer, TradingRules.MaxCashTransfer, ErrorMessage = "ValRange")] decimal Amount);
