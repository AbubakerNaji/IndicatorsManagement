using IndicatorsManagement.Domain.Common;

namespace IndicatorsManagement.Domain.Entities;

public class ReopenRequest : BaseEntity
{
    public int IndicatorEntryId { get; set; }
    public int RequestedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public virtual IndicatorEntry IndicatorEntry { get; set; } = null!;
    public virtual ApplicationUser RequestedByUser { get; set; } = null!;
    public virtual ApplicationUser? ReviewedByUser { get; set; }
}
