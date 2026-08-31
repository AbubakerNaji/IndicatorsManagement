using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Responses;

public class NotificationResponse
{
    public int Id { get; set; }
    public NotificationType NotificationType { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string MessageAr { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
