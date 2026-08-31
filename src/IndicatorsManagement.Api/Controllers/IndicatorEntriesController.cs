using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/indicator-entries")]
[Authorize]
public class IndicatorEntriesController : ControllerBase
{
    private readonly IIndicatorEntryService _entryService;

    public IndicatorEntriesController(IIndicatorEntryService entryService)
    {
        _entryService = entryService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int UserEntityId => int.TryParse(User.FindFirstValue("EntityId"), out var eid) ? eid : 0;
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetEntries([FromQuery] int? entityId, [FromQuery] int? indicatorId, [FromQuery] int? periodId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userRole = UserRole;
        var publishedOnly = userRole == Roles.Viewer;

        // Entity-scoped users can only see their own entity's entries
        if (userRole is Roles.DataEntryUser or Roles.EntityAdmin or Roles.Reviewer)
            entityId = UserEntityId;

        var result = await _entryService.GetEntriesAsync(entityId, indicatorId, periodId, page, pageSize, publishedOnly);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEntry(int id)
    {
        var result = await _entryService.GetEntryByIdAsync(id, UserEntityId, UserRole);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.EntityAdmin},{Roles.DataEntryUser}")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateIndicatorEntryRequest request)
    {
        var result = await _entryService.CreateEntryAsync(request, UserId, UserEntityId);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetEntry), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{entryId:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.EntityAdmin},{Roles.DataEntryUser}")]
    public async Task<IActionResult> UpdateEntry(int entryId, [FromBody] UpdateIndicatorEntryRequest request)
    {
        var result = await _entryService.UpdateEntryAsync(entryId, request, UserId, UserEntityId, UserRole);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{entryId:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.EntityAdmin},{Roles.DataEntryUser}")]
    public async Task<IActionResult> SoftDeleteEntry(int entryId)
    {
        var result = await _entryService.SoftDeleteEntryAsync(entryId, UserId, UserEntityId, UserRole);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{entryId:int}/submit")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.EntityAdmin},{Roles.DataEntryUser}")]
    public async Task<IActionResult> SubmitEntry(int entryId)
    {
        var result = await _entryService.SubmitEntryAsync(entryId, UserId, UserEntityId, UserRole);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{entryId:int}/approve-entity")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.EntityAdmin},{Roles.Reviewer}")]
    public async Task<IActionResult> ApproveEntityLevel(int entryId, [FromBody] WorkflowActionRequest? request)
    {
        var result = await _entryService.ApproveEntityLevelAsync(entryId, UserId, UserEntityId, UserRole, request?.Notes);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{entryId:int}/approve-ministry")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> ApproveMinistryLevel(int entryId, [FromBody] WorkflowActionRequest? request)
    {
        var result = await _entryService.ApproveMinistryLevelAsync(entryId, UserId, request?.Notes);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{entryId:int}/reject")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin},{Roles.EntityAdmin},{Roles.Reviewer}")]
    public async Task<IActionResult> RejectEntry(int entryId, [FromBody] WorkflowActionRequest request)
    {
        var result = await _entryService.RejectEntryAsync(entryId, UserId, UserEntityId, UserRole, request.Notes ?? "");
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{entryId:int}/return")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin},{Roles.EntityAdmin},{Roles.Reviewer}")]
    public async Task<IActionResult> ReturnEntry(int entryId, [FromBody] WorkflowActionRequest request)
    {
        var result = await _entryService.ReturnEntryAsync(entryId, UserId, UserEntityId, UserRole, request.Notes ?? "");
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
