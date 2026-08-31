using IndicatorsManagement.Domain.Common;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class ReportingPeriod : BaseEntity
{
    public PeriodType PeriodType { get; set; }
    public int Year { get; set; }
    public int? Month { get; set; }
    public int? Quarter { get; set; }
    public int? HalfYear { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string DisplayNameAr { get; set; } = string.Empty;
    public bool IsOpen { get; set; } = true;
}
