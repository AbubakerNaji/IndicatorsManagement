using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Responses;

public class IndicatorEntryResponse
{
    public int Id { get; set; }
    public int IndicatorId { get; set; }
    public string IndicatorNameAr { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string EntityNameAr { get; set; } = string.Empty;
    public int ReportingPeriodId { get; set; }
    public string PeriodDisplayNameAr { get; set; } = string.Empty;
    public decimal? ValueNumeric { get; set; }
    public string? ValueText { get; set; }
    public string? UnitSnapshot { get; set; }
    public WorkflowState WorkflowState { get; set; }
    public int VersionNo { get; set; }
    public string? Notes { get; set; }
    public string? Source { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime EnteredAt { get; set; }
    public string EnteredByName { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public List<EntryDimensionResponse> Dimensions { get; set; } = [];
    public List<AttachmentResponse> Attachments { get; set; } = [];
}

public class EntryDimensionResponse
{
    public int Id { get; set; }
    public int DimensionId { get; set; }
    public string DimensionNameAr { get; set; } = string.Empty;
    public int? DimensionValueId { get; set; }
    public string? DimensionValueAr { get; set; }
    public decimal? ValueNumeric { get; set; }
}

public class AttachmentResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
}
