using IndicatorsManagement.Domain.Common;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class IndicatorEntry : BaseEntity
{
    public int IndicatorId { get; set; }
    public int EntityId { get; set; }
    public int ReportingPeriodId { get; set; }
    public decimal? ValueNumeric { get; set; }
    public string? ValueText { get; set; }
    public string? UnitSnapshot { get; set; }
    public WorkflowState WorkflowState { get; set; } = WorkflowState.Draft;
    public PublicationStatus PublicationStatus { get; set; } = PublicationStatus.Unpublished;
    public int VersionNo { get; set; } = 1;
    public bool IsDeleted { get; set; }
    public string? Notes { get; set; }
    public string? Source { get; set; }
    public string? RejectionReason { get; set; }

    public int EnteredBy { get; set; }
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? EntityApprovedBy { get; set; }
    public DateTime? EntityApprovedAt { get; set; }
    public int? MinistryApprovedBy { get; set; }
    public DateTime? MinistryApprovedAt { get; set; }

    public virtual Indicator Indicator { get; set; } = null!;
    public virtual Entity Entity { get; set; } = null!;
    public virtual ReportingPeriod ReportingPeriod { get; set; } = null!;
    public virtual ApplicationUser EnteredByUser { get; set; } = null!;
    public virtual ApplicationUser? ReviewedByUser { get; set; }
    public virtual ApplicationUser? EntityApprovedByUser { get; set; }
    public virtual ApplicationUser? MinistryApprovedByUser { get; set; }
    public virtual ICollection<IndicatorEntryDimension> EntryDimensions { get; set; } = [];
    public virtual ICollection<Attachment> Attachments { get; set; } = [];
}
