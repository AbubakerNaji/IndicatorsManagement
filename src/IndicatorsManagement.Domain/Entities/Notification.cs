using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public NotificationType NotificationType { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string MessageAr { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
