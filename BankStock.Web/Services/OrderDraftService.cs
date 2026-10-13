using BankStock.Web.Models;
using BankStock.Web.ViewModels.Investor;

namespace BankStock.Web.Services;

public class OrderDraftService : IOrderDraftService
{
    public const int MaxDrafts = 10;
    private const string KeyPrefix = "OrderDrafts:";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrderDraftService> _logger;

    public OrderDraftService(IHttpContextAccessor httpContextAccessor, ILogger<OrderDraftService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    // Khóa theo UserId: phòng trường hợp đổi tài khoản trên cùng trình duyệt mà Session chưa bị xóa.
    private static string Key(string userId) => KeyPrefix + userId;

    public IReadOnlyList<OrderDraftItem> GetAll(string userId) =>
        Session?.GetJson<List<OrderDraftItem>>(Key(userId), _logger) ?? [];

    public ServiceResult Add(string userId, PlaceOrderViewModel order)
    {
        if (Session is not { } session)
        {
            return ServiceResult.Invalid("AdminOperationFailed");
        }

        var drafts = GetAll(userId).ToList();
        if (drafts.Count >= MaxDrafts)
        {
            return ServiceResult.Conflict("OrderDraftFull", null, MaxDrafts);
        }

        // Lệnh Market không lưu giá: khi đặt sẽ khớp theo giá thị trường lúc đó.
        drafts.Add(new OrderDraftItem(
            Guid.NewGuid(),
            order.StockCode,
            order.Type,
            order.Mode,
            order.Quantity,
            order.Mode == OrderMode.Limit ? order.Price : null,
            DateTime.UtcNow));
        session.SetJson(Key(userId), drafts);
        return ServiceResult.Success();
    }

    public bool Remove(string userId, Guid draftId)
    {
        if (Session is not { } session)
        {
            return false;
        }

        var drafts = GetAll(userId).ToList();
        if (drafts.RemoveAll(d => d.Id == draftId) == 0)
        {
            return false;
        }

        session.SetJson(Key(userId), drafts);
        return true;
    }

    public void Clear(string userId) => Session?.Remove(Key(userId));
}
