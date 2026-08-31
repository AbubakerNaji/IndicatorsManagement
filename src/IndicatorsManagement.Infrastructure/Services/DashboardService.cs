using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IndicatorsDbContext _db;

    public DashboardService(IndicatorsDbContext db) => _db = db;

    public async Task<ApiResponse<MinistryDashboardResponse>> GetMinistryDashboardAsync()
    {
        var totalIndicators = await _db.Indicators.CountAsync(i => i.IsActive);
        var totalEntities = await _db.Entities.CountAsync(e => e.Status == "active");
        var totalSubmissions = await _db.IndicatorEntries.CountAsync(e => !e.IsDeleted);
        var overdueCount = await _db.SubmissionObligations.CountAsync(o => o.Status == ObligationStatus.Overdue);

        // Submission rate by entity
        var entityRates = await _db.Entities.Where(e => e.Status == "active")
            .Select(e => new EntitySubmissionRate
            {
                EntityId = e.Id,
                EntityNameAr = e.NameAr,
                TotalObligations = _db.SubmissionObligations.Count(o => o.IndicatorAssignment.EntityId == e.Id),
                SubmittedCount = _db.SubmissionObligations.Count(o =>
                    o.IndicatorAssignment.EntityId == e.Id &&
                    (o.Status == ObligationStatus.Submitted || o.Status == ObligationStatus.Approved))
            }).ToListAsync();

        foreach (var r in entityRates)
            r.SubmissionRate = r.TotalObligations > 0 ? Math.Round((decimal)r.SubmittedCount / r.TotalObligations * 100, 1) : 0;

        // Workflow state distribution
        var distribution = await _db.IndicatorEntries
            .Where(e => !e.IsDeleted)
            .GroupBy(e => e.WorkflowState)
            .Select(g => new { State = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.State, x => x.Count);

        return ApiResponse<MinistryDashboardResponse>.Ok(new MinistryDashboardResponse
        {
            TotalIndicators = totalIndicators,
            TotalEntities = totalEntities,
            TotalSubmissions = totalSubmissions,
            OverdueCount = overdueCount,
            SubmissionsByEntity = entityRates,
            WorkflowDistribution = distribution
        });
    }

    public async Task<ApiResponse<EntityDashboardResponse>> GetEntityDashboardAsync(int entityId, int userEntityId, string userRole)
    {
        // S5 — an entity-scoped user cannot see other entities' dashboards.
        if (userRole is Roles.DataEntryUser or Roles.EntityAdmin or Roles.Reviewer
            && entityId != userEntityId)
        {
            return ApiResponse<EntityDashboardResponse>.Fail("الجهة غير موجودة");
        }

        var entity = await _db.Entities.FindAsync(entityId);
        if (entity is null) return ApiResponse<EntityDashboardResponse>.Fail("الجهة غير موجودة");

        var assignedIndicators = await _db.IndicatorAssignments.CountAsync(a => a.EntityId == entityId && a.IsActive);
        var obligations = await _db.SubmissionObligations
            .Where(o => o.IndicatorAssignment.EntityId == entityId)
            .ToListAsync();

        return ApiResponse<EntityDashboardResponse>.Ok(new EntityDashboardResponse
        {
            EntityId = entityId,
            EntityNameAr = entity.NameAr,
            AssignedIndicators = assignedIndicators,
            SubmittedCount = obligations.Count(o => o.Status == ObligationStatus.Submitted),
            PendingCount = obligations.Count(o => o.Status == ObligationStatus.Not_Started || o.Status == ObligationStatus.In_Progress),
            OverdueCount = obligations.Count(o => o.Status == ObligationStatus.Overdue),
            ApprovedCount = obligations.Count(o => o.Status == ObligationStatus.Approved)
        });
    }

    public async Task<ApiResponse<List<TaskItemResponse>>> GetUserTasksAsync(int userId, int entityId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekEnd = today.AddDays(7);
        var monthEnd = today.AddDays(30);

        var obligations = await _db.SubmissionObligations
            .Include(o => o.IndicatorAssignment).ThenInclude(a => a.Indicator)
            .Include(o => o.ReportingPeriod)
            .Where(o => o.IndicatorAssignment.EntityId == entityId && o.IndicatorAssignment.IsActive)
            .ToListAsync();

        var tasks = new List<TaskItemResponse>();

        foreach (var o in obligations)
        {
            // Find matching entry
            var entry = await _db.IndicatorEntries
                .Where(e => e.IndicatorId == o.IndicatorAssignment.IndicatorId
                    && e.EntityId == entityId
                    && e.ReportingPeriodId == o.ReportingPeriodId
                    && !e.IsDeleted)
                .FirstOrDefaultAsync();

            string priority;
            if (o.Status == ObligationStatus.Approved)
                priority = "completed";
            else if (o.DueDate < today)
                priority = "overdue";
            else if (o.DueDate <= weekEnd)
                priority = "due_this_week";
            else if (o.DueDate <= monthEnd)
                priority = "due_this_month";
            else
                continue; // Skip far-future obligations

            tasks.Add(new TaskItemResponse
            {
                ObligationId = o.Id,
                IndicatorId = o.IndicatorAssignment.IndicatorId,
                IndicatorNameAr = o.IndicatorAssignment.Indicator.NameAr,
                IndicatorCode = o.IndicatorAssignment.Indicator.Code,
                ReportingPeriodId = o.ReportingPeriodId,
                PeriodDisplayNameAr = o.ReportingPeriod.DisplayNameAr,
                DueDate = o.DueDate,
                Status = o.Status,
                EntryId = entry?.Id,
                EntryWorkflowState = entry?.WorkflowState,
                Priority = priority
            });
        }

        return ApiResponse<List<TaskItemResponse>>.Ok(
            tasks.OrderBy(t => t.Priority == "overdue" ? 0 : t.Priority == "due_this_week" ? 1 : t.Priority == "due_this_month" ? 2 : 3)
                 .ThenBy(t => t.DueDate)
                 .ToList());
    }
}
