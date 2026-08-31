namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendToMultipleAsync(IEnumerable<string> recipients, string subject, string htmlBody);
}
