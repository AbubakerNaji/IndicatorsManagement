using Microsoft.AspNetCore.Identity;

namespace IndicatorsManagement.Domain.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public int? EntityId { get; set; }
    public string FullNameAr { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Entity? Entity { get; set; }
}
