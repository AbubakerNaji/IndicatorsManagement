namespace IndicatorsManagement.Domain.Entities;

public class VersionHistory
{
    public int Id { get; set; }
    public int IndicatorEntryId { get; set; }
    public int VersionNo { get; set; }
    public decimal? ValueNumeric { get; set; }
    public string? ValueText { get; set; }
    public string WorkflowState { get; set; } = string.Empty;
    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? ChangeReason { get; set; }
    public string? SnapshotJson { get; set; }

    public virtual IndicatorEntry IndicatorEntry { get; set; } = null!;
    public virtual ApplicationUser ChangedByUser { get; set; } = null!;
}
