namespace IndicatorsManagement.Domain.Entities;

public class Attachment
{
    public int Id { get; set; }
    public int IndicatorEntryId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public string? Description { get; set; }

    public virtual IndicatorEntry IndicatorEntry { get; set; } = null!;
    public virtual ApplicationUser UploadedByUser { get; set; } = null!;
}
