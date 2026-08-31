namespace IndicatorsManagement.Api.Authorization;

public static class PolicyNames
{
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string MinistryLevel = "MinistryLevel";
    public const string EntityLevel = "EntityLevel";
    public const string DataEntry = "DataEntry";
    public const string ReviewerAccess = "ReviewerAccess";
    public const string AuditorAccess = "AuditorAccess";
    public const string EntityScoped = "EntityScoped";
    public const string ViewerAccess = "ViewerAccess";
    public const string PublishAccess = "PublishAccess";
}
