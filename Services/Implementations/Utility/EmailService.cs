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
            // Sử dụng GetAwaiter().GetResult() thay vì Wait() để tránh deadlock
            SendEmailAsync(toEmail, subject, body).GetAwaiter().GetResult();
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, string? replyTo = null)
        {
            try
            {
                // Validate settings
                if (string.IsNullOrEmpty(_settings.FromEmail) || string.IsNullOrEmpty(_settings.AppPassword))
                {
                    _logger.LogError("EmailSettings chưa được cấu hình đầy đủ: FromEmail hoặc AppPassword bị trống");
                    throw new InvalidOperationException("EmailSettings chưa được cấu hình đầy đủ");
                }

                // Loại bỏ khoảng trắng trong App Password (Gmail App Password có thể có khoảng trắng)
                string cleanAppPassword = _settings.AppPassword.Replace(" ", "");

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
                    UseDefaultCredentials = false, // Quan trọng: không dùng credentials mặc định
                    DeliveryMethod = SmtpDeliveryMethod.Network, // Đảm bảo gửi qua network
                    Credentials = new NetworkCredential(_settings.FromEmail, cleanAppPassword),
                    Timeout = 30000 // 30 giây timeout
                };

                _logger.LogInformation("Đang gửi email tới {To} từ {From} qua {Host}:{Port}", to, _settings.FromEmail, _settings.SmtpHost, _settings.SmtpPort);
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Gửi email thành công tới {To}", to);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP lỗi khi gửi email tới {To}: {Message}. StatusCode: {StatusCode}", to, ex.Message, ex.StatusCode);
                
                // Log thêm thông tin debug
                _logger.LogError("SMTP Config - Host: {Host}, Port: {Port}, SSL: {SSL}, FromEmail: {FromEmail}", 
                    _settings.SmtpHost, _settings.SmtpPort, _settings.EnableSsl, _settings.FromEmail);
                
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi bất ngờ khi gửi email tới {To}: {Error}", to, ex.Message);
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
