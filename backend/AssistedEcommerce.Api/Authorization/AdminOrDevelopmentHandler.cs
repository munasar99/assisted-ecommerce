using AssistedEcommerce.Api.Constants;
using Microsoft.AspNetCore.Authorization;

namespace AssistedEcommerce.Api.Authorization;

/// <summary>Development: admin endpoints without JWT. Production: requires admin role.</summary>
public class AdminOrDevelopmentRequirement : IAuthorizationRequirement;

public class AdminOrDevelopmentHandler(IWebHostEnvironment env) : AuthorizationHandler<AdminOrDevelopmentRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOrDevelopmentRequirement requirement)
    {
        if (env.IsDevelopment())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(Roles.Admin))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
