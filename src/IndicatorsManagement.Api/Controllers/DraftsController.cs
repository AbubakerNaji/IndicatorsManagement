using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/drafts")]
[Authorize]
public class DraftsController : ControllerBase
{
    private readonly IDraftRecoveryService _draftService;

    public DraftsController(IDraftRecoveryService draftService)
    {
        _draftService = draftService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int UserEntityId => int.TryParse(User.FindFirstValue("EntityId"), out var eid) ? eid : 0;

    [HttpPost("save")]
    public async Task<IActionResult> SaveDraft([FromBody] SaveDraftRequest request)
    {
        var result = await _draftService.SaveDraftAsync(UserId, request.IndicatorId, UserEntityId, request.ReportingPeriodId, request.DraftDataJson);
        return Ok(result);
    }

    [HttpGet("recover")]
    public async Task<IActionResult> GetRecoverableDrafts()
    {
        var result = await _draftService.GetRecoverableDraftsAsync(UserId);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DiscardDraft(int id)
    {
        var result = await _draftService.DiscardDraftAsync(id, UserId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

public class SaveDraftRequest
{
    public int IndicatorId { get; set; }
    public int ReportingPeriodId { get; set; }
    public string DraftDataJson { get; set; } = string.Empty;
}
