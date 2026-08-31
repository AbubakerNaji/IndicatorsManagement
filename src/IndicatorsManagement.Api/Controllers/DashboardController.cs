using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int UserEntityId => int.TryParse(User.FindFirstValue("EntityId"), out var eid) ? eid : 0;

    [HttpGet("ministry")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> GetMinistryDashboard()
    {
        var result = await _dashboardService.GetMinistryDashboardAsync();
        return Ok(result);
    }

    [HttpGet("entity/{id:int}")]
    public async Task<IActionResult> GetEntityDashboard(int id)
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var result = await _dashboardService.GetEntityDashboardAsync(id, UserEntityId, userRole);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetUserTasks()
    {
        var result = await _dashboardService.GetUserTasksAsync(UserId, UserEntityId);
        return Ok(result);
    }
}
