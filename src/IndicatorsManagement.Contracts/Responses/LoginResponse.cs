namespace IndicatorsManagement.Contracts.Responses;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;
}

public class UserInfo
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? EntityNameAr { get; set; }
}
