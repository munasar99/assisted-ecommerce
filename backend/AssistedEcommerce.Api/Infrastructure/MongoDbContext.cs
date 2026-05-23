using AssistedEcommerce.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Infrastructure;

public class MongoDbContext
{
    private readonly IMongoDatabase _db;

    public MongoDbContext(IOptions<MongoDbSettings> settings, IConfiguration configuration)
    {
        var bound = settings.Value;
        var conn = ConfigEnvironment.ResolveConnectionString(bound, configuration);
        var dbName = string.IsNullOrWhiteSpace(bound.DatabaseName) ? "ubaxsana" : bound.DatabaseName;

        Settings = new MongoDbSettings { ConnectionString = conn, DatabaseName = dbName };

        if (string.IsNullOrWhiteSpace(conn))
        {
            throw new InvalidOperationException(
                "MongoDB ma configured. Railway: geli MONGODB_URI (mongodb+srv://...) kadib redeploy.");
        }

        var client = new MongoClient(conn);
        _db = client.GetDatabase(dbName);
    }

    /// <summary>MongoDB database name (e.g. ubaxsana). All API saves go here permanently.</summary>
    public MongoDbSettings Settings { get; }
    public string DatabaseName => Settings.DatabaseName;
    public IMongoDatabase Database => _db;

    public IMongoCollection<Admin> Admins => _db.GetCollection<Admin>("Admins");
    public IMongoCollection<User> Users => _db.GetCollection<User>("Users");
    public IMongoCollection<Order> Orders => _db.GetCollection<Order>("Orders");
    public IMongoCollection<Invoice> Invoices => _db.GetCollection<Invoice>("Invoices");
    public IMongoCollection<Payment> Payments => _db.GetCollection<Payment>("Payments");
    public IMongoCollection<DeliveryZone> DeliveryZones => _db.GetCollection<DeliveryZone>("DeliveryZones");
    public IMongoCollection<AuditLog> AuditLogs => _db.GetCollection<AuditLog>("AuditLogs");
    public IMongoCollection<Counter> Counters => _db.GetCollection<Counter>("Counters");
}
