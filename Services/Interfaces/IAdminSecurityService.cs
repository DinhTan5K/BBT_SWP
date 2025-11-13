using System.Threading.Tasks;
using start.Models;
using start.Models.System;

namespace start.Services
{
    public enum AdminOtpPurpose
    {
        Login,
        Setup,
        Disable
    }

    public class AdminOtpResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AdminOtpVerificationResult
    {
        public bool Succeeded { get; set; }
        public bool IsLocked { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface IAdminSecurityService
    {
        Task<AdminSecurity> GetOrCreateAsync(string employeeId);
        Task<bool> IsTwoFactorEnabledAsync(string employeeId);
        Task<AdminOtpResult> SendOtpAsync(Employee admin, AdminOtpPurpose purpose);
        Task<AdminOtpVerificationResult> VerifyOtpAsync(string employeeId, string otp);
        Task EnableTwoFactorAsync(string employeeId);
        Task DisableTwoFactorAsync(string employeeId);
        Task ResetOtpStateAsync(string employeeId);
    }
}


