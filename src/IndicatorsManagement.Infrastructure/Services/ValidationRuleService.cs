using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class ValidationRuleService : IValidationRuleService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;

    public ValidationRuleService(IndicatorsDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<List<ValidationRuleResponse>>> GetRulesByIndicatorAsync(int indicatorId)
    {
        var rules = await _db.ValidationRules.AsNoTracking()
            .Where(r => r.IndicatorId == indicatorId)
            .Select(r => new ValidationRuleResponse
            {
                Id = r.Id,
                IndicatorId = r.IndicatorId,
                RuleType = r.RuleType,
                MinValue = r.MinValue,
                MaxValue = r.MaxValue,
                IsMandatoryNotes = r.IsMandatoryNotes,
                IsMandatoryAttachment = r.IsMandatoryAttachment
            }).ToListAsync();

        return ApiResponse<List<ValidationRuleResponse>>.Ok(rules);
    }

    public async Task<ApiResponse<ValidationRuleResponse>> CreateRuleAsync(CreateValidationRuleRequest request, int userId)
    {
        if (!await _db.Indicators.AnyAsync(i => i.Id == request.IndicatorId))
            return ApiResponse<ValidationRuleResponse>.Fail("المؤشر غير موجود");

        var rule = new ValidationRule
        {
            IndicatorId = request.IndicatorId,
            RuleType = request.RuleType,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            IsMandatoryNotes = request.IsMandatoryNotes,
            IsMandatoryAttachment = request.IsMandatoryAttachment
        };

        _db.ValidationRules.Add(rule);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "ValidationRule", rule.Id, "Create_ValidationRule",
            newValues: JsonSerializer.Serialize(new { request.IndicatorId, request.RuleType, request.MinValue, request.MaxValue }));

        return ApiResponse<ValidationRuleResponse>.Ok(new ValidationRuleResponse
        {
            Id = rule.Id,
            IndicatorId = rule.IndicatorId,
            RuleType = rule.RuleType,
            MinValue = rule.MinValue,
            MaxValue = rule.MaxValue,
            IsMandatoryNotes = rule.IsMandatoryNotes,
            IsMandatoryAttachment = rule.IsMandatoryAttachment
        }, "تم إنشاء قاعدة التحقق بنجاح");
    }

    public async Task<ApiResponse<ValidationRuleResponse>> UpdateRuleAsync(int id, CreateValidationRuleRequest request, int userId)
    {
        var rule = await _db.ValidationRules.FindAsync(id);
        if (rule is null)
            return ApiResponse<ValidationRuleResponse>.Fail("قاعدة التحقق غير موجودة");

        var oldValues = JsonSerializer.Serialize(new { rule.RuleType, rule.MinValue, rule.MaxValue });

        rule.RuleType = request.RuleType;
        rule.MinValue = request.MinValue;
        rule.MaxValue = request.MaxValue;
        rule.IsMandatoryNotes = request.IsMandatoryNotes;
        rule.IsMandatoryAttachment = request.IsMandatoryAttachment;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "ValidationRule", id, "Update_ValidationRule",
            oldValues: oldValues,
            newValues: JsonSerializer.Serialize(new { request.RuleType, request.MinValue, request.MaxValue }));

        return ApiResponse<ValidationRuleResponse>.Ok(new ValidationRuleResponse
        {
            Id = rule.Id,
            IndicatorId = rule.IndicatorId,
            RuleType = rule.RuleType,
            MinValue = rule.MinValue,
            MaxValue = rule.MaxValue,
            IsMandatoryNotes = rule.IsMandatoryNotes,
            IsMandatoryAttachment = rule.IsMandatoryAttachment
        }, "تم تحديث قاعدة التحقق بنجاح");
    }

    public async Task<ApiResponse> DeleteRuleAsync(int id, int userId)
    {
        var rule = await _db.ValidationRules.FindAsync(id);
        if (rule is null)
            return ApiResponse.Fail("قاعدة التحقق غير موجودة");

        _db.ValidationRules.Remove(rule);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "ValidationRule", id, "Delete_ValidationRule");
        return ApiResponse.Ok("تم حذف قاعدة التحقق بنجاح");
    }
}
