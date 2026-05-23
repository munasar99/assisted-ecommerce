namespace AssistedEcommerce.Api.DTOs;

public record UploadResponse(string Url, string FileName);

public record PaymentUploadResponse(
    string PaymentId,
    string OrderId,
    string Status,
    string PaymentScreenshotUrl,
    bool EmailSent = false,
    string? EmailError = null);
