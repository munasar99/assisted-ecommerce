using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssistedEcommerce.Api.Models;

public class DeliveryZone
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("zoneId")]
    public string ZoneId { get; set; } = string.Empty;

    [BsonElement("districtName")]
    public string DistrictName { get; set; } = string.Empty;

    [BsonElement("districtNameEn")]
    public string DistrictNameEn { get; set; } = string.Empty;

    [BsonElement("feeUsd")]
    public decimal FeeUsd { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedBy")]
    public string? UpdatedBy { get; set; }
}
