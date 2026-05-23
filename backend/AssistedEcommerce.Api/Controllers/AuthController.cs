using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.Extensions;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(new ApiResponse<LoginResponse>(true, result));
    }

    [HttpPost("logout")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken ct)
    {
        var adminId = User.GetAdminId();
        await authService.LogoutAsync(adminId, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(new ApiResponse<object>(true, null, "Logged out."));
    }

    [HttpGet("profile")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<AdminProfileDto>>> Profile(CancellationToken ct)
    {
        var adminId = User.GetAdminId();
        var profile = await authService.GetProfileAsync(adminId, ct);
        return Ok(new ApiResponse<AdminProfileDto>(true, profile));
    }
}
