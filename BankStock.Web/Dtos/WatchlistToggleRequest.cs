using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.Dtos;

/// <summary>Body của POST /api/v1/watchlist/toggle. Annotation đặt trên tham số constructor (yêu cầu của MVC với record).</summary>
public record WatchlistToggleRequest(
    [Display(Name = "FieldStockCode"), Required(ErrorMessage = "ValRequired"), RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "ValStockCode")] string StockCode);
