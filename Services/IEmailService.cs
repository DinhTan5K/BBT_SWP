public interface IEmailService
{
    void SendOtp(string email, string otp);
    Task SendEmailAsync(string to, string subject, string htmlBody, string? replyTo = null);
    Task SendToAdminAsync(string subject, string htmlBody, string? replyTo = null);
}
