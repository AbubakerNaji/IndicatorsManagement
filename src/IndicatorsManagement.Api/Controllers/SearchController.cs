using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(Contracts.Responses.ApiResponse.Fail("عبارة البحث مطلوبة"));

        // Entity-scoped users only see their entity's results
        int? entityId = null;
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole is Roles.DataEntryUser or Roles.EntityAdmin or Roles.Reviewer)
        {
            var eid = User.FindFirstValue("EntityId");
            if (eid != null) entityId = int.Parse(eid);
        }

        var result = await _searchService.SearchAsync(q, entityId, page, pageSize);
        return Ok(result);
    }
}
