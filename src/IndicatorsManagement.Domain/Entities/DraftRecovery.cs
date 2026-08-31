namespace IndicatorsManagement.Domain.Entities;

public class DraftRecovery
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int IndicatorId { get; set; }
    public int EntityId { get; set; }
    public int ReportingPeriodId { get; set; }
    public string DraftDataJson { get; set; } = string.Empty;
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Indicator Indicator { get; set; } = null!;
    public virtual ReportingPeriod ReportingPeriod { get; set; } = null!;
}
