using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/entities")]
[Authorize]
public class EntitiesController : ControllerBase
{
    private readonly IEntityService _entityService;

    public EntitiesController(IEntityService entityService)
    {
        _entityService = entityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetEntities()
    {
        var result = await _entityService.GetEntitiesAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEntity(int id)
    {
        var result = await _entityService.GetEntityByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public async Task<IActionResult> CreateEntity([FromBody] CreateEntityRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _entityService.CreateEntityAsync(request, userId);
        if (!result.Success)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetEntity), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public async Task<IActionResult> UpdateEntity(int id, [FromBody] UpdateEntityRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _entityService.UpdateEntityAsync(id, request, userId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public async Task<IActionResult> DeactivateEntity(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _entityService.DeactivateEntityAsync(id, userId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
