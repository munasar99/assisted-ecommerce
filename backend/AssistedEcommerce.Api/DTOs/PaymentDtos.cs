namespace AssistedEcommerce.Api.DTOs;

public record PaymentDto(
    string PaymentId,
    string OrderId,
    string UserId,
    string? PayerPhone,
    string? PaymentMethod,
    string? ScreenshotUrl,
    decimal AmountUsd,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreatePaymentRequest(
    string OrderId,
    string? PayerPhone,
    string? PaymentMethod,
    decimal AmountUsd,
    string? ScreenshotUrl = null,
    string? Status = null);

public record UpdatePaymentRequest(
    string? PayerPhone = null,
    string? PaymentMethod = null,
    decimal? AmountUsd = null,
    string? ScreenshotUrl = null,
    string? Status = null);
