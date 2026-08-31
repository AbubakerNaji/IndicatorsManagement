using IndicatorsManagement.Domain.Common;

namespace IndicatorsManagement.Domain.Entities;

public class PublicationHistory : BaseEntity
{
    public int IndicatorEntryId { get; set; }
    public string Action { get; set; } = string.Empty; // "Published" or "Unpublished"
    public int PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    public virtual IndicatorEntry IndicatorEntry { get; set; } = null!;
    public virtual ApplicationUser PerformedByUser { get; set; } = null!;
}
