using System.Security.Claims;

namespace AssistedEcommerce.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetAdminId(this ClaimsPrincipal user) =>
        user.FindFirst("sub")?.Value
        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Admin id not found in token.");
}
