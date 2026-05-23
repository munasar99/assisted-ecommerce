using Microsoft.Extensions.Options;

namespace AssistedEcommerce.Api.Infrastructure;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string subFolder, CancellationToken ct = default);
    void ValidateFile(IFormFile file);
}

public class FileStorageService(IOptions<UploadSettings> settings) : IFileStorageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public void ValidateFile(IFormFile file)
    {
        var opts = settings.Value;
        if (file is null || file.Length == 0)
            throw new ArgumentException("File is required.");

        if (file.Length > opts.MaxBytes)
            throw new ArgumentException("File exceeds maximum size of 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Only JPG, PNG, JPEG, and WEBP files are allowed.");
    }

    public async Task<string> SaveAsync(IFormFile file, string subFolder, CancellationToken ct = default)
    {
        ValidateFile(file);
        var root = Path.Combine(Directory.GetCurrentDirectory(), settings.Value.RootPath, subFolder);
        Directory.CreateDirectory(root);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(root, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        return $"/{settings.Value.RootPath}/{subFolder}/{fileName}".Replace('\\', '/');
    }
}
