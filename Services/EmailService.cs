using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace start.Services
{
    public class EmailService : IEmailService
    {
        private readonly string? fromEmail;
        private readonly string? appPassword;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            fromEmail = config["EmailSettings:FromEmail"];
            appPassword = config["EmailSettings:AppPassword"];
            _logger = logger;
        }

        public void SendOtp(string toEmail, string otp)
        {
            try
            {
                var fromAddress = new MailAddress(fromEmail ?? "", "OTP Verification");
                var toAddress = new MailAddress(toEmail);

                string subject = "Mã xác thực OTP";
                string body = $"Mã OTP của bạn là: {otp}";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, appPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    _logger.LogInformation("Đang gửi OTP tới {Email}", toEmail);
                    smtp.Send(message);
                    _logger.LogInformation("Gửi OTP thành công tới {Email}", toEmail);
                }
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP Exception: {Message}", ex.Message);
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi bất ngờ khi gửi OTP tới {Email}", toEmail);
                throw;
            }
        }
    }
}
