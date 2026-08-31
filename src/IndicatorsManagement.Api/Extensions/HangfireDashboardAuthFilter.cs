using System.Security.Claims;
using Hangfire.Dashboard;
using IndicatorsManagement.Contracts.Constants;

namespace IndicatorsManagement.Api.Extensions;

public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
            return false;

        return user.IsInRole(Roles.SuperAdmin);
    }
}
