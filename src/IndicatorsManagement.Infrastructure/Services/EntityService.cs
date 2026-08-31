using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class EntityService : IEntityService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;

    public EntityService(IndicatorsDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<List<EntityResponse>>> GetEntitiesAsync()
    {
        var entities = await _db.Entities.AsNoTracking()
            .Include(e => e.ChildEntities)
            .Where(e => e.ParentEntityId == null)
            .OrderBy(e => e.NameAr)
            .ToListAsync();

        var result = entities.Select(MapToResponse).ToList();
        return ApiResponse<List<EntityResponse>>.Ok(result);
    }

    public async Task<ApiResponse<EntityResponse>> GetEntityByIdAsync(int id)
    {
        var entity = await _db.Entities.AsNoTracking()
            .Include(e => e.ChildEntities)
            .Include(e => e.ParentEntity)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
            return ApiResponse<EntityResponse>.Fail("الجهة غير موجودة");

        return ApiResponse<EntityResponse>.Ok(MapToResponse(entity));
    }

    public async Task<ApiResponse<EntityResponse>> CreateEntityAsync(CreateEntityRequest request, int creatorUserId)
    {
        // Validate name uniqueness
        if (await _db.Entities.AnyAsync(e => e.NameAr == request.NameAr))
            return ApiResponse<EntityResponse>.Fail("اسم الجهة مستخدم مسبقاً");

        if (request.ParentEntityId.HasValue)
        {
            var parentExists = await _db.Entities.AnyAsync(e => e.Id == request.ParentEntityId.Value);
            if (!parentExists)
                return ApiResponse<EntityResponse>.Fail("الجهة الأم غير موجودة");
        }

        var entity = new Domain.Entities.Entity
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Type = request.Type,
            ParentEntityId = request.ParentEntityId,
            Status = "active"
        };

        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(creatorUserId, "Entity", entity.Id, "Create_Entity",
            newValues: JsonSerializer.Serialize(new { entity.NameAr, entity.Type }));

        return ApiResponse<EntityResponse>.Ok(MapToResponse(entity), "تم إنشاء الجهة بنجاح");
    }

    public async Task<ApiResponse<EntityResponse>> UpdateEntityAsync(int id, UpdateEntityRequest request, int updaterUserId)
    {
        var entity = await _db.Entities.FindAsync(id);
        if (entity is null)
            return ApiResponse<EntityResponse>.Fail("الجهة غير موجودة");

        // Check name uniqueness (excluding self)
        if (await _db.Entities.AnyAsync(e => e.NameAr == request.NameAr && e.Id != id))
            return ApiResponse<EntityResponse>.Fail("اسم الجهة مستخدم مسبقاً");

        var oldValues = JsonSerializer.Serialize(new { entity.NameAr, entity.Status });

        entity.NameAr = request.NameAr;
        entity.NameEn = request.NameEn;
        entity.Type = request.Type;
        entity.ParentEntityId = request.ParentEntityId;
        entity.Status = request.Status;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(updaterUserId, "Entity", entity.Id, "Update_Entity",
            oldValues: oldValues,
            newValues: JsonSerializer.Serialize(new { request.NameAr, request.Status }));

        return ApiResponse<EntityResponse>.Ok(MapToResponse(entity), "تم تحديث الجهة بنجاح");
    }

    public async Task<ApiResponse> DeactivateEntityAsync(int id, int deactivatorUserId)
    {
        var entity = await _db.Entities.FindAsync(id);
        if (entity is null)
            return ApiResponse.Fail("الجهة غير موجودة");

        entity.Status = "inactive";
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(deactivatorUserId, "Entity", id, "Deactivate_Entity");

        return ApiResponse.Ok("تم تعطيل الجهة بنجاح");
    }

    private static EntityResponse MapToResponse(Domain.Entities.Entity e) => new()
    {
        Id = e.Id,
        NameAr = e.NameAr,
        NameEn = e.NameEn,
        Type = e.Type,
        ParentEntityId = e.ParentEntityId,
        ParentEntityNameAr = e.ParentEntity?.NameAr,
        Status = e.Status,
        ChildEntities = e.ChildEntities.Select(MapToResponse).ToList()
    };
}
