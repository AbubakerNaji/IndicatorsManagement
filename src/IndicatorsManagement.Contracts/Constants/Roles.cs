namespace IndicatorsManagement.Contracts.Constants;

public static class Roles
{
    public const string SuperAdmin = "Super_Admin";
    public const string MinistryAdmin = "Ministry_Admin";
    public const string EntityAdmin = "Entity_Admin";
    public const string DataEntryUser = "Data_Entry_User";
    public const string Reviewer = "Reviewer";
    public const string Auditor = "Auditor";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [SuperAdmin, MinistryAdmin, EntityAdmin, DataEntryUser, Reviewer, Auditor, Viewer];
    public static readonly string[] AdminRoles = [SuperAdmin, MinistryAdmin, EntityAdmin];
    public static readonly string[] ApprovalRoles = [SuperAdmin, MinistryAdmin, Reviewer, EntityAdmin];
    public static readonly string[] PublishRoles = [SuperAdmin, MinistryAdmin];
}
