using start.Models;
public interface IAuthService
{
    // Helper
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);

    // Đăng nhập
    Customer? Login(string loginId, string password);

    // Đăng ký
    Customer? Register(Customer model, out string otp , out Dictionary<string, string> errors);

    // Đổi mật khẩu
    bool ChangePassword(int userId, string currentPassword, string newPassword, out string error);

    // Reset mật khẩu
    bool ResetPassword(string email, string newPassword, out string error);

    // Đặt mật khẩu lần đầu (tài khoản Google chưa có password)
    bool SetPassword(int userId, string newPassword, out string error);

    // Xác thực Email bằng OTP
    bool VerifyEmail(string email, string otp);
    Customer HandleGoogleLogin(string email, string name); 
    bool SendOtp(string email, string purpose, out string otp, out string error);
}
