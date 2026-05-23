using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Exceptions;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Services;

public interface IDeliveryService
{
    Task<IReadOnlyList<DeliveryZoneDto>> GetAllZonesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryZoneDto>> GetActiveZonesAsync(CancellationToken ct = default);
    Task<DeliveryZoneDto> GetZoneByIdAsync(string zoneId, CancellationToken ct = default);
    Task<DeliveryZoneDto> CreateZoneAsync(CreateDeliveryZoneRequest request, string adminId, CancellationToken ct = default);
    Task<DeliveryZoneDto> UpdateZoneAsync(string zoneId, UpdateDeliveryZoneRequest request, string adminId, CancellationToken ct = default);
    Task<DeliveryZoneDto> UpdateFeeAsync(string zoneId, decimal feeUsd, string adminId, CancellationToken ct = default);
    Task<DeliveryZoneDto> ToggleZoneAsync(string zoneId, string adminId, CancellationToken ct = default);
    Task DeleteZoneAsync(string zoneId, string adminId, CancellationToken ct = default);
    Task<DeliveryZone> GetActiveZoneByZoneIdAsync(string zoneId, CancellationToken ct = default);
}

public class DeliveryService(MongoDbContext db, IAuditService auditService) : IDeliveryService
{
    public async Task<IReadOnlyList<DeliveryZoneDto>> GetAllZonesAsync(CancellationToken ct = default)
    {
        var zones = await db.DeliveryZones.Find(_ => true).SortBy(z => z.SortOrder).ToListAsync(ct);
        return zones.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DeliveryZoneDto>> GetActiveZonesAsync(CancellationToken ct = default)
    {
        var zones = await db.DeliveryZones.Find(z => z.IsActive).SortBy(z => z.SortOrder).ToListAsync(ct);
        return zones.Select(Map).ToList();
    }

    public async Task<DeliveryZone> GetActiveZoneByZoneIdAsync(string districtKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(districtKey))
            throw new ApiException("Dooro degmada liiska (districtId). Ha qorin magac qaldan sida HODON — dooro ZONE-HODAN.");

        var key = NormalizeZoneIdKey(districtKey.Trim());
        var active = await db.DeliveryZones.Find(z => z.IsActive).ToListAsync(ct);
        var zone = active.FirstOrDefault(z =>
            string.Equals(z.ZoneId, key, StringComparison.OrdinalIgnoreCase));

        return zone ?? throw new NotFoundException(
            $"Degmo '{districtKey.Trim()}' lama helin. Dooro liiska: GET /api/delivery/zones/active (tusaale ZONE-DAYNIILE, ZONE-HODAN).");
    }

    public async Task<DeliveryZoneDto> GetZoneByIdAsync(string zoneId, CancellationToken ct = default)
    {
        var zone = await FindByZoneIdOrObjectIdAsync(zoneId, ct);
        return Map(zone);
    }

    public async Task<DeliveryZoneDto> CreateZoneAsync(CreateDeliveryZoneRequest request, string adminId, CancellationToken ct = default)
    {
        var zoneId = request.ZoneId.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(zoneId))
            throw new ApiException("ZoneId is required.");

        var exists = await db.DeliveryZones.Find(z => z.ZoneId == zoneId).AnyAsync(ct);
        if (exists)
            throw new ApiException("Zone with this ID already exists.");

        if (request.FeeUsd < 0)
            throw new ApiException("Fee cannot be negative.");

        var zone = new DeliveryZone
        {
            ZoneId = zoneId,
            DistrictName = request.DistrictName.Trim(),
            DistrictNameEn = request.DistrictNameEn.Trim(),
            FeeUsd = request.FeeUsd,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = adminId
        };
        await db.DeliveryZones.InsertOneAsync(zone, cancellationToken: ct);
        await auditService.LogAsync(adminId, "CREATE_DELIVERY_ZONE", "DeliveryZones", zone.ZoneId);

        return Map(zone);
    }

    public async Task<DeliveryZoneDto> UpdateZoneAsync(
        string zoneId, UpdateDeliveryZoneRequest request, string adminId, CancellationToken ct = default)
    {
        var zone = await FindByZoneIdOrObjectIdAsync(zoneId, ct);

        if (!string.IsNullOrWhiteSpace(request.DistrictName))
            zone.DistrictName = request.DistrictName.Trim();
        if (request.DistrictNameEn is not null)
            zone.DistrictNameEn = request.DistrictNameEn.Trim();
        if (request.FeeUsd is { } fee)
        {
            if (fee < 0)
                throw new ApiException("Fee cannot be negative.");
            zone.FeeUsd = fee;
        }
        if (request.IsActive is { } active)
            zone.IsActive = active;
        if (request.SortOrder is { } sort)
            zone.SortOrder = sort;

        zone.UpdatedAt = DateTime.UtcNow;
        zone.UpdatedBy = adminId;

        await db.DeliveryZones.ReplaceOneAsync(z => z.Id == zone.Id, zone, cancellationToken: ct);
        await auditService.LogAsync(adminId, "UPDATE_DELIVERY_ZONE", "DeliveryZones", zone.ZoneId);

        return Map(zone);
    }

    public async Task DeleteZoneAsync(string zoneId, string adminId, CancellationToken ct = default)
    {
        var zone = await FindByZoneIdOrObjectIdAsync(zoneId, ct);
        var orderCount = await db.Orders.CountDocumentsAsync(o => o.DistrictId == zone.ZoneId, cancellationToken: ct);
        if (orderCount > 0)
            throw new ApiException("Cannot delete zone used by orders.");

        await db.DeliveryZones.DeleteOneAsync(z => z.Id == zone.Id, ct);
        await auditService.LogAsync(adminId, "DELETE_DELIVERY_ZONE", "DeliveryZones", zone.ZoneId);
    }

    public async Task<DeliveryZoneDto> UpdateFeeAsync(string zoneId, decimal feeUsd, string adminId, CancellationToken ct = default)
    {
        if (feeUsd < 0) throw new ApiException("Fee cannot be negative.");

        var zone = await FindByZoneIdOrObjectIdAsync(zoneId, ct);
        zone.FeeUsd = feeUsd;
        zone.UpdatedAt = DateTime.UtcNow;
        zone.UpdatedBy = adminId;
        await db.DeliveryZones.ReplaceOneAsync(z => z.Id == zone.Id, zone, cancellationToken: ct);
        await auditService.LogAsync(adminId, "UPDATE_DELIVERY_FEE", "DeliveryZones", zone.ZoneId, new { feeUsd });

        return Map(zone);
    }

    public async Task<DeliveryZoneDto> ToggleZoneAsync(string zoneId, string adminId, CancellationToken ct = default)
    {
        var zone = await FindByZoneIdOrObjectIdAsync(zoneId, ct);
        zone.IsActive = !zone.IsActive;
        zone.UpdatedAt = DateTime.UtcNow;
        zone.UpdatedBy = adminId;
        await db.DeliveryZones.ReplaceOneAsync(z => z.Id == zone.Id, zone, cancellationToken: ct);
        await auditService.LogAsync(adminId, "TOGGLE_DELIVERY_ZONE", "DeliveryZones", zone.ZoneId, new { zone.IsActive });

        return Map(zone);
    }

    private async Task<DeliveryZone> FindByZoneIdOrObjectIdAsync(string id, CancellationToken ct)
    {
        var key = id.Trim();
        if (string.IsNullOrEmpty(key))
            throw new NotFoundException("Delivery zone not found.");

        var zoneKey = NormalizeZoneIdKey(key);
        var byZoneId = await db.DeliveryZones
            .Find(z => z.ZoneId == zoneKey)
            .FirstOrDefaultAsync(ct);
        if (byZoneId is not null)
            return byZoneId;

        if (ObjectId.TryParse(key, out _))
        {
            var byObjectId = await db.DeliveryZones
                .Find(z => z.Id == key)
                .FirstOrDefaultAsync(ct);
            if (byObjectId is not null)
                return byObjectId;
        }

        throw new NotFoundException("Delivery zone not found.");
    }

    private static readonly Dictionary<string, string> ZoneIdAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ZONE-ABDIAZIZ"] = MogadishuDistricts.ToZoneId("Abdicasiis"),
            ["ZONE-DEYNIILE"] = MogadishuDistricts.ToZoneId("Dayniile"),
            ["ZONE-HAMAR-JAJAB"] = MogadishuDistricts.ToZoneId("Hamar-Jajab"),
            ["ZONE-HAMAR-WEYNE"] = MogadishuDistricts.ToZoneId("Hamar-Weyne"),
            ["ZONE-HAMAR JAJAB"] = MogadishuDistricts.ToZoneId("Hamar-Jajab"),
            ["ZONE-HAMAR WEYNE"] = MogadishuDistricts.ToZoneId("Hamar-Weyne"),
        };

    private static string NormalizeZoneIdKey(string zoneId)
    {
        var key = zoneId.Trim();
        if (ZoneIdAliases.TryGetValue(key, out var mapped))
            return mapped;
        return key
            .Replace("DEYNIILE", "DAYNIILE", StringComparison.OrdinalIgnoreCase)
            .Replace("ABDIAZIZ", "ABDICASIIS", StringComparison.OrdinalIgnoreCase);
    }

    private static DeliveryZoneDto Map(DeliveryZone z) =>
        new(z.Id ?? string.Empty, z.ZoneId, z.DistrictName, z.DistrictNameEn, z.FeeUsd, z.IsActive, z.SortOrder);
}
