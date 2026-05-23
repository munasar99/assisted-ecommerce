using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistedEcommerce.Api.Infrastructure;

public static class FileStorageServiceExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CloudinarySettings>(options =>
        {
            configuration.GetSection(CloudinarySettings.SectionName).Bind(options);
            options.CloudName =
                Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")?.Trim()
                ?? options.CloudName;
            options.ApiKey =
                Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")?.Trim()
                ?? options.ApiKey;
            options.ApiSecret =
                Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")?.Trim()
                ?? options.ApiSecret;
        });

        services.AddSingleton<FileStorageService>();
        services.AddSingleton<CloudinaryFileStorageService>();
        services.AddSingleton<IFileStorageService>(sp =>
        {
            var cloudinary = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;
            return cloudinary.IsConfigured
                ? sp.GetRequiredService<CloudinaryFileStorageService>()
                : sp.GetRequiredService<FileStorageService>();
        });

        return services;
    }
}
