using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Infrastructure;

namespace AssistedEcommerce.Api.Services;

public interface IUploadService
{
    Task<UploadResponse> UploadOrderScreenshotAsync(IFormFile file, string? orderId, CancellationToken ct = default);
}

public class UploadService(IFileStorageService fileStorage) : IUploadService
{
    public async Task<UploadResponse> UploadOrderScreenshotAsync(IFormFile file, string? orderId, CancellationToken ct = default)
    {
        var folder = string.IsNullOrWhiteSpace(orderId) ? "orders/pending" : $"orders/{orderId}";
        var url = await fileStorage.SaveAsync(file, folder, ct);
        var fileName = Path.GetFileName(url);
        return new UploadResponse(url, fileName);
    }
}
