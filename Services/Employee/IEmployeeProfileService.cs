using start.Models;
using start.Models.ViewModels;
public interface IEmployeeProfileService
{
    bool EditProfile(string employeeId, EditEmployeeProfile model, out string error);
    Task<bool> UploadAvatar(string employeeId, IFormFile avatar);
    Employee? GetById(string employeeId);

}