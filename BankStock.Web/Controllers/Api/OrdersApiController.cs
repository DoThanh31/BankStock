using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers.Api;

/// <summary>Sổ lệnh của Investor đang đăng nhập: xem, đặt, hủy lệnh chờ. Lệnh của người khác luôn trả 404 (chống IDOR).</summary>
[Authorize(Roles = AppRoles.Investor)]
[Route(ApiDefaults.RoutePrefix + "/orders")]
public class OrdersApiController : ApiControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersApiController(IOrderService orderService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _orderService = orderService;
    }

    /// <summary>Lịch sử đặt lệnh (mới nhất trước), lọc theo mã / trạng thái / loại lệnh, phân trang.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<OrderDto>>> GetAll([FromQuery] OrderApiQuery query, CancellationToken cancellationToken)
    {
        var page = await _orderService.GetOrdersAsync(CurrentUserId, query.ToListQuery(), cancellationToken);
        return Ok(PagedResultDto<OrderDto>.From(page, OrderDto.From));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderAsync(CurrentUserId, id, cancellationToken);
        return order is null ? NotFoundProblem(Localizer["AdminNotFound"].Value) : Ok(OrderDto.From(order));
    }

    /// <summary>
    /// Đặt lệnh mua/bán giả lập. Market khớp ngay; Limit khớp ngay nếu giá thị trường đã chạm giá đặt, ngược lại chờ (Pending).
    /// Trả 201 kèm Location của lệnh và số dư ví sau lệnh. <c>POST /orders/place</c> là alias theo tài liệu đề bài.
    /// </summary>
    [HttpPost]
    [HttpPost("place")]
    [ProducesResponseType(typeof(OrderPlacedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderPlacedDto>> Place([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var (result, order) = await _orderService.PlaceOrderAsync(CurrentUserId, request.ToCommand(), cancellationToken);
        if (!result.Succeeded || order is null)
        {
            return ProblemFrom(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
    }

    /// <summary>Hủy lệnh đang chờ khớp và hoàn lại tiền / cổ phiếu bị giam. Lệnh đã khớp hoặc đã hủy → 409.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.CancelOrderAsync(CurrentUserId, id, cancellationToken);
        return result.Succeeded ? NoContent() : ProblemFrom(result);
    }
}
