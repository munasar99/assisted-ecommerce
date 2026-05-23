namespace AssistedEcommerce.Api.Infrastructure;

public class UploadSettings
{
    public const string SectionName = "Uploads";
    public string RootPath { get; set; } = "uploads";
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}
