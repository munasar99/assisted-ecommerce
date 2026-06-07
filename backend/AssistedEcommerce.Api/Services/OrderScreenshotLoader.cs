namespace AssistedEcommerce.Api.Services;

public interface IOrderScreenshotLoader
{
    Task<string?> ToBase64Async(string? screenshotUrl, CancellationToken ct = default);
}

public class OrderScreenshotLoader(IHttpClientFactory httpClientFactory, ILogger<OrderScreenshotLoader> logger)
    : IOrderScreenshotLoader
{
    public async Task<string?> ToBase64Async(string? screenshotUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(screenshotUrl)) return null;

        var url = screenshotUrl.Trim();

        try
        {
            if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var localPath = Path.Combine(Directory.GetCurrentDirectory(), url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(localPath)) return null;
                var bytes = await File.ReadAllBytesAsync(localPath, ct);
                return Convert.ToBase64String(bytes);
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var remoteBytes = await client.GetByteArrayAsync(uri, ct);
                return Convert.ToBase64String(remoteBytes);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not load order screenshot for AI validation: {Url}", url);
        }

        return null;
    }
}
