namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(int? userId, string entityType, int? entityId, string actionType,
        string? oldValues = null, string? newValues = null, string? ipAddress = null,
        string resultStatus = "Success", string? errorCode = null, string? errorMessage = null);

    // S10 — walks the audit chain and returns a verification report.
    Task<AuditChainVerification> VerifyChainAsync();
}

public class AuditChainVerification
{
    public long TotalRows { get; set; }
    public bool IsValid { get; set; }
    public long? FirstBrokenRowId { get; set; }
    public string? BreakReason { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
