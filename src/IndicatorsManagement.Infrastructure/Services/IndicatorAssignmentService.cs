using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class IndicatorAssignmentService : IIndicatorAssignmentService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;

    public IndicatorAssignmentService(IndicatorsDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<PaginatedResponse<AssignmentResponse>>> GetAssignmentsAsync(
        int? indicatorId = null, int? entityId = null, int page = 1, int pageSize = 20)
    {
        var query = _db.IndicatorAssignments.AsNoTracking()
            .Include(a => a.Indicator)
            .Include(a => a.Entity)
            .AsQueryable();

        if (indicatorId.HasValue) query = query.Where(a => a.IndicatorId == indicatorId.Value);
        if (entityId.HasValue) query = query.Where(a => a.EntityId == entityId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AssignmentResponse
            {
                Id = a.Id,
                IndicatorId = a.IndicatorId,
                IndicatorNameAr = a.Indicator.NameAr,
                IndicatorCode = a.Indicator.Code,
                EntityId = a.EntityId,
                EntityNameAr = a.Entity.NameAr,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                ReportingFrequency = a.ReportingFrequency,
                IsActive = a.IsActive
            }).ToListAsync();

        return ApiResponse<PaginatedResponse<AssignmentResponse>>.Ok(new PaginatedResponse<AssignmentResponse>
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }

    public async Task<ApiResponse<AssignmentResponse>> CreateAssignmentAsync(CreateAssignmentRequest request, int creatorUserId)
    {
        if (!await _db.Indicators.AnyAsync(i => i.Id == request.IndicatorId))
            return ApiResponse<AssignmentResponse>.Fail("المؤشر غير موجود");
        if (!await _db.Entities.AnyAsync(e => e.Id == request.EntityId))
            return ApiResponse<AssignmentResponse>.Fail("الجهة غير موجودة");

        var assignment = new IndicatorAssignment
        {
            IndicatorId = request.IndicatorId,
            EntityId = request.EntityId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ReportingFrequency = request.ReportingFrequency,
            IsActive = true
        };

        _db.IndicatorAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        // Auto-generate obligations for matching reporting periods
        await GenerateObligationsAsync(assignment.Id, creatorUserId);

        await _audit.LogAsync(creatorUserId, "IndicatorAssignment", assignment.Id, "Create_Assignment",
            newValues: JsonSerializer.Serialize(new { request.IndicatorId, request.EntityId, request.ReportingFrequency }));

        // Reload with navigation properties
        var created = await _db.IndicatorAssignments.AsNoTracking()
            .Include(a => a.Indicator).Include(a => a.Entity)
            .FirstAsync(a => a.Id == assignment.Id);

        return ApiResponse<AssignmentResponse>.Ok(new AssignmentResponse
        {
            Id = created.Id,
            IndicatorId = created.IndicatorId,
            IndicatorNameAr = created.Indicator.NameAr,
            IndicatorCode = created.Indicator.Code,
            EntityId = created.EntityId,
            EntityNameAr = created.Entity.NameAr,
            StartDate = created.StartDate,
            EndDate = created.EndDate,
            ReportingFrequency = created.ReportingFrequency,
            IsActive = created.IsActive
        }, "تم إنشاء التكليف بنجاح");
    }

    public async Task<ApiResponse<AssignmentResponse>> UpdateAssignmentAsync(int id, UpdateAssignmentRequest request, int updaterUserId)
    {
        var assignment = await _db.IndicatorAssignments
            .Include(a => a.Indicator).Include(a => a.Entity)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment is null)
            return ApiResponse<AssignmentResponse>.Fail("التكليف غير موجود");

        assignment.EndDate = request.EndDate;
        assignment.ReportingFrequency = request.ReportingFrequency;
        assignment.IsActive = request.IsActive;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(updaterUserId, "IndicatorAssignment", id, "Update_Assignment");

        return ApiResponse<AssignmentResponse>.Ok(new AssignmentResponse
        {
            Id = assignment.Id,
            IndicatorId = assignment.IndicatorId,
            IndicatorNameAr = assignment.Indicator.NameAr,
            IndicatorCode = assignment.Indicator.Code,
            EntityId = assignment.EntityId,
            EntityNameAr = assignment.Entity.NameAr,
            StartDate = assignment.StartDate,
            EndDate = assignment.EndDate,
            ReportingFrequency = assignment.ReportingFrequency,
            IsActive = assignment.IsActive
        }, "تم تحديث التكليف بنجاح");
    }

    public async Task<ApiResponse<int>> GenerateObligationsAsync(int assignmentId, int userId)
    {
        var assignment = await _db.IndicatorAssignments.FindAsync(assignmentId);
        if (assignment is null)
            return ApiResponse<int>.Fail("التكليف غير موجود");

        // Get matching reporting periods
        var periods = await _db.ReportingPeriods
            .Where(p => p.PeriodType == assignment.ReportingFrequency
                && p.StartDate >= assignment.StartDate
                && (assignment.EndDate == null || p.EndDate <= assignment.EndDate))
            .ToListAsync();

        var existingPeriodIds = await _db.SubmissionObligations
            .Where(o => o.IndicatorAssignmentId == assignmentId)
            .Select(o => o.ReportingPeriodId)
            .ToListAsync();

        var newObligations = periods
            .Where(p => !existingPeriodIds.Contains(p.Id))
            .Select(p => new SubmissionObligation
            {
                IndicatorAssignmentId = assignmentId,
                ReportingPeriodId = p.Id,
                DueDate = p.EndDate.AddDays(30), // Default: 30 days after period end
                Status = ObligationStatus.Not_Started
            }).ToList();

        if (newObligations.Count == 0)
            return ApiResponse<int>.Ok(0, "جميع الالتزامات موجودة مسبقاً");

        _db.SubmissionObligations.AddRange(newObligations);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "SubmissionObligation", assignmentId, "Generate_Obligations",
            newValues: $"Generated {newObligations.Count} obligations");

        return ApiResponse<int>.Ok(newObligations.Count, $"تم إنشاء {newObligations.Count} التزام إبلاغ بنجاح");
    }
}
