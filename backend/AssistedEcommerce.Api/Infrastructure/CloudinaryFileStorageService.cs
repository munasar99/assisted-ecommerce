using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace AssistedEcommerce.Api.Infrastructure;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly UploadSettings _upload;
    private readonly string _rootFolder;

    public CloudinaryFileStorageService(
        IOptions<CloudinarySettings> cloudinaryOptions,
        IOptions<UploadSettings> uploadOptions)
    {
        var cfg = cloudinaryOptions.Value;
        _upload = uploadOptions.Value;
        _rootFolder = cfg.Folder.Trim().Trim('/');

        var account = new Account(cfg.CloudName, cfg.ApiKey, cfg.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public void ValidateFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("File is required.");

        if (file.Length > _upload.MaxBytes)
            throw new ArgumentException("File exceeds maximum size of 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!IsAllowedExtension(ext))
            throw new ArgumentException("Only JPG, PNG, JPEG, and WEBP files are allowed.");
    }

    public async Task<string> SaveAsync(IFormFile file, string subFolder, CancellationToken ct = default)
    {
        ValidateFile(file);

        var folder = string.IsNullOrWhiteSpace(_rootFolder)
            ? SanitizeFolder(subFolder)
            : $"{_rootFolder}/{SanitizeFolder(subFolder)}";

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return result.SecureUrl?.ToString()
               ?? throw new InvalidOperationException("Cloudinary returned no image URL.");
    }

    private static string SanitizeFolder(string subFolder) =>
        subFolder.Replace('\\', '/').Trim('/');

    private static bool IsAllowedExtension(string ext) =>
        ext is ".jpg" or ".jpeg" or ".png" or ".webp";
}
