using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Requests;

public class GenerateReportingPeriodsRequest
{
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public List<PeriodType> PeriodTypes { get; set; } = [];
}
