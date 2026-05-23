using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssistedEcommerce.Api.Models;

public class Invoice
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("productCost")]
    public decimal ProductCost { get; set; }

    [BsonElement("serviceFee")]
    public decimal ServiceFee { get; set; }

    [BsonElement("deliveryFee")]
    public decimal DeliveryFee { get; set; }

    [BsonElement("otherCharges")]
    public decimal OtherCharges { get; set; }

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("issuedAt")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("issuedBy")]
    public string IssuedBy { get; set; } = string.Empty;
}
