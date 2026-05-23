using AssistedEcommerce.Api.Extensions;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOrDevelopment")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await userService.GetUsersAsync(page, pageSize, search, ct);
        return Ok(new ApiResponse<PagedResult<UserDto>>(true, result));
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(string userId, CancellationToken ct)
    {
        var result = await userService.GetUserByIdAsync(userId, ct);
        return Ok(new ApiResponse<UserDto>(true, result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var result = await userService.CreateUserAsync(request, adminId, ct);
        return CreatedAtAction(nameof(GetById), new { userId = result.UserId }, new ApiResponse<UserDto>(true, result));
    }

    [HttpPut("{userId}")]
    [Consumes("application/json")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        string userId, [FromBody] UpdateUserRequest? body, CancellationToken ct)
    {
        if (body is null)
            return BadRequest(new ApiResponse<object>(false, null, "JSON body required. Example: { \"fullName\": \"Ali\", \"phone\": \"+252612345678\", \"status\": \"active\" }"));

        var adminId = TryGetAdminId() ?? "dev-api-test";
        var result = await userService.UpdateUserAsync(userId, body, adminId, ct);
        return Ok(new ApiResponse<UserDto>(true, result));
    }

    [HttpDelete("{userId}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string userId, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        await userService.DeleteUserAsync(userId, adminId, ct);
        return Ok(new ApiResponse<object>(true, null, "User deleted."));
    }

    [HttpPatch("{userId}/status")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateStatus(
        string userId, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        var adminId = TryGetAdminId() ?? "dev-api-test";
        var result = await userService.UpdateUserStatusAsync(userId, request.Status, adminId, ct);
        return Ok(new ApiResponse<UserDto>(true, result));
    }

    private string? TryGetAdminId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        try { return User.GetAdminId(); }
        catch { return null; }
    }
}
