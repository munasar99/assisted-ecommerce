using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.Models;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Infrastructure;

public class DatabaseSeeder(
    MongoDbContext db,
    IIdGeneratorService idGenerator,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var dbName = configuration["MongoDb:DatabaseName"] ?? "ubaxsana";
        logger.LogInformation("========================================");
        logger.LogInformation("MongoDB: connecting to database '{Database}'...", dbName);

        try
        {
            await db.DeliveryZones.Database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
                new MongoDB.Bson.BsonDocument("ping", 1), cancellationToken: ct);
            logger.LogInformation("MongoDB: PING OK — connected to '{Database}'", dbName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "MongoDB: CONNECTION FAILED. Hubi connection string (Atlas: mongodb+srv://...) iyo IP Access 0.0.0.0/0.");
            throw;
        }

        await EnsureIndexesAsync(ct);
        logger.LogInformation("MongoDB: indexes ensured.");

        await NormalizeAndMergeDuplicateUsersAsync(ct);
        await MigratePickupLabelsAsync(ct);
        await BackfillOrderUserRegisteredAtAsync(ct);

        if (configuration.GetValue<bool>("Seed:ResetTransactionalDataOnStartup"))
            await ResetTransactionalDataAsync(ct);

        await SeedAdminAsync(ct);
        await SeedDeliveryZonesAsync(ct);

        var collections = await db.DeliveryZones.Database.ListCollectionNames().ToListAsync(ct);
        logger.LogInformation("MongoDB: collections in '{Database}': {Collections}", dbName, string.Join(", ", collections));
        logger.LogInformation("========================================");
    }

    private async Task BackfillOrderUserRegisteredAtAsync(CancellationToken ct)
    {
        var users = await db.Users.Find(_ => true).ToListAsync(ct);
        var userMap = users.ToDictionary(u => u.UserId, u => u.CreatedAt);

        foreach (var user in users)
        {
            var filter = Builders<Order>.Filter.Eq(o => o.UserId, user.UserId);
            var update = Builders<Order>.Update.Set(o => o.UserRegisteredAt, user.CreatedAt);
            await db.Orders.UpdateManyAsync(filter, update, cancellationToken: ct);
        }

        var orphanOrders = await db.Orders
            .Find(o => o.UserRegisteredAt == default)
            .ToListAsync(ct);
        foreach (var order in orphanOrders)
        {
            var at = userMap.GetValueOrDefault(order.UserId, order.CreatedAt);
            await db.Orders.UpdateOneAsync(
                o => o.Id == order.Id,
                Builders<Order>.Update.Set(o => o.UserRegisteredAt, at),
                cancellationToken: ct);
        }
    }

    private async Task MigratePickupLabelsAsync(CancellationToken ct)
    {
        const string label = DeliveryTypes.PickupDistrictName;
        var legacy = new[] { "Tabo — soo qaado", "Tabo — pick up the items", "Tabo" };

        foreach (var old in legacy)
        {
            var nameFilter = Builders<Order>.Filter.Eq(o => o.DistrictName, old);
            await db.Orders.UpdateManyAsync(
                nameFilter,
                Builders<Order>.Update.Set(o => o.DistrictName, label),
                cancellationToken: ct);

            var addrFilter = Builders<Order>.Filter.Eq(o => o.AddressDetail, old);
            await db.Orders.UpdateManyAsync(
                addrFilter,
                Builders<Order>.Update.Set(o => o.AddressDetail, label),
                cancellationToken: ct);
        }
    }

    private async Task EnsureIndexesAsync(CancellationToken ct)
    {
        await TryCreateIndexAsync(db.Admins,
            new CreateIndexModel<Admin>(Builders<Admin>.IndexKeys.Ascending(a => a.Email),
                new CreateIndexOptions { Unique = true }), ct);
        await TryCreateIndexAsync(db.Users,
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Phone),
                new CreateIndexOptions { Unique = true }), ct);
        await TryCreateIndexAsync(db.Users,
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.UserId),
                new CreateIndexOptions { Unique = true }), ct);
        await TryCreateIndexAsync(db.Orders,
            new CreateIndexModel<Order>(Builders<Order>.IndexKeys.Ascending(o => o.OrderId),
                new CreateIndexOptions { Unique = true }), ct);
        await TryCreateIndexAsync(db.Invoices,
            new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(i => i.InvoiceNumber),
                new CreateIndexOptions { Unique = true }), ct);
        await TryCreateIndexAsync(db.DeliveryZones,
            new CreateIndexModel<DeliveryZone>(Builders<DeliveryZone>.IndexKeys.Ascending(z => z.ZoneId),
                new CreateIndexOptions { Unique = true }), ct);
        await TryCreateIndexAsync(db.Payments,
            new CreateIndexModel<Payment>(Builders<Payment>.IndexKeys.Ascending(p => p.PaymentId),
                new CreateIndexOptions { Unique = true }), ct);
        await TryCreateIndexAsync(db.Payments,
            new CreateIndexModel<Payment>(Builders<Payment>.IndexKeys.Ascending(p => p.OrderId)), ct);
    }

    /// <summary>Telefoon hal qaab (+252…) + isku dar users isku telefoon (duplicate).</summary>
    private async Task NormalizeAndMergeDuplicateUsersAsync(CancellationToken ct)
    {
        var all = await db.Users.Find(FilterDefinition<User>.Empty).ToListAsync(ct);
        if (all.Count == 0)
            return;

        var merged = 0;
        var normalized = 0;

        var groups = all
            .GroupBy(u => PhoneNormalizer.CanonicalKey(u.Phone))
            .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1);

        foreach (var g in groups)
        {
            var ordered = g.OrderBy(u => u.CreatedAt).ThenBy(u => u.UserId).ToList();
            var primary = ordered[0];
            foreach (var dup in ordered.Skip(1))
            {
                await db.Orders.UpdateManyAsync(
                    o => o.UserId == dup.UserId,
                    Builders<Order>.Update
                        .Set(o => o.UserId, primary.UserId)
                        .Set(o => o.CustomerPhone, primary.Phone),
                    cancellationToken: ct);

                await db.Invoices.UpdateManyAsync(
                    i => i.UserId == dup.UserId,
                    Builders<Invoice>.Update.Set(i => i.UserId, primary.UserId),
                    cancellationToken: ct);

                var dupOrders = dup.TotalOrders;
                if (dupOrders > 0)
                {
                    await db.Users.UpdateOneAsync(
                        u => u.Id == primary.Id,
                        Builders<User>.Update
                            .Inc(u => u.TotalOrders, dupOrders)
                            .Set(u => u.UpdatedAt, DateTime.UtcNow),
                        cancellationToken: ct);
                }

                await db.Users.DeleteOneAsync(u => u.Id == dup.Id, ct);
                merged++;
            }

            if (!string.IsNullOrWhiteSpace(primary.FullName))
            {
                var bestName = ordered
                    .Select(x => x.FullName)
                    .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? primary.FullName;
                if (primary.FullName != bestName)
                {
                    primary.FullName = bestName;
                    primary.UpdatedAt = DateTime.UtcNow;
                    await db.Users.ReplaceOneAsync(u => u.Id == primary.Id, primary, cancellationToken: ct);
                }
            }
        }

        all = await db.Users.Find(FilterDefinition<User>.Empty).ToListAsync(ct);
        foreach (var u in all)
        {
            var canonical = PhoneNormalizer.Normalize(u.Phone);
            if (string.IsNullOrEmpty(canonical) || u.Phone == canonical)
                continue;
            u.Phone = canonical;
            u.UpdatedAt = DateTime.UtcNow;
            await db.Users.ReplaceOneAsync(x => x.Id == u.Id, u, cancellationToken: ct);
            normalized++;
        }

        if (normalized > 0 || merged > 0)
            logger.LogInformation("Users: {Merged} duplicate accounts merged, {Normalized} phones normalized.", merged, normalized);
    }

    private async Task TryCreateIndexAsync<T>(IMongoCollection<T> collection, CreateIndexModel<T> model, CancellationToken ct)
    {
        try
        {
            await collection.Indexes.CreateOneAsync(model, cancellationToken: ct);
        }
        catch (MongoCommandException ex) when (IsIndexAlreadyExists(ex))
        {
            logger.LogDebug("Index skip (already exists): {Collection}", collection.CollectionNamespace.CollectionName);
        }
    }

    private static bool IsIndexAlreadyExists(MongoCommandException ex)
    {
        if (ex.Code is 85 or 86)
            return true;
        var m = ex.Message ?? "";
        return m.Contains("already exists", StringComparison.OrdinalIgnoreCase)
               || m.Contains("IndexOptionsConflict", StringComparison.OrdinalIgnoreCase)
               || m.Contains("same name", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Tirtir users/orders/payments — IDs dib u bilaab USR-001, ORD-001-01. Admin + 18 degmo waa la hayaa.</summary>
    public async Task ResetTransactionalDataAsync(CancellationToken ct = default)
    {
        var users = await db.Users.DeleteManyAsync(FilterDefinition<User>.Empty, ct);
        var orders = await db.Orders.DeleteManyAsync(FilterDefinition<Order>.Empty, ct);
        var invoices = await db.Invoices.DeleteManyAsync(FilterDefinition<Invoice>.Empty, ct);
        var payments = await db.Payments.DeleteManyAsync(FilterDefinition<Payment>.Empty, ct);
        var audits = await db.AuditLogs.DeleteManyAsync(FilterDefinition<AuditLog>.Empty, ct);
        var counters = await db.Counters.DeleteManyAsync(FilterDefinition<Counter>.Empty, ct);

        logger.LogWarning(
            "DATABASE RESET: users={Users}, orders={Orders}, invoices={Invoices}, payments={Payments}, counters={Counters}. " +
            "Next IDs: 01, order1.",
            users.DeletedCount, orders.DeletedCount, invoices.DeletedCount, payments.DeletedCount, counters.DeletedCount);
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var email = (configuration["Seed:AdminEmail"] ?? "admin@assisted.local").Trim().ToLowerInvariant();
        var password = configuration["Seed:AdminPassword"] ?? "Admin@123";
        var name = (configuration["Seed:AdminName"] ?? "System Admin").Trim();

        var exists = await db.Admins.Find(a => a.Email == email).AnyAsync(ct);
        if (exists) return;

        var admin = new Admin
        {
            AdminId = await idGenerator.NextAdminIdAsync(ct),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = name,
            Role = Roles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await db.Admins.InsertOneAsync(admin, cancellationToken: ct);
        logger.LogWarning("Seeded admin {Email}. Change password in production!", email);
    }

    private async Task SeedDeliveryZonesAsync(CancellationToken ct)
    {
        var inserted = 0;
        var reactivated = 0;

        foreach (var z in MogadishuDistricts.All)
        {
            var zoneId = MogadishuDistricts.ToZoneId(z.En);
            var existing = await db.DeliveryZones.Find(d => d.ZoneId == zoneId).FirstOrDefaultAsync(ct);

            if (existing is null)
            {
                await db.DeliveryZones.InsertOneAsync(new DeliveryZone
                {
                    ZoneId = zoneId,
                    DistrictName = z.So,
                    DistrictNameEn = z.En,
                    FeeUsd = z.FeeUsd,
                    IsActive = true,
                    SortOrder = z.SortOrder,
                    UpdatedAt = DateTime.UtcNow
                }, cancellationToken: ct);
                inserted++;
                continue;
            }

            if (!existing.IsActive)
            {
                await db.DeliveryZones.UpdateOneAsync(
                    d => d.Id == existing.Id,
                    Builders<DeliveryZone>.Update
                        .Set(d => d.IsActive, true)
                        .Set(d => d.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);
                reactivated++;
            }
        }

        var active = await db.DeliveryZones.CountDocumentsAsync(
            Builders<DeliveryZone>.Filter.Eq(z => z.IsActive, true), cancellationToken: ct);
        var total = await db.DeliveryZones.CountDocumentsAsync(FilterDefinition<DeliveryZone>.Empty, cancellationToken: ct);
        logger.LogInformation(
            "Delivery zones: {Active} active / {Total} total ({Inserted} new, {Reactivated} reactivated).",
            active, total, inserted, reactivated);
    }
}
