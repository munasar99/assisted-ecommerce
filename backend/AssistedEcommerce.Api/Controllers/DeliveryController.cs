using AssistedEcommerce.Api.Extensions;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Exceptions;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/delivery/zones")]
public class DeliveryController(IDeliveryService deliveryService) : ControllerBase
{
    /// <summary>GET list (admin) — GET /api/delivery/zones</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeliveryZoneDto>>>> GetAll(CancellationToken ct)
    {
        var zones = await deliveryService.GetAllZonesAsync(ct);
        return Ok(new ApiResponse<IReadOnlyList<DeliveryZoneDto>>(true, zones));
    }

    /// <summary>GET active (public) — GET /api/delivery/zones/active</summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeliveryZoneDto>>>> GetActive(CancellationToken ct)
    {
        var zones = await deliveryService.GetActiveZonesAsync(ct);
        return Ok(new ApiResponse<IReadOnlyList<DeliveryZoneDto>>(true, zones));
    }

    /// <summary>GET one (admin) — GET /api/delivery/zones/{id}</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<DeliveryZoneDto>>> GetById(string id, CancellationToken ct)
    {
        var zone = await deliveryService.GetZoneByIdAsync(id, ct);
        return Ok(new ApiResponse<DeliveryZoneDto>(true, zone));
    }

    /// <summary>POST create (admin) — POST /api/delivery/zones</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<DeliveryZoneDto>>> Create(
        [FromBody] CreateDeliveryZoneRequest request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var zone = await deliveryService.CreateZoneAsync(request, adminId, ct);
        return CreatedAtAction(nameof(GetById), new { id = zone.ZoneId }, new ApiResponse<DeliveryZoneDto>(true, zone));
    }

    /// <summary>PUT update (admin) — PUT /api/delivery/zones/{id}</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<DeliveryZoneDto>>> Update(
        string id, [FromBody] UpdateDeliveryZoneRequest? request, CancellationToken ct)
    {
        if (request is null)
            throw new ApiException("Request body is required.");
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var zone = await deliveryService.UpdateZoneAsync(id, request, adminId, ct);
        return Ok(new ApiResponse<DeliveryZoneDto>(true, zone));
    }

    /// <summary>DELETE (admin) — DELETE /api/delivery/zones/{id}</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        await deliveryService.DeleteZoneAsync(id, adminId, ct);
        return Ok(new ApiResponse<object>(true, null, "Zone deleted."));
    }

    [HttpPut("{id}/fee")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<DeliveryZoneDto>>> UpdateFee(
        string id, [FromBody] UpdateDeliveryFeeRequest request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var zone = await deliveryService.UpdateFeeAsync(id, request.FeeUsd, adminId, ct);
        return Ok(new ApiResponse<DeliveryZoneDto>(true, zone));
    }

    [HttpPatch("{id}/toggle")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<DeliveryZoneDto>>> Toggle(string id, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var zone = await deliveryService.ToggleZoneAsync(id, adminId, ct);
        return Ok(new ApiResponse<DeliveryZoneDto>(true, zone));
    }

    private string? TryGetAdminId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        try { return User.GetAdminId(); }
        catch { return null; }
    }
}
