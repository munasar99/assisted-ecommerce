using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.Extensions;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>GET list — GET /api/orders?page=1 (dev: no token; production: Bearer admin)</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderListItemDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? district = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await orderService.GetOrdersAsync(page, pageSize, status, district, search, ct);
        return Ok(new ApiResponse<PagedResult<OrderListItemDto>>(true, result));
    }

    /// <summary>GET track (public) — GET /api/orders/track?orderId=&amp;phone=</summary>
    [HttpGet("track")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> Track(
        [FromQuery] string orderId, [FromQuery] string phone, CancellationToken ct)
    {
        var result = await orderService.TrackOrderAsync(orderId, phone, ct);
        return Ok(new ApiResponse<OrderDetailDto>(true, result));
    }

    /// <summary>GET one — GET /api/orders/ORD-001-01 (ha isticmaalin {orderId} qoraal ahaan)</summary>
    [HttpGet("{orderId}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetById(string orderId, CancellationToken ct)
    {
        var result = await orderService.GetOrderDetailAsync(orderId, ct);
        return Ok(new ApiResponse<OrderDetailDto>(true, result));
    }

    /// <summary>POST create (public)</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CreateOrderResponse>>> Create(
        [FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orderService.CreateOrderAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { orderId = result.OrderId }, new ApiResponse<CreateOrderResponse>(true, result));
    }

    /// <summary>PUT update — PUT /api/orders/ORD-001-01 + JSON body</summary>
    [HttpPut("{orderId}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> Update(
        string orderId, [FromBody] UpdateOrderRequest? request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var result = await orderService.UpdateOrderAsync(orderId, request ?? new UpdateOrderRequest(), adminId, ct);
        return Ok(new ApiResponse<OrderDetailDto>(true, result));
    }

    /// <summary>DELETE — DELETE /api/orders/ORD-001-01</summary>
    [HttpDelete("{orderId}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string orderId, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        await orderService.DeleteOrderAsync(orderId, adminId, ct);
        return Ok(new ApiResponse<object>(true, null, "Order deleted."));
    }

    [HttpPatch("{orderId}/status")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> UpdateStatus(
        string orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var result = await orderService.UpdateStatusAsync(orderId, request, adminId, ct);
        return Ok(new ApiResponse<OrderDetailDto>(true, result));
    }

    [HttpPost("{orderId}/invoice")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice(
        string orderId, [FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var result = await orderService.CreateInvoiceAsync(orderId, request, adminId, ct);
        return Ok(new ApiResponse<InvoiceDto>(true, result));
    }

    private string? TryGetAdminId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        try { return User.GetAdminId(); }
        catch { return null; }
    }
}
