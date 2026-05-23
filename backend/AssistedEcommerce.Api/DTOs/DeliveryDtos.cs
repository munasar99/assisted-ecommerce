namespace AssistedEcommerce.Api.DTOs;

public record DeliveryZoneDto(
    string Id,
    string ZoneId,
    string DistrictName,
    string DistrictNameEn,
    decimal FeeUsd,
    bool IsActive,
    int SortOrder);

public record UpdateDeliveryFeeRequest(decimal FeeUsd);

public record CreateDeliveryZoneRequest(
    string ZoneId,
    string DistrictName,
    string DistrictNameEn,
    decimal FeeUsd,
    bool IsActive = true,
    int SortOrder = 0);

/// <summary>PUT /api/delivery/zones/{id} — dhammaan beeraha ama qayb ka mid ah.</summary>
public class UpdateDeliveryZoneRequest
{
    public string? DistrictName { get; set; }
    public string? DistrictNameEn { get; set; }
    public decimal? FeeUsd { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}
