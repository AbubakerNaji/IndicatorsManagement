using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/validation-rules")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
public class ValidationRulesController : ControllerBase
{
    private readonly IValidationRuleService _ruleService;

    public ValidationRulesController(IValidationRuleService ruleService)
    {
        _ruleService = ruleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRules([FromQuery] int indicatorId)
    {
        var result = await _ruleService.GetRulesByIndicatorAsync(indicatorId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateValidationRuleRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ruleService.CreateRuleAsync(request, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] CreateValidationRuleRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ruleService.UpdateRuleAsync(id, request, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ruleService.DeleteRuleAsync(id, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
