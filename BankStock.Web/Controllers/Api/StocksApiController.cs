using BankStock.Web.Dtos;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers.Api;

/// <summary>Bảng giá, chi tiết mã và dữ liệu nến (công khai, chỉ đọc).</summary>
[AllowAnonymous]
[Route(ApiDefaults.RoutePrefix + "/stocks")]
public class StocksApiController : ApiControllerBase
{
    private readonly IStockService _stockService;

    public StocksApiController(IStockService stockService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _stockService = stockService;
    }

    /// <summary>Danh sách mã đang giao dịch kèm giá mới nhất; lọc (nhóm, P/E &lt; 10, CASA &gt; 30%, cổ tức), sắp xếp, phân trang.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<StockSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<StockSummaryDto>>> GetAll([FromQuery] StockApiQuery query, CancellationToken cancellationToken)
    {
        var page = await _stockService.GetStockListAsync(query.ToListQuery(), cancellationToken);
        return Ok(PagedResultDto<StockSummaryDto>.From(page, StockSummaryDto.From));
    }

    /// <summary>Chi tiết một mã: giá, chỉ số cơ bản, thông tin ngân hàng và BCTC các quý gần nhất.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(StockDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockDetailDto>> GetByCode(string code, CancellationToken cancellationToken)
    {
        var detail = await _stockService.GetStockDetailAsync(code, cancellationToken);
        return detail is null ? NotFoundProblem(StockNotFound(code)) : Ok(StockDetailDto.From(detail));
    }

    /// <summary>Dữ liệu nến cho biểu đồ. timeframe: 1D | 1W | 1M | 1Y (mặc định 1M).</summary>
    [HttpGet("{code}/history")]
    [ProducesResponseType(typeof(StockHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockHistoryDto>> GetHistory(string code, [FromQuery] string? timeframe, CancellationToken cancellationToken)
    {
        if (!PriceTimeframes.TryParse(timeframe ?? PriceTimeframes.Default, out var parsed))
        {
            ModelState.AddModelError(nameof(timeframe), $"Allowed values: {string.Join(", ", PriceTimeframes.Codes)}.");
            return ValidationProblem(ModelState);
        }

        var history = await _stockService.GetPriceHistoryAsync(code, parsed, cancellationToken);
        return history is null ? NotFoundProblem(StockNotFound(code)) : Ok(history);
    }

    private string StockNotFound(string code) => Localizer["OrderStockNotTradable", code].Value;
}
