using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.Models.System;

namespace start.Services
{
    public class AdminSecurityService : IAdminSecurityService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public AdminSecurityService(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        public async Task<AdminSecurity> GetOrCreateAsync(string employeeId)
        {
            var security = await _db.AdminSecurities.FirstOrDefaultAsync(s => s.EmployeeID == employeeId);
            if (security != null) return security;

            security = new AdminSecurity
            {
                EmployeeID = employeeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.AdminSecurities.Add(security);
            await _db.SaveChangesAsync();

            return security;
        }

        public async Task<bool> IsTwoFactorEnabledAsync(string employeeId)
        {
            var security = await GetOrCreateAsync(employeeId);
            return security.IsTwoFactorEnabled;
        }

        public async Task<AdminOtpResult> SendOtpAsync(Employee admin, AdminOtpPurpose purpose)
        {
            if (string.IsNullOrWhiteSpace(admin.Email))
            {
                return new AdminOtpResult
                {
                    Succeeded = false,
                    Message = "Tài khoản admin chưa có email. Vui lòng cập nhật email trước."
                };
            }

            var security = await GetOrCreateAsync(admin.EmployeeID!);
            var now = DateTime.UtcNow;

            if (security.LockedUntil.HasValue && security.LockedUntil.Value > now)
            {
                return new AdminOtpResult
                {
                    Succeeded = false,
                    Message = $"Tài khoản đang bị khóa đăng nhập tới {security.LockedUntil.Value.ToLocalTime():HH:mm dd/MM}."
                };
            }

            var otp = GenerateOtp();

            security.LastOtpCode = HashOtp(otp);
            security.LastOtpExpiredAt = now.Add(OtpLifetime);
            security.UpdatedAt = now;

            if (purpose == AdminOtpPurpose.Setup)
            {
                security.TwoFactorType = "Email";
            }

            await _db.SaveChangesAsync();

            var subject = purpose == AdminOtpPurpose.Login
                ? "Mã xác thực đăng nhập 2FA cho quản trị viên"
                : purpose == AdminOtpPurpose.Disable
                ? "Mã xác nhận tắt 2FA cho quản trị viên"
                : "Mã xác nhận bật 2FA cho quản trị viên";

            var htmlBody =
                $@"<p>Xin chào {admin.FullName ?? admin.EmployeeID},</p>
                   <p>Mã xác thực của bạn là: <strong style='font-size:20px'>{otp}</strong></p>
                   <p>Mã sẽ hết hạn sau 5 phút. Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
                   <p>Trân trọng,<br/>Hệ thống Bubble Tea Shop</p>";

            await _emailService.SendEmailAsync(admin.Email!, subject, htmlBody);

            return new AdminOtpResult
            {
                Succeeded = true,
                Message = "Đã gửi mã OTP tới email quản trị viên."
            };
        }

        public async Task<AdminOtpVerificationResult> VerifyOtpAsync(string employeeId, string otp)
        {
            var security = await GetOrCreateAsync(employeeId);
            var now = DateTime.UtcNow;

            if (security.LockedUntil.HasValue && security.LockedUntil.Value > now)
            {
                return new AdminOtpVerificationResult
                {
                    Succeeded = false,
                    IsLocked = true,
                    Message = $"Tài khoản đang bị khóa tới {security.LockedUntil.Value.ToLocalTime():HH:mm dd/MM}."
                };
            }

            if (string.IsNullOrWhiteSpace(security.LastOtpCode) || !security.LastOtpExpiredAt.HasValue)
            {
                return new AdminOtpVerificationResult
                {
                    Succeeded = false,
                    Message = "OTP không hợp lệ. Vui lòng yêu cầu mã mới."
                };
            }

            if (security.LastOtpExpiredAt.Value < now)
            {
                await ResetOtpStateAsync(employeeId);
                return new AdminOtpVerificationResult
                {
                    Succeeded = false,
                    Message = "OTP đã hết hạn. Vui lòng yêu cầu mã mới."
                };
            }

            var hashed = HashOtp(otp);
            if (!hashed.Equals(security.LastOtpCode, StringComparison.Ordinal))
            {
                security.FailedCount += 1;
                security.UpdatedAt = now;

                if (security.FailedCount >= MaxFailedAttempts)
                {
                    security.LockedUntil = now.Add(LockDuration);
                    security.FailedCount = 0;
                }

                await _db.SaveChangesAsync();

                if (security.LockedUntil.HasValue && security.LockedUntil.Value > now)
                {
                    return new AdminOtpVerificationResult
                    {
                        Succeeded = false,
                        IsLocked = true,
                        Message = $"Bạn đã nhập sai quá nhiều lần. Tài khoản bị khóa tới {security.LockedUntil.Value.ToLocalTime():HH:mm dd/MM}."
                    };
                }

                return new AdminOtpVerificationResult
                {
                    Succeeded = false,
                    Message = "OTP không chính xác. Vui lòng kiểm tra lại."
                };
            }

            security.FailedCount = 0;
            security.LockedUntil = null;
            security.LastOtpCode = null;
            security.LastOtpExpiredAt = null;
            security.UpdatedAt = now;
            await _db.SaveChangesAsync();

            return new AdminOtpVerificationResult
            {
                Succeeded = true,
                Message = "Xác thực OTP thành công."
            };
        }

        public async Task EnableTwoFactorAsync(string employeeId)
        {
            var security = await GetOrCreateAsync(employeeId);
            security.IsTwoFactorEnabled = true;
            security.TwoFactorType ??= "Email";
            security.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task DisableTwoFactorAsync(string employeeId)
        {
            var security = await GetOrCreateAsync(employeeId);
            security.IsTwoFactorEnabled = false;
            security.TwoFactorType = null;
            security.TwoFactorSecret = null;
            security.RecoveryCodes = null;
            security.LastOtpCode = null;
            security.LastOtpExpiredAt = null;
            security.FailedCount = 0;
            security.LockedUntil = null;
            security.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task ResetOtpStateAsync(string employeeId)
        {
            var security = await GetOrCreateAsync(employeeId);
            security.LastOtpCode = null;
            security.LastOtpExpiredAt = null;
            security.FailedCount = 0;
            security.LockedUntil = null;
            security.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        private static string GenerateOtp()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
            return value.ToString("D6");
        }

        private static string HashOtp(string otp)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(otp);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}


