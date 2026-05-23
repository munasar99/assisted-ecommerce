using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssistedEcommerce.Api.Models;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("customerFullName")]
    public string? CustomerFullName { get; set; }

    [BsonElement("customerPhone")]
    public string? CustomerPhone { get; set; }

    [BsonElement("customerEmail")]
    public string? CustomerEmail { get; set; }

    [BsonElement("productUrl")]
    public string ProductUrl { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string? ProductName { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("deliveryType")]
    public string DeliveryType { get; set; } = string.Empty;

    [BsonElement("districtId")]
    public string DistrictId { get; set; } = string.Empty;

    [BsonElement("districtName")]
    public string DistrictName { get; set; } = string.Empty;

    [BsonElement("addressDetail")]
    public string AddressDetail { get; set; } = string.Empty;

    [BsonElement("deliveryFee")]
    public decimal DeliveryFee { get; set; }

    [BsonElement("productUnitPriceUsd")]
    public decimal ProductUnitPriceUsd { get; set; }

    [BsonElement("productSubtotalUsd")]
    public decimal ProductSubtotalUsd { get; set; }

    [BsonElement("serviceFeeUsd")]
    public decimal ServiceFeeUsd { get; set; }

    [BsonElement("totalAmountUsd")]
    public decimal TotalAmountUsd { get; set; }

    [BsonElement("payerPhone")]
    public string? PayerPhone { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = Constants.OrderStatuses.Pending;

    [BsonElement("orderScreenshotUrl")]
    public string? OrderScreenshotUrl { get; set; }

    [BsonElement("paymentScreenshotUrl")]
    public string? PaymentScreenshotUrl { get; set; }

    [BsonElement("invoiceId")]
    public string? InvoiceId { get; set; }

    [BsonElement("statusHistory")]
    public List<StatusHistoryEntry> StatusHistory { get; set; } = [];

    [BsonElement("formStartedAtUtc")]
    public DateTime? FormStartedAtUtc { get; set; }

    [BsonElement("submittedAtUtc")]
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Marka user-ku iska diiwaangashay — kala sooc dashboard (cusub → duug).</summary>
    [BsonElement("userRegisteredAt")]
    public DateTime UserRegisteredAt { get; set; } = DateTime.UtcNow;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
