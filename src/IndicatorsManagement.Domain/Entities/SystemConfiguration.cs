namespace IndicatorsManagement.Domain.Entities;

public class SystemConfiguration
{
    public int Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ApplicationUser? UpdatedByUser { get; set; }
}
