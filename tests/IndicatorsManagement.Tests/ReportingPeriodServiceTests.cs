using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Property 3: Reporting Period Uniqueness (Req 3.2)
/// </summary>
public class ReportingPeriodServiceTests
{
    [Fact]
    public async Task GeneratePeriods_DuplicatesSkipped()
    {
        var db = TestDbContextFactory.Create(nameof(GeneratePeriods_DuplicatesSkipped));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new ReportingPeriodService(db, audit);

        // First generation
        var result1 = await service.GeneratePeriodsAsync(new GenerateReportingPeriodsRequest
        {
            StartYear = 2027, EndYear = 2027, PeriodTypes = [PeriodType.Annual]
        }, 1);
        result1.Success.Should().BeTrue();
        result1.Data.Should().Be(1);

        // Second generation — duplicate should be skipped
        var result2 = await service.GeneratePeriodsAsync(new GenerateReportingPeriodsRequest
        {
            StartYear = 2027, EndYear = 2027, PeriodTypes = [PeriodType.Annual]
        }, 1);
        result2.Data.Should().Be(0);
    }
}
