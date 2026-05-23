using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.Models;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Infrastructure;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        await CreateIndexesAsync(db);

        if (!await db.DeliveryZones.Find(_ => true).AnyAsync())
        {
            var docs = MogadishuDistricts.All.Select(z => new DeliveryZone
            {
                ZoneId = MogadishuDistricts.ToZoneId(z.En),
                DistrictName = z.So,
                DistrictNameEn = z.En,
                FeeUsd = z.FeeUsd,
                IsActive = true,
                SortOrder = z.SortOrder,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            await db.DeliveryZones.InsertManyAsync(docs);
            logger.LogInformation("Seeded {Count} delivery zones.", docs.Count);
        }

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@assisted.local";
        var existingAdmin = await db.Admins.Find(a => a.Email == adminEmail).FirstOrDefaultAsync();
        if (existingAdmin == null)
        {
            var idGen = scope.ServiceProvider.GetRequiredService<IIdGeneratorService>();
            var password = config["Seed:AdminPassword"] ?? "Admin@123";
            var admin = new Admin
            {
                AdminId = await idGen.NextAdminIdAsync(),
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = config["Seed:AdminName"] ?? "System Admin",
                Role = Constants.Roles.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await db.Admins.InsertOneAsync(admin);
            logger.LogInformation("Seeded admin {Email}. Dev password from Seed:AdminPassword.", adminEmail);
        }
    }

    private static async Task CreateIndexesAsync(MongoDbContext db)
    {
        await db.Admins.Indexes.CreateOneAsync(
            new CreateIndexModel<Admin>(Builders<Admin>.IndexKeys.Ascending(a => a.Email),
                new CreateIndexOptions { Unique = true }));

        await db.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Phone),
                new CreateIndexOptions { Unique = true }));
        await db.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.UserId),
                new CreateIndexOptions { Unique = true }));

        await db.Orders.Indexes.CreateOneAsync(
            new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(o => o.OrderId),
                new CreateIndexOptions { Unique = true }));
        await db.Orders.Indexes.CreateOneAsync(
            new CreateIndexModel<Order>(Builders<Order>.IndexKeys
                .Ascending(o => o.Status)
                .Descending(o => o.CreatedAt)));

        await db.Invoices.Indexes.CreateOneAsync(
            new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(i => i.InvoiceNumber),
                new CreateIndexOptions { Unique = true }));

        await db.DeliveryZones.Indexes.CreateOneAsync(
            new CreateIndexModel<DeliveryZone>(Builders<DeliveryZone>.IndexKeys.Ascending(z => z.ZoneId),
                new CreateIndexOptions { Unique = true }));
    }
}
