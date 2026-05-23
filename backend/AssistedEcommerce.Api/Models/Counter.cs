using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssistedEcommerce.Api.Models;

public class Counter
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("seq")]
    public long Seq { get; set; }
}
