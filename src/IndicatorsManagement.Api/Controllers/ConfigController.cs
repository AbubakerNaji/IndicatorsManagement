using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/config")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
public class ConfigController : ControllerBase
{
    private readonly ISystemConfigurationService _configService;

    public ConfigController(ISystemConfigurationService configService)
    {
        _configService = configService;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var result = await _configService.GetAllConfigAsync();
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateConfigRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _configService.UpdateConfigAsync(request.Key, request.Value, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
