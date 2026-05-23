using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadController(IUploadService uploadService) : ControllerBase
{
    [HttpPost("order")]
    [AllowAnonymous]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UploadResponse>>> UploadOrder(
        IFormFile file, [FromForm] string? orderId = null, CancellationToken ct = default)
    {
        var result = await uploadService.UploadOrderScreenshotAsync(file, orderId, ct);
        return Ok(new ApiResponse<UploadResponse>(true, result));
    }
}
