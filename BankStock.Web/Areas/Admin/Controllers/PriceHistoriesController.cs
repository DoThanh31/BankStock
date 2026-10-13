using BankStock.Web.Filters;
using BankStock.Web.Dtos;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

public class PriceHistoriesController : AdminControllerBase
{
    private const string FormPartial = "_Form";
    private const string AdjustPartial = "_AdjustForm";

    private readonly IPriceHistoryAdminService _priceService;
    private readonly IStockAdminService _stockService;

    public PriceHistoriesController(IPriceHistoryAdminService priceService, IStockAdminService stockService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _priceService = priceService;
        _stockService = stockService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] PriceHistoryListQuery query, CancellationToken cancellationToken)
    {
        var items = await _priceService.GetPagedAsync(query, cancellationToken);
        return ListView(new PagedListViewModel<PriceHistoryListItem, PriceHistoryListQuery>(query, items)
        {
            StockOptions = await _stockService.GetOptionsAsync(cancellationToken)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? stockId, CancellationToken cancellationToken)
    {
        await LoadStockOptionsAsync(cancellationToken);
        return PartialView(FormPartial, new PriceHistoryFormViewModel { StockId = stockId ?? 0, TradeDate = DateTime.UtcNow.Date });
    }

    [HttpPost]
    public async Task<IActionResult> Create(PriceHistoryFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = 0;
        return await SaveAsync(model, () => _priceService.CreateAsync(model, cancellationToken), cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _priceService.GetForEditAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        await LoadStockOptionsAsync(cancellationToken);
        return PartialView(FormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, PriceHistoryFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        return await SaveAsync(model, () => _priceService.UpdateAsync(model, cancellationToken), cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _priceService.DeleteAsync(id, cancellationToken);
        return ActionResultJson(result, "AdminDeleted");
    }

    /// <summary>Form điều chỉnh giá thủ công (tham chiếu, trần, sàn, giá khớp) cho một mã và một ngày.</summary>
    [HttpGet]
    public async Task<IActionResult> Adjust(int stockId, DateTime? tradeDate, CancellationToken cancellationToken)
    {
        var model = await _priceService.GetAdjustAsync(stockId, tradeDate, cancellationToken);
        return model is null ? NotFound() : PartialView(AdjustPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Adjust(PriceAdjustViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return InvalidForm(AdjustPartial, model);
        }

        var result = await _priceService.AdjustAsync(model, cancellationToken);
        return FormResult(result, AdjustPartial, model, "AdminPriceAdjusted", model.StockCode, result.AffectedCount);
    }

    /// <summary>Trần/sàn gợi ý theo biên độ sàn niêm yết (HOSE 7%, HNX 10%, UPCOM 15%).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PriceBandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Band(int stockId, decimal refPrice, CancellationToken cancellationToken)
    {
        if (refPrice <= 0 || refPrice > (decimal)PriceBandFormBase.MaxPrice)
        {
            return JsonError(StatusCodes.Status400BadRequest, Localizer["ValRange", Localizer["ThRef"], 1, PriceBandFormBase.MaxPrice].Value);
        }

        var band = await _priceService.GetBandAsync(stockId, refPrice, cancellationToken);
        return band is null ? JsonError(StatusCodes.Status404NotFound, Localizer["AdminNotFound"].Value) : Json(band);
    }

    private async Task<IActionResult> SaveAsync(PriceHistoryFormViewModel model, Func<Task<ServiceResult>> save, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadStockOptionsAsync(cancellationToken);
            return InvalidForm(FormPartial, model);
        }

        var result = await save();
        if (!result.Succeeded)
        {
            await LoadStockOptionsAsync(cancellationToken);
        }
        return FormResult(result, FormPartial, model, "AdminSavedPrice");
    }

    private async Task LoadStockOptionsAsync(CancellationToken cancellationToken)
    {
        var stocks = await _stockService.GetOptionsAsync(cancellationToken);
        ViewData["StockOptions"] = stocks
            .Select(s => new SelectListItem($"{s.StockCode} ({s.Exchange})", s.Id.ToString()))
            .ToList();
    }
}
