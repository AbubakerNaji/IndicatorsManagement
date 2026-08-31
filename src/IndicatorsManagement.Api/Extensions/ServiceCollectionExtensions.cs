using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Infrastructure.Services;

namespace IndicatorsManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IIndicatorService, IndicatorService>();
        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IReportingPeriodService, ReportingPeriodService>();
        services.AddScoped<IIndicatorAssignmentService, IndicatorAssignmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IIndicatorEntryService, IndicatorEntryService>();
        services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();
        services.AddScoped<IValidationRuleService, ValidationRuleService>();
        services.AddScoped<IDraftRecoveryService, DraftRecoveryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IPublicationService, PublicationService>();
        services.AddSingleton<IEmailService, EmailService>();

        return services;
    }
}
