using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace start.Services
{
    public class EmailService
    {
        private readonly string? _fromEmail;
        private readonly string? _appPassword;
        private readonly string? _adminEmail;

        public EmailService(IConfiguration config)
        {
            _fromEmail = config["EmailSettings:FromEmail"];
            _appPassword = config["EmailSettings:AppPassword"];
            _adminEmail = config["EmailSettings:AdminEmail"];
        }

        // ========== OTP CHO ĐĂNG KÝ ==========
        public void SendOtp(string toEmail, string otp)
        {
            var fromAddress = new MailAddress(_fromEmail ?? "", "OTP Verification");
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
                Credentials = new NetworkCredential(fromAddress.Address, _appPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

        // ========== HỖ TRỢ LIÊN HỆ ==========
        public async Task SendEmailAsync(string to, string subject, string htmlBody, string? replyTo = null)
        {
            var fromAddress = new MailAddress(_fromEmail ?? "", "Buble Tea");
            var toAddress = new MailAddress(to);

            using var message = new MailMessage();
            message.From = fromAddress;
            message.To.Add(toAddress);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            if (!string.IsNullOrEmpty(replyTo))
            {
                message.ReplyToList.Add(new MailAddress(replyTo));
            }

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_fromEmail ?? "", _appPassword ?? "")
            };

            await smtp.SendMailAsync(message);
        }

        public Task SendToAdminAsync(string subject, string htmlBody, string? replyTo = null)
        {
            if (string.IsNullOrEmpty(_adminEmail))
                throw new InvalidOperationException("AdminEmail chưa được cấu hình trong appsettings.json");

            return SendEmailAsync(_adminEmail!, subject, htmlBody, replyTo);
        }
    }
}
