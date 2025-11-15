using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace start.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService>? _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService>? logger = null)
        {
            _logger = logger;
            var cloudName = configuration["CloudinarySettings:CloudName"];
            var apiKey = configuration["CloudinarySettings:ApiKey"];
            var apiSecret = configuration["CloudinarySettings:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                var errorMsg = "Cloudinary settings are missing or invalid. Please check appsettings.json";
                _logger?.LogError(errorMsg);
                System.Diagnostics.Debug.WriteLine($"[CloudinaryService] {errorMsg}");
                throw new InvalidOperationException(errorMsg);
            }

            try
            {
                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account);
                _logger?.LogInformation("CloudinaryService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Cloudinary account");
                System.Diagnostics.Debug.WriteLine($"[CloudinaryService] Failed to initialize: {ex.Message}");
                throw;
            }
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder = "uploads/avatars")
        {
            if (file == null || file.Length == 0)
            {
                _logger?.LogWarning("UploadImageAsync: File is null or empty");
                return null;
            }

            // Validate file size (max 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                _logger?.LogWarning($"UploadImageAsync: File size {file.Length} exceeds maximum {maxFileSize}");
                return null;
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                _logger?.LogWarning($"UploadImageAsync: Invalid file extension: {fileExtension}");
                return null;
            }

            try
            {
                // Tạo publicId duy nhất với employee prefix thay vì customer
                var publicId = $"employee_{Guid.NewGuid():N}";
                
                // Đọc toàn bộ file vào memory stream để tránh lỗi khi stream bị dispose
                using var memoryStream = new MemoryStream();
                using (var stream = file.OpenReadStream())
                {
                    await stream.CopyToAsync(memoryStream);
                }
                memoryStream.Position = 0; // Reset position về đầu stream

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, memoryStream),
                    Folder = folder,
                    PublicId = publicId,
                    Overwrite = true, // Cho phép ghi đè nếu publicId trùng (không nên xảy ra với Guid)
                    AllowedFormats = new[] { "jpg", "jpeg", "png", "gif", "webp" },
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK && uploadResult.SecureUrl != null)
                {
                    _logger?.LogInformation($"UploadImageAsync: Successfully uploaded image with publicId: {publicId}, URL: {uploadResult.SecureUrl}");
                    return uploadResult.SecureUrl.ToString();
                }
                else
                {
                    var errorMsg = uploadResult.Error?.Message ?? "Unknown error";
                    _logger?.LogError($"UploadImageAsync: Upload failed with status code: {uploadResult.StatusCode}, Error: {errorMsg}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"UploadImageAsync: Exception occurred while uploading image: {ex.Message}, StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

