using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class IndicatorEntryService : IIndicatorEntryService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly INotificationService _notification;

    // S5 — roles that are entity-scoped. Everyone else (Super_Admin, Ministry_Admin, Auditor)
    // sees all rows. Viewer only sees Published rows, which is enforced by the GetEntries filter.
    private static readonly HashSet<string> EntityScopedRoles = new()
    {
        Roles.DataEntryUser, Roles.EntityAdmin, Roles.Reviewer
    };

    private static bool IsEntityScoped(string role) => EntityScopedRoles.Contains(role);

    public IndicatorEntryService(IndicatorsDbContext db, IAuditLogService audit, INotificationService notification)
    {
        _db = db;
        _audit = audit;
        _notification = notification;
    }

    public async Task<ApiResponse<PaginatedResponse<IndicatorEntryResponse>>> GetEntriesAsync(
        int? entityId = null, int? indicatorId = null, int? periodId = null, int page = 1, int pageSize = 20, bool publishedOnly = false)
    {
        var query = _db.IndicatorEntries.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Include(e => e.Indicator)
            .Include(e => e.Entity)
            .Include(e => e.ReportingPeriod)
            .Include(e => e.EnteredByUser)
            .Include(e => e.EntryDimensions).ThenInclude(d => d.Dimension)
            .Include(e => e.EntryDimensions).ThenInclude(d => d.DimensionValue)
            .Include(e => e.Attachments.Where(a => !a.IsDeleted))
            .AsQueryable();

        if (publishedOnly) query = query.Where(e => e.PublicationStatus == Domain.Enums.PublicationStatus.Published);
        if (entityId.HasValue) query = query.Where(e => e.EntityId == entityId.Value);
        if (indicatorId.HasValue) query = query.Where(e => e.IndicatorId == indicatorId.Value);
        if (periodId.HasValue) query = query.Where(e => e.ReportingPeriodId == periodId.Value);

        var totalCount = await query.CountAsync();
        var entries = await query
            .OrderByDescending(e => e.EnteredAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var items = entries.Select(MapToResponse).ToList();

        return ApiResponse<PaginatedResponse<IndicatorEntryResponse>>.Ok(new PaginatedResponse<IndicatorEntryResponse>
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }

    public async Task<ApiResponse<IndicatorEntryResponse>> GetEntryByIdAsync(int id, int userEntityId, string userRole)
    {
        var entry = await LoadEntry(id);
        if (entry is null || entry.IsDeleted)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        // S5 — cross-entity access returns "not found" (never "forbidden") to avoid leaking id existence.
        if (IsEntityScoped(userRole) && entry.EntityId != userEntityId)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        // Viewer role only sees published rows.
        if (userRole == Roles.Viewer && entry.PublicationStatus != PublicationStatus.Published)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(entry));
    }

    public async Task<ApiResponse<IndicatorEntryResponse>> CreateEntryAsync(
        CreateIndicatorEntryRequest request, int userId, int userEntityId)
    {
        // Validate entity is active
        var entity = await _db.Entities.FirstOrDefaultAsync(e => e.Id == userEntityId);
        if (entity is null || entity.Status != "active")
            return ApiResponse<IndicatorEntryResponse>.Fail("الجهة معطّلة ولا يمكن إنشاء إدخالات جديدة لها");

        // B2 — reporting period must be open.
        var period = await _db.ReportingPeriods.FirstOrDefaultAsync(p => p.Id == request.ReportingPeriodId);
        if (period is null)
            return ApiResponse<IndicatorEntryResponse>.Fail("فترة الإبلاغ غير موجودة");
        if (!period.IsOpen)
            return ApiResponse<IndicatorEntryResponse>.Fail("فترة الإبلاغ مغلقة ولا يمكن إنشاء إدخال جديد فيها");

        // Validate active assignment exists
        var hasAssignment = await _db.IndicatorAssignments.AnyAsync(a =>
            a.IndicatorId == request.IndicatorId && a.EntityId == userEntityId && a.IsActive
            && a.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow)
            && (a.EndDate == null || a.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow)));

        if (!hasAssignment)
            return ApiResponse<IndicatorEntryResponse>.Fail("لا يوجد تكليف فعّال لهذا المؤشر للجهة الخاصة بك");

        // Validate no active entry exists for same indicator/entity/period
        var existingActive = await _db.IndicatorEntries.AnyAsync(e =>
            e.IndicatorId == request.IndicatorId && e.EntityId == userEntityId
            && e.ReportingPeriodId == request.ReportingPeriodId
            && !e.IsDeleted && e.WorkflowState != WorkflowState.Rejected);

        if (existingActive)
            return ApiResponse<IndicatorEntryResponse>.Fail("يوجد إدخال فعّال لهذا المؤشر في هذه الفترة");

        // Load indicator with its dimensions and dimension values for full validation.
        var indicator = await _db.Indicators
            .Include(i => i.Dimensions).ThenInclude(d => d.Values)
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId);

        if (indicator is null)
            return ApiResponse<IndicatorEntryResponse>.Fail("المؤشر غير موجود");

        // B3 — verify EVERY mandatory dimension is supplied, and each (DimensionId, DimensionValueId)
        // pair actually belongs to this indicator.
        var dimensionCheck = ValidateDimensions(indicator, request.Dimensions);
        if (dimensionCheck is not null)
            return ApiResponse<IndicatorEntryResponse>.Fail(dimensionCheck);

        var entry = new IndicatorEntry
        {
            IndicatorId = request.IndicatorId,
            EntityId = userEntityId,
            ReportingPeriodId = request.ReportingPeriodId,
            ValueNumeric = request.ValueNumeric,
            ValueText = request.ValueText,
            UnitSnapshot = indicator.UnitAr,
            Notes = request.Notes,
            Source = request.Source,
            WorkflowState = WorkflowState.Draft,
            PublicationStatus = PublicationStatus.Unpublished,
            VersionNo = 1,
            EnteredBy = userId,
            EnteredAt = DateTime.UtcNow
        };

        // Add dimensions
        if (request.Dimensions is { Count: > 0 })
        {
            foreach (var dimReq in request.Dimensions)
            {
                entry.EntryDimensions.Add(new IndicatorEntryDimension
                {
                    DimensionId = dimReq.DimensionId,
                    DimensionValueId = dimReq.DimensionValueId,
                    ValueNumeric = dimReq.ValueNumeric
                });
            }
        }

        _db.IndicatorEntries.Add(entry);
        await _db.SaveChangesAsync();

        // Update obligation status
        await UpdateObligationStatus(request.IndicatorId, userEntityId, request.ReportingPeriodId, ObligationStatus.In_Progress);

        await _audit.LogAsync(userId, "IndicatorEntry", entry.Id, "Create_Entry",
            newValues: JsonSerializer.Serialize(new { request.IndicatorId, request.ReportingPeriodId, request.ValueNumeric }));

        var created = await LoadEntry(entry.Id);
        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(created!), "تم إنشاء الإدخال بنجاح");
    }

    public async Task<ApiResponse<IndicatorEntryResponse>> UpdateEntryAsync(int id, UpdateIndicatorEntryRequest request, int userId, int userEntityId, string userRole)
    {
        var entry = await _db.IndicatorEntries
            .Include(e => e.EntryDimensions)
            .Include(e => e.Indicator).ThenInclude(i => i.Dimensions).ThenInclude(d => d.Values)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (entry is null)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        // S5 — cross-entity update: same "not found" masking.
        if (IsEntityScoped(userRole) && entry.EntityId != userEntityId)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        if (entry.WorkflowState != WorkflowState.Draft && entry.WorkflowState != WorkflowState.Returned_For_Modification)
            return ApiResponse<IndicatorEntryResponse>.Fail("لا يمكن تعديل الإدخال في حالته الحالية");

        // B3 — dimension validation on update too.
        if (request.Dimensions is not null)
        {
            var dimensionCheck = ValidateDimensions(entry.Indicator, request.Dimensions);
            if (dimensionCheck is not null)
                return ApiResponse<IndicatorEntryResponse>.Fail(dimensionCheck);
        }

        var oldValues = JsonSerializer.Serialize(new { entry.ValueNumeric, entry.ValueText, entry.Notes, entry.Source });

        // B4 — write version_history when a submitted-then-returned entry is being changed again.
        var hasBeenSubmitted = entry.SubmittedAt.HasValue;
        if (hasBeenSubmitted)
        {
            _db.VersionHistories.Add(new VersionHistory
            {
                IndicatorEntryId = entry.Id,
                VersionNo = entry.VersionNo,
                ValueNumeric = entry.ValueNumeric,
                ValueText = entry.ValueText,
                WorkflowState = entry.WorkflowState.ToString(),
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow,
                ChangeReason = "Update after return-for-modification",
                SnapshotJson = oldValues
            });
            entry.VersionNo += 1;
        }

        entry.ValueNumeric = request.ValueNumeric;
        entry.ValueText = request.ValueText;
        entry.Notes = request.Notes;
        entry.Source = request.Source;
        entry.UpdatedAt = DateTime.UtcNow;

        // Update dimensions
        if (request.Dimensions is not null)
        {
            _db.IndicatorEntryDimensions.RemoveRange(entry.EntryDimensions);
            foreach (var dimReq in request.Dimensions)
            {
                entry.EntryDimensions.Add(new IndicatorEntryDimension
                {
                    DimensionId = dimReq.DimensionId,
                    DimensionValueId = dimReq.DimensionValueId,
                    ValueNumeric = dimReq.ValueNumeric
                });
            }
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "IndicatorEntry", id, "Update_Entry",
            oldValues: oldValues,
            newValues: JsonSerializer.Serialize(new { request.ValueNumeric, request.ValueText, entry.VersionNo }));

        var updated = await LoadEntry(id);
        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(updated!), "تم تحديث الإدخال بنجاح");
    }

    public async Task<ApiResponse> SoftDeleteEntryAsync(int id, int userId, int userEntityId, string userRole)
    {
        var entry = await _db.IndicatorEntries
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (entry is null)
            return ApiResponse.Fail("الإدخال غير موجود");

        if (IsEntityScoped(userRole) && entry.EntityId != userEntityId)
            return ApiResponse.Fail("الإدخال غير موجود");

        if (entry.WorkflowState != WorkflowState.Draft)
            return ApiResponse.Fail("لا يمكن حذف الإدخال إلا في حالة المسودة");

        entry.IsDeleted = true;
        entry.UpdatedAt = DateTime.UtcNow;

        // Cascade soft delete attachments
        foreach (var attachment in entry.Attachments)
            attachment.IsDeleted = true;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "IndicatorEntry", id, "Soft_Delete_Entry");

        return ApiResponse.Ok("تم حذف الإدخال بنجاح");
    }

    // --- Workflow Actions ---

    public async Task<ApiResponse<IndicatorEntryResponse>> SubmitEntryAsync(int id, int userId, int userEntityId, string userRole)
    {
        var entry = await _db.IndicatorEntries
            .Include(e => e.Indicator).ThenInclude(i => i.ValidationRules)
            .Include(e => e.ReportingPeriod)
            .Include(e => e.Attachments.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (entry is null)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        if (IsEntityScoped(userRole) && entry.EntityId != userEntityId)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        // B2 — reject if the period has been closed between draft and submit.
        if (!entry.ReportingPeriod.IsOpen)
            return ApiResponse<IndicatorEntryResponse>.Fail("فترة الإبلاغ مغلقة ولا يمكن إرسال الإدخال");

        if (entry.WorkflowState != WorkflowState.Draft && entry.WorkflowState != WorkflowState.Returned_For_Modification)
            return ApiResponse<IndicatorEntryResponse>.Fail("لا يمكن إرسال الإدخال في حالته الحالية");

        // Validate mandatory attachments
        var requiresAttachment = entry.Indicator.RequiresAttachment ||
            entry.Indicator.ValidationRules.Any(r => r.IsMandatoryAttachment);
        if (requiresAttachment && !entry.Attachments.Any())
            return ApiResponse<IndicatorEntryResponse>.Fail("يجب إرفاق مستند داعم قبل الإرسال");

        // Validate value against rules
        foreach (var rule in entry.Indicator.ValidationRules)
        {
            if (entry.ValueNumeric.HasValue)
            {
                if (rule.MinValue.HasValue && entry.ValueNumeric < rule.MinValue)
                    return ApiResponse<IndicatorEntryResponse>.Fail($"القيمة يجب أن تكون أكبر من أو تساوي {rule.MinValue}");
                if (rule.MaxValue.HasValue && entry.ValueNumeric > rule.MaxValue)
                    return ApiResponse<IndicatorEntryResponse>.Fail($"القيمة يجب أن تكون أقل من أو تساوي {rule.MaxValue}");
            }
            if (rule.IsMandatoryNotes && string.IsNullOrWhiteSpace(entry.Notes))
                return ApiResponse<IndicatorEntryResponse>.Fail("يجب إدخال ملاحظات قبل الإرسال");
        }

        entry.WorkflowState = WorkflowState.Under_Review;
        entry.SubmittedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Update obligation status
        await UpdateObligationStatus(entry.IndicatorId, entry.EntityId, entry.ReportingPeriodId, ObligationStatus.Submitted);

        await _audit.LogAsync(userId, "IndicatorEntry", id, "Submit_Entry",
            newValues: JsonSerializer.Serialize(new { NewState = nameof(WorkflowState.Under_Review) }));

        // Notify reviewers and entity admins
        await NotifyEntityUsersAsync(entry.EntityId, NotificationType.Workflow_Change,
            "إدخال جديد للمراجعة",
            $"تم إرسال إدخال للمؤشر {entry.Indicator.NameAr} للمراجعة",
            "IndicatorEntry", entry.Id);

        var updated = await LoadEntry(id);
        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(updated!), "تم إرسال الإدخال للمراجعة بنجاح");
    }

    public async Task<ApiResponse<IndicatorEntryResponse>> ApproveEntityLevelAsync(int id, int userId, int userEntityId, string userRole, string? notes)
    {
        var entry = await _db.IndicatorEntries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry is null) return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        if (IsEntityScoped(userRole) && entry.EntityId != userEntityId)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        if (entry.WorkflowState != WorkflowState.Under_Review)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال ليس في حالة قيد المراجعة");

        // S4 — the same user who authored an entry may not approve it.
        if (entry.EnteredBy == userId)
            return ApiResponse<IndicatorEntryResponse>.Fail("لا يمكنك اعتماد إدخال قمت بإدخاله بنفسك");

        entry.WorkflowState = WorkflowState.Approved_By_Entity;
        entry.EntityApprovedBy = userId;
        entry.EntityApprovedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "IndicatorEntry", id, "Approve_Entity_Level",
            newValues: JsonSerializer.Serialize(new { NewState = nameof(WorkflowState.Approved_By_Entity), notes }));

        // Notify data entry user + ministry admins
        await _notification.CreateNotificationAsync(entry.EnteredBy, NotificationType.Workflow_Change,
            "تم اعتماد الإدخال على مستوى الجهة", $"تم اعتماد إدخالك من الجهة وهو الآن بانتظار اعتماد الوزارة", "IndicatorEntry", entry.Id);
        await NotifyMinistryAdminsAsync("إدخال بانتظار الاعتماد النهائي", $"إدخال معتمد من الجهة بانتظار اعتمادكم", "IndicatorEntry", entry.Id);

        var updated = await LoadEntry(id);
        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(updated!), "تم اعتماد الإدخال على مستوى الجهة");
    }

    public async Task<ApiResponse<IndicatorEntryResponse>> ApproveMinistryLevelAsync(int id, int userId, string? notes)
    {
        var entry = await _db.IndicatorEntries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry is null) return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");
        if (entry.WorkflowState != WorkflowState.Approved_By_Entity)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال ليس في حالة معتمد من الجهة");

        // S4 — ministry-level approval also blocks self-approval.
        if (entry.EnteredBy == userId || entry.EntityApprovedBy == userId)
            return ApiResponse<IndicatorEntryResponse>.Fail("لا يمكنك اعتماد إدخال قمت بإدخاله أو باعتماده مسبقًا");

        entry.WorkflowState = WorkflowState.Final_Approved;
        entry.MinistryApprovedBy = userId;
        entry.MinistryApprovedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Update obligation to approved
        await UpdateObligationStatus(entry.IndicatorId, entry.EntityId, entry.ReportingPeriodId, ObligationStatus.Approved);

        await _audit.LogAsync(userId, "IndicatorEntry", id, "Approve_Ministry_Level",
            newValues: JsonSerializer.Serialize(new { NewState = nameof(WorkflowState.Final_Approved), notes }));

        // Notify data entry user + entity admins
        await _notification.CreateNotificationAsync(entry.EnteredBy, NotificationType.Workflow_Change,
            "تم الاعتماد النهائي", $"تم الاعتماد النهائي لإدخالك من الوزارة", "IndicatorEntry", entry.Id);
        await NotifyEntityUsersAsync(entry.EntityId, NotificationType.Workflow_Change,
            "اعتماد نهائي لإدخال", "تم الاعتماد النهائي لأحد الإدخالات من الوزارة", "IndicatorEntry", entry.Id);

        var updated = await LoadEntry(id);
        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(updated!), "تم الاعتماد النهائي للإدخال");
    }

    public async Task<ApiResponse<IndicatorEntryResponse>> RejectEntryAsync(int id, int userId, int userEntityId, string userRole, string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return ApiResponse<IndicatorEntryResponse>.Fail("يجب إدخال سبب الرفض");

        var entry = await _db.IndicatorEntries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry is null) return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");
        if (IsEntityScoped(userRole) && entry.EntityId != userEntityId)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");
        if (entry.WorkflowState != WorkflowState.Under_Review && entry.WorkflowState != WorkflowState.Approved_By_Entity)
            return ApiResponse<IndicatorEntryResponse>.Fail("لا يمكن رفض الإدخال في حالته الحالية");

        entry.WorkflowState = WorkflowState.Rejected;
        entry.RejectionReason = notes;
        entry.ReviewedBy = userId;
        entry.ReviewedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "IndicatorEntry", id, "Reject_Entry",
            newValues: JsonSerializer.Serialize(new { NewState = nameof(WorkflowState.Rejected), RejectionReason = notes }));

        await _notification.CreateNotificationAsync(entry.EnteredBy, NotificationType.Workflow_Change,
            "تم رفض الإدخال", $"تم رفض إدخالك. السبب: {notes}", "IndicatorEntry", entry.Id);
        await NotifyEntityUsersAsync(entry.EntityId, NotificationType.Workflow_Change,
            "رفض إدخال", $"تم رفض أحد الإدخالات. السبب: {notes}", "IndicatorEntry", entry.Id);

        var updated = await LoadEntry(id);
        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(updated!), "تم رفض الإدخال");
    }

    public async Task<ApiResponse<IndicatorEntryResponse>> ReturnEntryAsync(int id, int userId, int userEntityId, string userRole, string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return ApiResponse<IndicatorEntryResponse>.Fail("يجب إدخال سبب الإعادة");

        var entry = await _db.IndicatorEntries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry is null) return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");
        if (IsEntityScoped(userRole) && entry.EntityId != userEntityId)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");
        if (entry.WorkflowState != WorkflowState.Under_Review)
            return ApiResponse<IndicatorEntryResponse>.Fail("لا يمكن إعادة الإدخال في حالته الحالية");

        entry.WorkflowState = WorkflowState.Returned_For_Modification;
        entry.RejectionReason = notes;
        entry.ReviewedBy = userId;
        entry.ReviewedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "IndicatorEntry", id, "Return_Entry",
            newValues: JsonSerializer.Serialize(new { NewState = nameof(WorkflowState.Returned_For_Modification), ReturnReason = notes }));

        await _notification.CreateNotificationAsync(entry.EnteredBy, NotificationType.Workflow_Change,
            "تم إعادة الإدخال للتعديل", $"تم إعادة إدخالك للتعديل. السبب: {notes}", "IndicatorEntry", entry.Id);

        var updated = await LoadEntry(id);
        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(updated!), "تم إعادة الإدخال للتعديل");
    }

    // --- Helpers ---

    // B3 — thorough dimension validation used by Create and Update.
    private static string? ValidateDimensions(Indicator indicator, IReadOnlyCollection<EntryDimensionRequest>? supplied)
    {
        var mandatory = indicator.Dimensions.Where(d => d.IsMandatory).ToList();
        var suppliedList = supplied ?? new List<EntryDimensionRequest>();

        // Missing mandatory dimensions.
        foreach (var m in mandatory)
        {
            if (!suppliedList.Any(s => s.DimensionId == m.Id))
                return $"البُعد الإلزامي '{m.DimensionNameAr}' غير مُدخل";
        }

        // Every submitted dimension must belong to this indicator, and any value must be one of its values.
        foreach (var s in suppliedList)
        {
            var dim = indicator.Dimensions.FirstOrDefault(d => d.Id == s.DimensionId);
            if (dim is null)
                return "أحد الأبعاد المُدخلة لا يخص هذا المؤشر";

            if (s.DimensionValueId.HasValue)
            {
                if (dim.Values.All(v => v.Id != s.DimensionValueId.Value))
                    return $"قيمة البُعد '{dim.DimensionNameAr}' غير صالحة";
            }
        }

        return null;
    }

    private async Task<IndicatorEntry?> LoadEntry(int id)
    {
        return await _db.IndicatorEntries.AsNoTracking()
            .Include(e => e.Indicator)
            .Include(e => e.Entity)
            .Include(e => e.ReportingPeriod)
            .Include(e => e.EnteredByUser)
            .Include(e => e.EntryDimensions).ThenInclude(d => d.Dimension)
            .Include(e => e.EntryDimensions).ThenInclude(d => d.DimensionValue)
            .Include(e => e.Attachments.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    private async Task UpdateObligationStatus(int indicatorId, int entityId, int periodId, ObligationStatus status)
    {
        var obligation = await _db.SubmissionObligations
            .Include(o => o.IndicatorAssignment)
            .FirstOrDefaultAsync(o =>
                o.IndicatorAssignment.IndicatorId == indicatorId
                && o.IndicatorAssignment.EntityId == entityId
                && o.ReportingPeriodId == periodId);

        if (obligation is not null)
        {
            obligation.Status = status;
            await _db.SaveChangesAsync();
        }
    }

    private async Task NotifyEntityUsersAsync(int entityId, NotificationType type, string title, string message, string? relatedType, int? relatedId)
    {
        // P2 — was N+1; do a single insert with AddRange.
        var userIds = await _db.Users
            .Where(u => u.EntityId == entityId && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        var notifications = userIds.Select(uid => new Notification
        {
            UserId = uid,
            NotificationType = type,
            TitleAr = title,
            MessageAr = message,
            RelatedEntityType = relatedType,
            RelatedEntityId = relatedId,
            IsRead = false,
            SentAt = DateTime.UtcNow
        }).ToList();

        if (notifications.Count > 0)
        {
            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();
        }
    }

    private async Task NotifyMinistryAdminsAsync(string title, string message, string? relatedType, int? relatedId)
    {
        // P2 — bulk insert instead of one INSERT per admin.
        var adminIds = await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == Roles.MinistryAdmin || x.Name == Roles.SuperAdmin)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        var notifications = adminIds.Select(uid => new Notification
        {
            UserId = uid,
            NotificationType = NotificationType.Workflow_Change,
            TitleAr = title,
            MessageAr = message,
            RelatedEntityType = relatedType,
            RelatedEntityId = relatedId,
            IsRead = false,
            SentAt = DateTime.UtcNow
        }).ToList();

        if (notifications.Count > 0)
        {
            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();
        }
    }

    private static IndicatorEntryResponse MapToResponse(IndicatorEntry e) => new()
    {
        Id = e.Id,
        IndicatorId = e.IndicatorId,
        IndicatorNameAr = e.Indicator.NameAr,
        IndicatorCode = e.Indicator.Code,
        EntityId = e.EntityId,
        EntityNameAr = e.Entity.NameAr,
        ReportingPeriodId = e.ReportingPeriodId,
        PeriodDisplayNameAr = e.ReportingPeriod.DisplayNameAr,
        ValueNumeric = e.ValueNumeric,
        ValueText = e.ValueText,
        UnitSnapshot = e.UnitSnapshot,
        WorkflowState = e.WorkflowState,
        VersionNo = e.VersionNo,
        Notes = e.Notes,
        Source = e.Source,
        RejectionReason = e.RejectionReason,
        EnteredAt = e.EnteredAt,
        EnteredByName = e.EnteredByUser.FullNameAr,
        SubmittedAt = e.SubmittedAt,
        Dimensions = e.EntryDimensions.Select(d => new EntryDimensionResponse
        {
            Id = d.Id,
            DimensionId = d.DimensionId,
            DimensionNameAr = d.Dimension.DimensionNameAr,
            DimensionValueId = d.DimensionValueId,
            DimensionValueAr = d.DimensionValue?.ValueAr,
            ValueNumeric = d.ValueNumeric
        }).ToList(),
        Attachments = e.Attachments.Select(a => new AttachmentResponse
        {
            Id = a.Id,
            FileName = a.FileName,
            FileType = a.FileType,
            FileSize = a.FileSize,
            Description = a.Description,
            UploadedAt = a.UploadedAt
        }).ToList()
    };
}
