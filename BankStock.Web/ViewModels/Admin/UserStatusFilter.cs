namespace BankStock.Web.ViewModels.Admin;

/// <summary>Lọc trạng thái tài khoản: đang hoạt động, bị Admin khóa, hoặc bị khóa tạm do sai mật khẩu.</summary>
public enum UserStatusFilter { Active, Disabled, LockedOut }
