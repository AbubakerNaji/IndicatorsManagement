using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class ReportingPeriodService : IReportingPeriodService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;

    public ReportingPeriodService(IndicatorsDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<List<ReportingPeriodResponse>>> GetPeriodsAsync(PeriodType? periodType = null, int? year = null)
    {
        var query = _db.ReportingPeriods.AsNoTracking().AsQueryable();

        if (periodType.HasValue)
            query = query.Where(p => p.PeriodType == periodType.Value);
        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        var periods = await query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.PeriodType)
            .ThenBy(p => p.StartDate)
            .Select(p => new ReportingPeriodResponse
            {
                Id = p.Id,
                PeriodType = p.PeriodType,
                Year = p.Year,
                Month = p.Month,
                Quarter = p.Quarter,
                HalfYear = p.HalfYear,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                DisplayNameAr = p.DisplayNameAr,
                IsOpen = p.IsOpen
            })
            .ToListAsync();

        return ApiResponse<List<ReportingPeriodResponse>>.Ok(periods);
    }

    public async Task<ApiResponse<int>> GeneratePeriodsAsync(GenerateReportingPeriodsRequest request, int creatorUserId)
    {
        var periods = new List<ReportingPeriod>();
        var monthNamesAr = new[] { "", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };

        for (int year = request.StartYear; year <= request.EndYear; year++)
        {
            foreach (var periodType in request.PeriodTypes)
            {
                switch (periodType)
                {
                    case PeriodType.Monthly:
                        for (int month = 1; month <= 12; month++)
                        {
                            if (await PeriodExists(periodType, year, month, null, null)) continue;
                            var daysInMonth = DateTime.DaysInMonth(year, month);
                            periods.Add(new ReportingPeriod
                            {
                                PeriodType = PeriodType.Monthly,
                                Year = year,
                                Month = month,
                                StartDate = new DateOnly(year, month, 1),
                                EndDate = new DateOnly(year, month, daysInMonth),
                                DisplayNameAr = $"{monthNamesAr[month]} {year}",
                                IsOpen = true
                            });
                        }
                        break;

                    case PeriodType.Quarterly:
                        for (int quarter = 1; quarter <= 4; quarter++)
                        {
                            if (await PeriodExists(periodType, year, null, quarter, null)) continue;
                            int startMonth = (quarter - 1) * 3 + 1;
                            int endMonth = startMonth + 2;
                            periods.Add(new ReportingPeriod
                            {
                                PeriodType = PeriodType.Quarterly,
                                Year = year,
                                Quarter = quarter,
                                StartDate = new DateOnly(year, startMonth, 1),
                                EndDate = new DateOnly(year, endMonth, DateTime.DaysInMonth(year, endMonth)),
                                DisplayNameAr = $"الربع {quarter} - {year}",
                                IsOpen = true
                            });
                        }
                        break;

                    case PeriodType.Semi_Annual:
                        for (int half = 1; half <= 2; half++)
                        {
                            if (await PeriodExists(periodType, year, null, null, half)) continue;
                            int startMonth = (half - 1) * 6 + 1;
                            int endMonth = startMonth + 5;
                            periods.Add(new ReportingPeriod
                            {
                                PeriodType = PeriodType.Semi_Annual,
                                Year = year,
                                HalfYear = half,
                                StartDate = new DateOnly(year, startMonth, 1),
                                EndDate = new DateOnly(year, endMonth, DateTime.DaysInMonth(year, endMonth)),
                                DisplayNameAr = $"النصف {(half == 1 ? "الأول" : "الثاني")} - {year}",
                                IsOpen = true
                            });
                        }
                        break;

                    case PeriodType.Annual:
                        if (await PeriodExists(periodType, year, null, null, null)) continue;
                        periods.Add(new ReportingPeriod
                        {
                            PeriodType = PeriodType.Annual,
                            Year = year,
                            StartDate = new DateOnly(year, 1, 1),
                            EndDate = new DateOnly(year, 12, 31),
                            DisplayNameAr = $"سنة {year}",
                            IsOpen = true
                        });
                        break;
                }
            }
        }

        if (periods.Count == 0)
            return ApiResponse<int>.Ok(0, "جميع الفترات موجودة مسبقاً");

        _db.ReportingPeriods.AddRange(periods);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(creatorUserId, "ReportingPeriod", null, "Generate_Periods",
            newValues: $"Generated {periods.Count} periods for {request.StartYear}-{request.EndYear}");

        return ApiResponse<int>.Ok(periods.Count, $"تم إنشاء {periods.Count} فترة إبلاغ بنجاح");
    }

    private async Task<bool> PeriodExists(PeriodType type, int year, int? month, int? quarter, int? halfYear)
    {
        return await _db.ReportingPeriods.AnyAsync(p =>
            p.PeriodType == type && p.Year == year &&
            p.Month == month && p.Quarter == quarter && p.HalfYear == halfYear);
    }
}
