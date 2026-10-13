namespace BankStock.Web.Services;

/// <summary>
/// Hệ thống mô phỏng không có SMTP: email chỉ được ghi ra log.
/// Thay bằng implementation gửi thật (SMTP/SendGrid) khi triển khai.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulated email to {To}. Subject: {Subject}. Body: {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
