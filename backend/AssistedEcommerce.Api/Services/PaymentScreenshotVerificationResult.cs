namespace AssistedEcommerce.Api.Services;

public sealed class PaymentScreenshotVerificationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal? DetectedAmountUsd { get; init; }
    public DateTime? DetectedAtUtc { get; init; }
    public string? OcrSnippet { get; init; }

    public static PaymentScreenshotVerificationResult Ok(decimal amount, DateTime atUtc, string? snippet) =>
        new() { IsValid = true, DetectedAmountUsd = amount, DetectedAtUtc = atUtc, OcrSnippet = snippet };

    public static PaymentScreenshotVerificationResult Fail(string message, string? snippet = null) =>
        new() { IsValid = false, ErrorMessage = message, OcrSnippet = snippet };
}
