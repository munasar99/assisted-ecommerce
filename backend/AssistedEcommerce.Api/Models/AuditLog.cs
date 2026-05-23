using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssistedEcommerce.Api.Models;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("adminId")]
    public string? AdminId { get; set; }

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("entity")]
    public string Entity { get; set; } = string.Empty;

    [BsonElement("entityId")]
    public string? EntityId { get; set; }

    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
