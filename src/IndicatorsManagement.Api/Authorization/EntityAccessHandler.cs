using System.Security.Claims;
using IndicatorsManagement.Contracts.Constants;
using Microsoft.AspNetCore.Authorization;

namespace IndicatorsManagement.Api.Authorization;

/// <summary>
/// Handles entity-based authorization: users can only access their own entity's data
/// unless they hold a ministry-level or super-admin role.
/// </summary>
public class EntityAccessHandler : AuthorizationHandler<EntityAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EntityAccessHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EntityAccessRequirement requirement)
    {
        var user = context.User;

        // Super_Admin and Ministry_Admin can access all entities
        if (user.IsInRole(Roles.SuperAdmin) || user.IsInRole(Roles.MinistryAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // For entity-scoped roles, check if the requested entityId matches the user's entity
        var userEntityClaim = user.FindFirstValue("EntityId");
        if (string.IsNullOrEmpty(userEntityClaim))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Check route value for entityId
        var routeEntityId = httpContext.Request.RouteValues["entityId"]?.ToString()
            ?? httpContext.Request.Query["entityId"].FirstOrDefault();

        // If no entityId in request, succeed (the service layer will scope by user's entity)
        if (string.IsNullOrEmpty(routeEntityId))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (routeEntityId == userEntityClaim)
            context.Succeed(requirement);
        else
            context.Fail();

        return Task.CompletedTask;
    }
}
