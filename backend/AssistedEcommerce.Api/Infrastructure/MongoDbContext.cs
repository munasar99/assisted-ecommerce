using AssistedEcommerce.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Infrastructure;

public class MongoDbContext
{
    private readonly IMongoDatabase _db;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        Settings = settings.Value;
        var client = new MongoClient(Settings.ConnectionString);
        _db = client.GetDatabase(Settings.DatabaseName);
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
