namespace BankStock.Web.Services;

public enum ServiceErrorType { None, NotFound, Conflict, Validation }

/// <summary>
/// Kết quả thao tác ghi của Service. Lỗi mang khóa resource (<see cref="ErrorKey"/>) để Controller dịch theo culture.
/// </summary>
public sealed record ServiceResult
{
    public ServiceErrorType ErrorType { get; init; }
    public string? ErrorKey { get; init; }

    /// <summary>Tên property gây lỗi (để gắn vào ModelState); null = lỗi chung.</summary>
    public string? Field { get; init; }

    public object[] ErrorArgs { get; init; } = [];
    public int? EntityId { get; init; }

    /// <summary>Số bản ghi bị ảnh hưởng thêm (vd. số lệnh chờ được khớp sau khi Admin đổi giá).</summary>
    public int AffectedCount { get; init; }

    public bool Succeeded => ErrorType == ServiceErrorType.None;

    public static ServiceResult Success(int? entityId = null) => new() { EntityId = entityId };

    public static ServiceResult NotFound() => new() { ErrorType = ServiceErrorType.NotFound, ErrorKey = "AdminNotFound" };

    public static ServiceResult Conflict(string errorKey, string? field = null, params object[] args) =>
        new() { ErrorType = ServiceErrorType.Conflict, ErrorKey = errorKey, Field = field, ErrorArgs = args };

    public static ServiceResult Invalid(string errorKey, string? field = null, params object[] args) =>
        new() { ErrorType = ServiceErrorType.Validation, ErrorKey = errorKey, Field = field, ErrorArgs = args };
}
