using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Infrastructure;

public interface IMongoDbConnectionChecker
{
    Task<MongoConnectionResult> TestAsync(CancellationToken ct = default);
}

public record MongoConnectionResult(bool Connected, string DatabaseName, string ConnectionString, IReadOnlyList<string> Collections, string? Error = null);

public class MongoDbConnectionChecker(MongoDbContext db, IOptions<MongoDbSettings> settings) : IMongoDbConnectionChecker
{
    public async Task<MongoConnectionResult> TestAsync(CancellationToken ct = default)
    {
        var cfg = settings.Value;
        try
        {
            await db.DeliveryZones.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: ct);

            var collections = await db.DeliveryZones.Database.ListCollectionNames().ToListAsync(ct);
            return new MongoConnectionResult(true, cfg.DatabaseName, MaskConnection(cfg.ConnectionString), collections);
        }
        catch (Exception ex)
        {
            return new MongoConnectionResult(false, cfg.DatabaseName, MaskConnection(cfg.ConnectionString), [], ex.Message);
        }
    }

    private static string MaskConnection(string cs) =>
        cs.Contains('@') ? cs[..cs.IndexOf('@')] + "@***" : cs;
}
