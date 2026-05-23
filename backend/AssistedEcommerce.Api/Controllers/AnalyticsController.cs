using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = Roles.Admin)]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardAnalyticsDto>>> Dashboard(CancellationToken ct)
    {
        var data = await analyticsService.GetDashboardAsync(ct);
        return Ok(new ApiResponse<DashboardAnalyticsDto>(true, data));
    }
}
