namespace AssistedEcommerce.Api.DTOs;

public record CreateOrderRequest(
    string FullName,
    string Phone,
    string Email,
    string ProductUrl,
    int Quantity,
    decimal ProductUnitPriceUsd,
    string DeliveryType,
    string? DistrictId = null,
    string? AddressDetail = null,
    string? ProductName = null,
    string? Notes = null,
    string? OrderScreenshotUrl = null,
    /// <summary>UTC markii macmiilku furay foomka — server wuxuu hubiyaa waqtiga buuxinta.</summary>
    DateTime? FormStartedAtUtc = null);

public record CreateOrderResponse(
    string OrderId,
    string UserId,
    string DeliveryType,
    string DistrictId,
    string DistrictName,
    decimal DeliveryFee,
    decimal ProductSubtotalUsd,
    decimal ServiceFeeUsd,
    decimal TotalAmountUsd,
    string? InvoiceId,
    string Status,
    bool EmailSent = false,
    string? EmailError = null);

public record TrackOrderRequest(string OrderId, string Phone);

public record UpdateOrderStatusRequest(string Status, string? Note = null);

/// <summary>PUT /api/orders/{id} — POST body oo kale waa la aqbalaa (fields optional).</summary>
public class UpdateOrderRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ProductUrl { get; set; }
    public string? ProductName { get; set; }
    public int? Quantity { get; set; }
    public decimal? ProductUnitPriceUsd { get; set; }
    public string? Notes { get; set; }
    public string? AddressDetail { get; set; }
    public string? DeliveryType { get; set; }
    public string? DistrictId { get; set; }
    public string? OrderScreenshotUrl { get; set; }
    public string? Status { get; set; }
    public string? StatusNote { get; set; }
}

public record CreateInvoiceRequest(decimal ProductCost, decimal ServiceFee, decimal OtherCharges);

public record OrderListItemDto(
    string OrderId,
    string UserId,
    string? CustomerFullName,
    string? CustomerPhone,
    string? CustomerEmail,
    string Status,
    string DistrictId,
    decimal DeliveryFee,
    decimal TotalAmountUsd,
    string? ProductName,
    string? PaymentScreenshotUrl,
    string? InvoiceId,
    DateTime CreatedAt);

public record OrderDetailDto(
    string OrderId,
    string UserId,
    string? CustomerFullName,
    string? CustomerPhone,
    string? CustomerEmail,
    string ProductUrl,
    string? ProductName,
    int Quantity,
    string? Notes,
    string DeliveryType,
    string DistrictId,
    string DistrictName,
    string AddressDetail,
    decimal DeliveryFee,
    decimal ProductUnitPriceUsd,
    decimal ProductSubtotalUsd,
    decimal KgFeeUsd,
    decimal ServiceFeeUsd,
    decimal TotalAmountUsd,
    string Status,
    string? OrderScreenshotUrl,
    string? PaymentScreenshotUrl,
    string? InvoiceId,
    IReadOnlyList<StatusHistoryDto> StatusHistory,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record StatusHistoryDto(string Status, DateTime At, string? By, string? Note);

public record InvoiceDto(
    string InvoiceNumber,
    string OrderId,
    decimal ProductCost,
    decimal ServiceFee,
    decimal DeliveryFee,
    decimal OtherCharges,
    decimal TotalAmount,
    string Currency,
    DateTime IssuedAt);
