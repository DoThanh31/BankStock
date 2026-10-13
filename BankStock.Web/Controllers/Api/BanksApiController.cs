using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers.Api;

/// <summary>Ngân hàng: đọc công khai; tạo / sửa / xóa chỉ Admin (ghi AuditLog trong Service).</summary>
[Route(ApiDefaults.RoutePrefix + "/banks")]
public class BanksApiController : ApiControllerBase
{
    private readonly IBankAdminService _bankService;

    public BanksApiController(IBankAdminService bankService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _bankService = bankService;
    }

    /// <summary>Danh sách ngân hàng, tìm theo mã / tên (q), phân trang.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResultDto<BankDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<BankDto>>> GetAll([FromQuery] BankApiQuery query, CancellationToken cancellationToken)
    {
        var page = await _bankService.GetPagedAsync(query.ToListQuery(), cancellationToken);
        return Ok(PagedResultDto<BankDto>.From(page, BankDto.From));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BankDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var bank = await _bankService.GetByIdAsync(id, cancellationToken);
        return bank is null ? NotFoundProblem(Localizer["AdminNotFound"].Value) : Ok(BankDto.From(bank));
    }

    /// <summary>Tạo ngân hàng. Trả 201 kèm header Location; trùng mã → 409.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(BankDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BankDto>> Create([FromBody] BankRequest request, CancellationToken cancellationToken)
    {
        var result = await _bankService.CreateAsync(request.ToFormModel(), cancellationToken);
        if (!result.Succeeded)
        {
            return ProblemFrom(result);
        }

        var bank = await _bankService.GetByIdAsync(result.EntityId!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.EntityId }, BankDto.From(bank!));
    }

    /// <summary>Cập nhật toàn bộ thông tin ngân hàng.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] BankRequest request, CancellationToken cancellationToken)
    {
        var result = await _bankService.UpdateAsync(request.ToFormModel(id), cancellationToken);
        return result.Succeeded ? NoContent() : ProblemFrom(result);
    }

    /// <summary>Xóa ngân hàng; còn mã cổ phiếu → 409.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _bankService.DeleteAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : ProblemFrom(result);
    }
}
