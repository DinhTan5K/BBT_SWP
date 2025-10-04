using start.Models;

public interface IProfileService
{
    // Sửa thông tin cá nhân
    bool EditProfile(int userId, EditProfileModel model, out string error);

    // Upload avatar
    Task<bool> UploadAvatar(int userId, IFormFile avatar);
     Customer? GetUserById(int userId);
}
