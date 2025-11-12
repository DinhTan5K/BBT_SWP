public interface ICloudinaryService
{
    Task<string?> UploadImageAsync(IFormFile file, string folder = "uploads/avatars");
    Task<bool> DeleteImageAsync(string publicId);
}

