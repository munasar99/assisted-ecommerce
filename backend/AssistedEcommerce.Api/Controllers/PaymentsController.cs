using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Extensions;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    /// <summary>GET list — MongoDB Payments collection</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? orderId = null,
        CancellationToken ct = default)
    {
        var result = await paymentService.GetPaymentsAsync(page, pageSize, status, orderId, ct);
        return Ok(new ApiResponse<PagedResult<PaymentDto>>(true, result));
    }

    /// <summary>GET one — GET /api/payments/PAY-0001</summary>
    [HttpGet("{paymentId}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetById(string paymentId, CancellationToken ct)
    {
        var result = await paymentService.GetPaymentByIdAsync(paymentId, ct);
        return Ok(new ApiResponse<PaymentDto>(true, result));
    }

    /// <summary>POST create (admin) — keydin MongoDB</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Create(
        [FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var result = await paymentService.CreatePaymentAsync(request, adminId, ct);
        return CreatedAtAction(nameof(GetById), new { paymentId = result.PaymentId },
            new ApiResponse<PaymentDto>(true, result));
    }

    /// <summary>PUT update — beddel status Confirm/Reject</summary>
    [HttpPut("{paymentId}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Update(
        string paymentId, [FromBody] UpdatePaymentRequest request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var result = await paymentService.UpdatePaymentAsync(paymentId, request, adminId, ct);
        return Ok(new ApiResponse<PaymentDto>(true, result));
    }

    /// <summary>DELETE — tirtir payment record</summary>
    [HttpDelete("{paymentId}")]
    [Authorize(Policy = "AdminOrDevelopment")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string paymentId, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        await paymentService.DeletePaymentAsync(paymentId, adminId, ct);
        return Ok(new ApiResponse<object>(true, null, "Payment deleted."));
    }

    /// <summary>POST upload screenshot (macmiil — public)</summary>
    [HttpPost("upload")]
    [AllowAnonymous]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<PaymentUploadResponse>>> Upload(
        [FromForm] string orderId,
        [FromForm] string phone,
        [FromForm] string payerPhone,
        [FromForm] string? paymentMethod,
        IFormFile file,
        CancellationToken ct)
    {
        var result = await paymentService.UploadPaymentAsync(orderId, phone, payerPhone, paymentMethod, file, ct);
        return Ok(new ApiResponse<PaymentUploadResponse>(true, result));
    }

    private string? TryGetAdminId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        try { return User.GetAdminId(); }
        catch { return null; }
    }
}
