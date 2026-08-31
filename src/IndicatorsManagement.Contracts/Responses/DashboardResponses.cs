using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Responses;

public class MinistryDashboardResponse
{
    public int TotalIndicators { get; set; }
    public int TotalEntities { get; set; }
    public int TotalSubmissions { get; set; }
    public int OverdueCount { get; set; }
    public List<EntitySubmissionRate> SubmissionsByEntity { get; set; } = [];
    public Dictionary<string, int> WorkflowDistribution { get; set; } = [];
}

public class EntityDashboardResponse
{
    public int EntityId { get; set; }
    public string EntityNameAr { get; set; } = string.Empty;
    public int AssignedIndicators { get; set; }
    public int SubmittedCount { get; set; }
    public int PendingCount { get; set; }
    public int OverdueCount { get; set; }
    public int ApprovedCount { get; set; }
}

public class EntitySubmissionRate
{
    public int EntityId { get; set; }
    public string EntityNameAr { get; set; } = string.Empty;
    public int TotalObligations { get; set; }
    public int SubmittedCount { get; set; }
    public decimal SubmissionRate { get; set; }
}

public class TaskItemResponse
{
    public int ObligationId { get; set; }
    public int IndicatorId { get; set; }
    public string IndicatorNameAr { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public int ReportingPeriodId { get; set; }
    public string PeriodDisplayNameAr { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public ObligationStatus Status { get; set; }
    public int? EntryId { get; set; }
    public WorkflowState? EntryWorkflowState { get; set; }
    public string Priority { get; set; } = string.Empty; // overdue, due_this_week, due_this_month, completed
}
