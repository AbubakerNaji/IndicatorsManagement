using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/reporting-periods")]
[Authorize]
public class ReportingPeriodsController : ControllerBase
{
    private readonly IReportingPeriodService _periodService;

    public ReportingPeriodsController(IReportingPeriodService periodService)
    {
        _periodService = periodService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPeriods([FromQuery] PeriodType? periodType, [FromQuery] int? year)
    {
        var result = await _periodService.GetPeriodsAsync(periodType, year);
        return Ok(result);
    }

    [HttpPost("generate")]
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public async Task<IActionResult> GeneratePeriods([FromBody] GenerateReportingPeriodsRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _periodService.GeneratePeriodsAsync(request, userId);
        return Ok(result);
    }
}
