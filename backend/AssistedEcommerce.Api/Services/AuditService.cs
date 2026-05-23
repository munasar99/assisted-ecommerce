using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using MongoDB.Bson;

namespace AssistedEcommerce.Api.Services;

public interface IAuditService
{
    Task LogAsync(string? adminId, string action, string entity, string? entityId, object? metadata = null, string? ip = null);
}

public class AuditService(MongoDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task LogAsync(string? adminId, string action, string entity, string? entityId, object? metadata = null, string? ip = null)
    {
        BsonDocument? metaDoc = null;
        if (metadata is not null)
            metaDoc = BsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(metadata));

        var log = new AuditLog
        {
            AdminId = adminId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Metadata = metaDoc,
            IpAddress = ip ?? httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        await db.AuditLogs.InsertOneAsync(log);
    }
}
