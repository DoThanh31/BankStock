namespace BankStock.Web.Dtos;

public record RegisterInvestorCommand(string FullName, string Email, string Password, string? PreferredLanguage);
