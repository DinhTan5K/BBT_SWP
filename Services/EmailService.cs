using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace start.Services
{
    public class EmailService
    {
        private readonly string? fromEmail;
        private readonly string? appPassword;

        public EmailService(IConfiguration config)
        {
            fromEmail = config["EmailSettings:FromEmail"];
            appPassword = config["EmailSettings:AppPassword"];
        }

        public void SendOtp(string toEmail, string otp)
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
                smtp.Send(message);
            }
        }
    }
}
