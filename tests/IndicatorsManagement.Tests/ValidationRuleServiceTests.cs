using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Property 23: Mandatory Validation Rules Enforcement (Req 8.7, 26.3, 26.4)
/// Property 34: Validation Rule Min/Max Enforcement (Req 26.2)
/// </summary>
public class ValidationRuleServiceTests
{
    [Fact]
    public async Task CreateValidationRule_ShouldPersist()
    {
        var db = TestDbContextFactory.Create(nameof(CreateValidationRule_ShouldPersist));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new ValidationRuleService(db, audit);

        var result = await service.CreateRuleAsync(new CreateValidationRuleRequest
        {
            IndicatorId = 1, RuleType = ValidationRuleType.Numeric, MinValue = 0, MaxValue = 100
        }, 1);

        result.Success.Should().BeTrue();
        result.Data!.MinValue.Should().Be(0);
        result.Data.MaxValue.Should().Be(100);
    }

    [Fact]
    public async Task CreateValidationRule_InvalidIndicator_ShouldFail()
    {
        var db = TestDbContextFactory.Create(nameof(CreateValidationRule_InvalidIndicator_ShouldFail));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new ValidationRuleService(db, audit);

        var result = await service.CreateRuleAsync(new CreateValidationRuleRequest
        {
            IndicatorId = 999, RuleType = ValidationRuleType.Numeric
        }, 1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitEntry_WithMandatoryAttachment_WithoutAttachment_ShouldFail()
    {
        // Property 23: mandatory attachments enforced on submit
        var db = TestDbContextFactory.Create(nameof(SubmitEntry_WithMandatoryAttachment_WithoutAttachment_ShouldFail));
        await TestDbContextFactory.SeedBasicData(db);

        // Set indicator to require attachment
        var indicator = await db.Indicators.FindAsync(1);
        indicator!.RequiresAttachment = true;
        await db.SaveChangesAsync();

        var audit = new AuditLogService(db);
        var notification = new NotificationService(db);
        var entryService = new IndicatorEntryService(db, audit, notification);

        var create = await entryService.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1, ValueNumeric = 50 }, 1, 2);

        var result = await entryService.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("مستند");
    }
}
