using AssistedEcommerce.Api.Constants;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssistedEcommerce.Api.Models;

public class Payment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("paymentId")]
    public string PaymentId { get; set; } = string.Empty;

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("payerPhone")]
    public string? PayerPhone { get; set; }

    [BsonElement("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [BsonElement("screenshotUrl")]
    public string? ScreenshotUrl { get; set; }

    [BsonElement("amountUsd")]
    public decimal AmountUsd { get; set; }

    [BsonElement("screenshotDetectedAmountUsd")]
    public decimal? ScreenshotDetectedAmountUsd { get; set; }

    [BsonElement("screenshotDetectedAtUtc")]
    public DateTime? ScreenshotDetectedAtUtc { get; set; }

    [BsonElement("screenshotVerificationNote")]
    public string? ScreenshotVerificationNote { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = PaymentStatuses.UnderReview;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
