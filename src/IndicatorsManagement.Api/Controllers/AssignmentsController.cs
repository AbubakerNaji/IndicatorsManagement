using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/indicator-assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IIndicatorAssignmentService _assignmentService;

    public AssignmentsController(IIndicatorAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAssignments([FromQuery] int? indicatorId, [FromQuery] int? entityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _assignmentService.GetAssignmentsAsync(indicatorId, entityId, page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _assignmentService.CreateAssignmentAsync(request, userId);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetAssignments), result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateAssignmentRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _assignmentService.UpdateAssignmentAsync(id, request, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{id:int}/generate-obligations")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> GenerateObligations(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _assignmentService.GenerateObligationsAsync(id, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
