using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class PublicationController : ControllerBase
{
    private readonly IPublicationService _publicationService;

    public PublicationController(IPublicationService publicationService)
    {
        _publicationService = publicationService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("indicator-entries/{entryId:int}/publish")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> PublishEntry(int entryId, [FromBody] PublicationActionRequest? request)
    {
        var result = await _publicationService.PublishEntryAsync(entryId, UserId, request?.Reason);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("indicator-entries/{entryId:int}/unpublish")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> UnpublishEntry(int entryId, [FromBody] PublicationActionRequest? request)
    {
        var result = await _publicationService.UnpublishEntryAsync(entryId, UserId, request?.Reason);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("indicator-entries/bulk-publish")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> BulkPublish([FromBody] BulkPublicationRequest request)
    {
        var result = await _publicationService.BulkPublishAsync(request.EntryIds, UserId, request.Reason);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("indicator-entries/bulk-unpublish")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> BulkUnpublish([FromBody] BulkPublicationRequest request)
    {
        var result = await _publicationService.BulkUnpublishAsync(request.EntryIds, UserId, request.Reason);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("indicator-entries/{entryId:int}/publication-history")]
    public async Task<IActionResult> GetPublicationHistory(int entryId)
    {
        var result = await _publicationService.GetPublicationHistoryAsync(entryId);
        return Ok(result);
    }

    [HttpGet("publication/statistics")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.MinistryAdmin}")]
    public async Task<IActionResult> GetPublicationStatistics()
    {
        var result = await _publicationService.GetPublicationStatisticsAsync();
        return Ok(result);
    }
}

public class PublicationActionRequest
{
    public string? Reason { get; set; }
}

public class BulkPublicationRequest
{
    public List<int> EntryIds { get; set; } = [];
    public string? Reason { get; set; }
}
