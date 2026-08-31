using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;

    public SystemConfigurationService(IndicatorsDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<Dictionary<string, string>>> GetAllConfigAsync()
    {
        var configs = await _db.SystemConfigurations.AsNoTracking()
            .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue);

        return ApiResponse<Dictionary<string, string>>.Ok(configs);
    }

    public async Task<ApiResponse> UpdateConfigAsync(string key, string value, int userId)
    {
        var config = await _db.SystemConfigurations.FirstOrDefaultAsync(c => c.ConfigKey == key);
        if (config is null)
            return ApiResponse.Fail("مفتاح الإعداد غير موجود");

        var oldValue = config.ConfigValue;
        config.ConfigValue = value;
        config.UpdatedBy = userId;
        config.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "SystemConfiguration", null, "Update_Config",
            oldValues: JsonSerializer.Serialize(new { Key = key, Value = oldValue }),
            newValues: JsonSerializer.Serialize(new { Key = key, Value = value }));

        return ApiResponse.Ok("تم تحديث الإعداد بنجاح");
    }

    public async Task<string?> GetConfigValueAsync(string key)
    {
        return await _db.SystemConfigurations.AsNoTracking()
            .Where(c => c.ConfigKey == key)
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync();
    }
}
