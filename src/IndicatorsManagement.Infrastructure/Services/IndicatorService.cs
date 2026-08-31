using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class IndicatorService : IIndicatorService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;

    public IndicatorService(IndicatorsDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<PaginatedResponse<IndicatorResponse>>> GetIndicatorsAsync(bool? isActive = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Indicators.AsNoTracking()
            .Include(i => i.Dimensions).ThenInclude(d => d.Values)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(i => i.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var indicators = await query
            .OrderBy(i => i.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = indicators.Select(MapToResponse).ToList();

        return ApiResponse<PaginatedResponse<IndicatorResponse>>.Ok(new PaginatedResponse<IndicatorResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<IndicatorResponse>> GetIndicatorByIdAsync(int id)
    {
        var indicator = await _db.Indicators.AsNoTracking()
            .Include(i => i.Dimensions).ThenInclude(d => d.Values)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (indicator is null)
            return ApiResponse<IndicatorResponse>.Fail("المؤشر غير موجود");

        return ApiResponse<IndicatorResponse>.Ok(MapToResponse(indicator));
    }

    public async Task<ApiResponse<IndicatorResponse>> CreateIndicatorAsync(CreateIndicatorRequest request, int creatorUserId)
    {
        // Validate code uniqueness
        if (await _db.Indicators.AnyAsync(i => i.Code == request.Code))
            return ApiResponse<IndicatorResponse>.Fail("رمز المؤشر مستخدم مسبقاً");

        var indicator = new Indicator
        {
            Code = request.Code,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            DefinitionAr = request.DefinitionAr,
            CalculationMethodAr = request.CalculationMethodAr,
            UnitAr = request.UnitAr,
            DataSourceAr = request.DataSourceAr,
            ObjectiveAr = request.ObjectiveAr,
            PublicationFrequency = request.PublicationFrequency,
            RequiresAttachment = request.RequiresAttachment,
            RequiresReview = request.RequiresReview,
            CreatedBy = creatorUserId,
            IsActive = true
        };

        // Add dimensions
        if (request.Dimensions is { Count: > 0 })
        {
            foreach (var dimReq in request.Dimensions)
            {
                var dimension = new Dimension
                {
                    DimensionNameAr = dimReq.DimensionNameAr,
                    DimensionType = dimReq.DimensionType,
                    IsMandatory = dimReq.IsMandatory,
                    DisplayOrder = dimReq.DisplayOrder
                };

                if (dimReq.Values is { Count: > 0 })
                {
                    foreach (var valReq in dimReq.Values)
                    {
                        dimension.Values.Add(new DimensionValue
                        {
                            ValueAr = valReq.ValueAr,
                            ValueEn = valReq.ValueEn,
                            DisplayOrder = valReq.DisplayOrder,
                            IsActive = true
                        });
                    }
                }

                indicator.Dimensions.Add(dimension);
            }
        }

        _db.Indicators.Add(indicator);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(creatorUserId, "Indicator", indicator.Id, "Create_Indicator",
            newValues: JsonSerializer.Serialize(new { indicator.Code, indicator.NameAr }));

        // Reload with dimensions
        var created = await _db.Indicators.AsNoTracking()
            .Include(i => i.Dimensions).ThenInclude(d => d.Values)
            .FirstAsync(i => i.Id == indicator.Id);

        return ApiResponse<IndicatorResponse>.Ok(MapToResponse(created), "تم إنشاء المؤشر بنجاح");
    }

    public async Task<ApiResponse<IndicatorResponse>> UpdateIndicatorAsync(int id, UpdateIndicatorRequest request, int updaterUserId)
    {
        var indicator = await _db.Indicators
            .Include(i => i.Dimensions).ThenInclude(d => d.Values)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (indicator is null)
            return ApiResponse<IndicatorResponse>.Fail("المؤشر غير موجود");

        var oldValues = JsonSerializer.Serialize(new { indicator.NameAr, indicator.IsActive });

        indicator.NameAr = request.NameAr;
        indicator.NameEn = request.NameEn;
        indicator.DefinitionAr = request.DefinitionAr;
        indicator.CalculationMethodAr = request.CalculationMethodAr;
        indicator.UnitAr = request.UnitAr;
        indicator.DataSourceAr = request.DataSourceAr;
        indicator.ObjectiveAr = request.ObjectiveAr;
        indicator.PublicationFrequency = request.PublicationFrequency;
        indicator.RequiresAttachment = request.RequiresAttachment;
        indicator.RequiresReview = request.RequiresReview;
        indicator.IsActive = request.IsActive;
        indicator.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(updaterUserId, "Indicator", indicator.Id, "Update_Indicator",
            oldValues: oldValues,
            newValues: JsonSerializer.Serialize(new { request.NameAr, request.IsActive }));

        return ApiResponse<IndicatorResponse>.Ok(MapToResponse(indicator), "تم تحديث المؤشر بنجاح");
    }

    public async Task<ApiResponse> DeleteIndicatorAsync(int id, int deleterUserId)
    {
        var indicator = await _db.Indicators.FindAsync(id);
        if (indicator is null)
            return ApiResponse.Fail("المؤشر غير موجود");

        // Prevent deletion if assignments exist
        var hasAssignments = await _db.IndicatorAssignments.AnyAsync(a => a.IndicatorId == id);
        if (hasAssignments)
            return ApiResponse.Fail("لا يمكن حذف المؤشر لوجود تكليفات مرتبطة به");

        _db.Indicators.Remove(indicator);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(deleterUserId, "Indicator", id, "Delete_Indicator");

        return ApiResponse.Ok("تم حذف المؤشر بنجاح");
    }

    public async Task<ApiResponse<DimensionResponse>> AddDimensionAsync(int indicatorId, CreateDimensionRequest request, int userId)
    {
        var indicator = await _db.Indicators.FindAsync(indicatorId);
        if (indicator is null)
            return ApiResponse<DimensionResponse>.Fail("المؤشر غير موجود");

        var dimension = new Dimension
        {
            IndicatorId = indicatorId,
            DimensionNameAr = request.DimensionNameAr,
            DimensionType = request.DimensionType,
            IsMandatory = request.IsMandatory,
            DisplayOrder = request.DisplayOrder
        };

        if (request.Values is { Count: > 0 })
        {
            foreach (var v in request.Values)
                dimension.Values.Add(new DimensionValue { ValueAr = v.ValueAr, ValueEn = v.ValueEn, DisplayOrder = v.DisplayOrder, IsActive = true });
        }

        _db.Dimensions.Add(dimension);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Dimension", dimension.Id, "Add_Dimension");

        var created = await _db.Dimensions.AsNoTracking().Include(d => d.Values).FirstAsync(d => d.Id == dimension.Id);
        return ApiResponse<DimensionResponse>.Ok(MapDimensionResponse(created), "تم إضافة البُعد بنجاح");
    }

    public async Task<ApiResponse<DimensionResponse>> UpdateDimensionAsync(int dimensionId, CreateDimensionRequest request, int userId)
    {
        var dimension = await _db.Dimensions.Include(d => d.Values).FirstOrDefaultAsync(d => d.Id == dimensionId);
        if (dimension is null)
            return ApiResponse<DimensionResponse>.Fail("البُعد غير موجود");

        dimension.DimensionNameAr = request.DimensionNameAr;
        dimension.DimensionType = request.DimensionType;
        dimension.IsMandatory = request.IsMandatory;
        dimension.DisplayOrder = request.DisplayOrder;
        dimension.UpdatedAt = DateTime.UtcNow;

        // Replace values
        if (request.Values is not null)
        {
            _db.DimensionValues.RemoveRange(dimension.Values);
            foreach (var v in request.Values)
                dimension.Values.Add(new DimensionValue { ValueAr = v.ValueAr, ValueEn = v.ValueEn, DisplayOrder = v.DisplayOrder, IsActive = true });
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Dimension", dimensionId, "Update_Dimension");

        return ApiResponse<DimensionResponse>.Ok(MapDimensionResponse(dimension), "تم تحديث البُعد بنجاح");
    }

    public async Task<ApiResponse> DeleteDimensionAsync(int dimensionId, int userId)
    {
        var dimension = await _db.Dimensions.FindAsync(dimensionId);
        if (dimension is null)
            return ApiResponse.Fail("البُعد غير موجود");

        _db.Dimensions.Remove(dimension); // Cascade deletes DimensionValues
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Dimension", dimensionId, "Delete_Dimension");

        return ApiResponse.Ok("تم حذف البُعد بنجاح");
    }

    private static DimensionResponse MapDimensionResponse(Dimension d) => new()
    {
        Id = d.Id,
        DimensionNameAr = d.DimensionNameAr,
        DimensionType = d.DimensionType,
        IsMandatory = d.IsMandatory,
        DisplayOrder = d.DisplayOrder,
        Values = d.Values.OrderBy(v => v.DisplayOrder).Select(v => new DimensionValueResponse
        {
            Id = v.Id, ValueAr = v.ValueAr, ValueEn = v.ValueEn, DisplayOrder = v.DisplayOrder, IsActive = v.IsActive
        }).ToList()
    };

    private static IndicatorResponse MapToResponse(Indicator i) => new()
    {
        Id = i.Id,
        Code = i.Code,
        NameAr = i.NameAr,
        NameEn = i.NameEn,
        DefinitionAr = i.DefinitionAr,
        CalculationMethodAr = i.CalculationMethodAr,
        UnitAr = i.UnitAr,
        DataSourceAr = i.DataSourceAr,
        ObjectiveAr = i.ObjectiveAr,
        PublicationFrequency = i.PublicationFrequency,
        IsActive = i.IsActive,
        RequiresAttachment = i.RequiresAttachment,
        RequiresReview = i.RequiresReview,
        Dimensions = i.Dimensions.OrderBy(d => d.DisplayOrder).Select(d => new DimensionResponse
        {
            Id = d.Id,
            DimensionNameAr = d.DimensionNameAr,
            DimensionType = d.DimensionType,
            IsMandatory = d.IsMandatory,
            DisplayOrder = d.DisplayOrder,
            Values = d.Values.OrderBy(v => v.DisplayOrder).Select(v => new DimensionValueResponse
            {
                Id = v.Id,
                ValueAr = v.ValueAr,
                ValueEn = v.ValueEn,
                DisplayOrder = v.DisplayOrder,
                IsActive = v.IsActive
            }).ToList()
        }).ToList()
    };
}
