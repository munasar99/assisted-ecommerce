namespace AssistedEcommerce.Api.Infrastructure;

public class AiBackendSettings
{
    public const string SectionName = "AiBackend";

    public string BaseUrl { get; set; } = "http://localhost:3001";
    public bool Enabled { get; set; } = true;
    public bool ValidateOrders { get; set; } = true;
    public bool VerifyReceipts { get; set; } = true;
    public bool SendPaymentNotifications { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 60;

    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(BaseUrl);
}
