namespace BankStock.Web.Dtos;

/// <summary>Token chống CSRF: client gửi lại <c>Token</c> trong header <c>HeaderName</c> ở mọi request POST/PUT/DELETE.</summary>
public record AntiforgeryTokenDto(string HeaderName, string Token);
