namespace IndicatorsManagement.Contracts.Requests;

public class UpdateUserRequest
{
    public string FullNameAr { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? EntityId { get; set; }
}
