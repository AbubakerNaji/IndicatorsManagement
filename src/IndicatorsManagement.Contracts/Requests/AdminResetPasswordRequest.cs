namespace IndicatorsManagement.Contracts.Requests;

public class AdminResetPasswordRequest
{
    public int UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}
