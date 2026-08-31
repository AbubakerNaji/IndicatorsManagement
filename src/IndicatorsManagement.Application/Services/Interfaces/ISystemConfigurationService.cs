using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface ISystemConfigurationService
{
    Task<ApiResponse<Dictionary<string, string>>> GetAllConfigAsync();
    Task<ApiResponse> UpdateConfigAsync(string key, string value, int userId);
    // Lookup a single config value; returns null when the key does not exist.
    Task<string?> GetConfigValueAsync(string key);
}
