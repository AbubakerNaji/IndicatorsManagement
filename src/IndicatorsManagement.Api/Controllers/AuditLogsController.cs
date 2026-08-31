using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Auditor}")]
public class AuditLogsController : ControllerBase
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _auditService;

    public AuditLogsController(IndicatorsDbContext db, IAuditLogService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int? userId, [FromQuery] string? actionType, [FromQuery] string? entityType,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrEmpty(actionType)) query = query.Where(a => a.ActionType == actionType);
        if (!string.IsNullOrEmpty(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new
            {
                a.Id, a.UserId, a.EntityType, a.EntityId, a.ActionType,
                a.ResultStatus, a.ErrorCode, a.ErrorMessage,
                a.OldValuesJson, a.NewValuesJson, a.IpAddress, a.CreatedAt
            }).ToListAsync();

        return Ok(ApiResponse<object>.Ok(new PaginatedResponse<object>
        {
            Items = items.Cast<object>().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        }));
    }

    [HttpGet("entity/{entityType}/{entityId:int}")]
    public async Task<IActionResult> GetEntityAuditTrail(string entityType, int entityId)
    {
        var logs = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new
            {
                a.Id, a.UserId, a.ActionType, a.ResultStatus,
                a.OldValuesJson, a.NewValuesJson, a.CreatedAt
            }).ToListAsync();

        return Ok(ApiResponse<object>.Ok(logs));
    }

    // S10 — verify the tamper-evident hash chain. Returns 200 with IsValid=true when
    // every row's hash matches its contents and its predecessor's hash.
    [HttpGet("verify-chain")]
    public async Task<IActionResult> VerifyChain()
    {
        var report = await _auditService.VerifyChainAsync();
        return Ok(ApiResponse<AuditChainVerification>.Ok(report,
            report.IsValid ? "سلسلة السجل سليمة" : "تم اكتشاف تلاعب في سجل التدقيق"));
    }
}
