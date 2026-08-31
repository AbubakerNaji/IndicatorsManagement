using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/indicators")]
[Authorize]
public class IndicatorsController : ControllerBase
{
    private readonly IIndicatorService _indicatorService;

    public IndicatorsController(IIndicatorService indicatorService)
    {
        _indicatorService = indicatorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetIndicators([FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _indicatorService.GetIndicatorsAsync(isActive, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetIndicator(int id)
    {
        var result = await _indicatorService.GetIndicatorByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> CreateIndicator([FromBody] CreateIndicatorRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _indicatorService.CreateIndicatorAsync(request, userId);
        if (!result.Success)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetIndicator), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> UpdateIndicator(int id, [FromBody] UpdateIndicatorRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _indicatorService.UpdateIndicatorAsync(id, request, userId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public async Task<IActionResult> DeleteIndicator(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _indicatorService.DeleteIndicatorAsync(id, userId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    // --- Dimension Management ---

    [HttpPost("{indicatorId:int}/dimensions")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> AddDimension(int indicatorId, [FromBody] CreateDimensionRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _indicatorService.AddDimensionAsync(indicatorId, request, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("dimensions/{dimensionId:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> UpdateDimension(int dimensionId, [FromBody] CreateDimensionRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _indicatorService.UpdateDimensionAsync(dimensionId, request, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("dimensions/{dimensionId:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> DeleteDimension(int dimensionId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _indicatorService.DeleteDimensionAsync(dimensionId, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
