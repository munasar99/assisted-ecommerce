using AssistedEcommerce.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Infrastructure;

public class MongoDbContext
{
    private readonly Lazy<IMongoDatabase> _db;

    public MongoDbContext(IOptions<MongoDbSettings> settings, IConfiguration configuration)
    {
        var bound = settings.Value;
        var conn = ConfigEnvironment.ResolveConnectionString(bound, configuration);
        var dbName = string.IsNullOrWhiteSpace(bound.DatabaseName) ? "ubaxsana" : bound.DatabaseName;
        Settings = new MongoDbSettings { ConnectionString = conn, DatabaseName = dbName };

        _db = new Lazy<IMongoDatabase>(() =>
        {
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException(
                    "MongoDB ma configured. Railway: geli MONGODB_URI kadib redeploy.");
            return new MongoClient(conn).GetDatabase(dbName);
        });
    }

    public MongoDbSettings Settings { get; }
    public string DatabaseName => Settings.DatabaseName;
    public IMongoDatabase Database => _db.Value;

    public IMongoCollection<Admin> Admins => Database.GetCollection<Admin>("Admins");
    public IMongoCollection<User> Users => Database.GetCollection<User>("Users");
    public IMongoCollection<Order> Orders => Database.GetCollection<Order>("Orders");
    public IMongoCollection<Invoice> Invoices => Database.GetCollection<Invoice>("Invoices");
    public IMongoCollection<Payment> Payments => Database.GetCollection<Payment>("Payments");
    public IMongoCollection<DeliveryZone> DeliveryZones => Database.GetCollection<DeliveryZone>("DeliveryZones");
    public IMongoCollection<AuditLog> AuditLogs => Database.GetCollection<AuditLog>("AuditLogs");
    public IMongoCollection<Counter> Counters => Database.GetCollection<Counter>("Counters");
}
