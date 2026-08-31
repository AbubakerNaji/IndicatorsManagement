namespace IndicatorsManagement.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ResultStatus { get; set; } = "Success";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // S10 — tamper-evident hash chain. Each row's RowHash is SHA-256 of
    // (PreviousHash || canonical fields). Mutating any historical row breaks the chain.
    public string PreviousHash { get; set; } = string.Empty;
    public string RowHash { get; set; } = string.Empty;

    public virtual ApplicationUser? User { get; set; }
}
