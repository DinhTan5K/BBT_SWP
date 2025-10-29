using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using start.Models.Configurations;

namespace start.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public void SendOtp(string toEmail, string otp)
        {
            string subject = "Mã xác thực OTP";
            string body = $"Mã OTP của bạn là: {otp}";
            SendEmailAsync(toEmail, subject, body).Wait();
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, string? replyTo = null)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_settings.FromEmail, "Support Team");
                message.To.Add(to);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                if (!string.IsNullOrEmpty(replyTo))
                    message.ReplyToList.Add(new MailAddress(replyTo));

                using var smtp = new SmtpClient
                {
                    Host = _settings.SmtpHost,
                    Port = _settings.SmtpPort,
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.FromEmail, _settings.AppPassword)
                };

                _logger.LogInformation("Đang gửi email tới {To}", to);
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Gửi email thành công tới {To}", to);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP lỗi khi gửi email tới {To}: {Message}", to, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi bất ngờ khi gửi email tới {To}", to);
                throw;
            }
        }

        public async Task SendToAdminAsync(string subject, string htmlBody, string? replyTo = null)
        {
            if (string.IsNullOrEmpty(_settings.AdminEmail))
            {
                _logger.LogWarning("AdminEmail chưa được cấu hình trong EmailSettings.");
                return;
            }

            await SendEmailAsync(_settings.AdminEmail, subject, htmlBody, replyTo);
        }
    }
}
