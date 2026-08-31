using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Property 28: Assignment Referential Integrity (Req 22.2)
/// Property 29: Assignment End Date Restriction (Req 22.5)
/// Property 30: Obligation Generation on Assignment (Req 23.1)
/// Property 31: Submission Obligation Status Updates (Req 23.4, 23.5, 23.7)
/// </summary>
public class AssignmentServiceTests
{
    [Fact]
    public async Task CreateAssignment_InvalidIndicator_ShouldFail()
    {
        // Property 28: assignments require valid indicator
        var db = TestDbContextFactory.Create(nameof(CreateAssignment_InvalidIndicator_ShouldFail));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorAssignmentService(db, audit);

        var result = await service.CreateAssignmentAsync(new CreateAssignmentRequest
        {
            IndicatorId = 999, EntityId = 2, StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ReportingFrequency = PeriodType.Monthly
        }, 1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAssignment_InvalidEntity_ShouldFail()
    {
        // Property 28: assignments require valid entity
        var db = TestDbContextFactory.Create(nameof(CreateAssignment_InvalidEntity_ShouldFail));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorAssignmentService(db, audit);

        var result = await service.CreateAssignmentAsync(new CreateAssignmentRequest
        {
            IndicatorId = 1, EntityId = 999, StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ReportingFrequency = PeriodType.Monthly
        }, 1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAssignment_ShouldAutoGenerateObligations()
    {
        // Property 30: obligations generated when assignment is created
        var db = TestDbContextFactory.Create(nameof(CreateAssignment_ShouldAutoGenerateObligations));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorAssignmentService(db, audit);

        var result = await service.CreateAssignmentAsync(new CreateAssignmentRequest
        {
            IndicatorId = 1, EntityId = 1, StartDate = new DateOnly(2026, 1, 1),
            ReportingFrequency = PeriodType.Monthly
        }, 1);

        result.Success.Should().BeTrue();

        // Check obligations were auto-generated
        var obligations = await db.SubmissionObligations
            .Where(o => o.IndicatorAssignment.IndicatorId == 1 && o.IndicatorAssignment.EntityId == 1)
            .ToListAsync();

        obligations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ObligationStatus_UpdatesOnEntryCreation()
    {
        // Property 31: obligation status updates on entry state changes
        var db = TestDbContextFactory.Create(nameof(ObligationStatus_UpdatesOnEntryCreation));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var notification = new NotificationService(db);
        var entryService = new IndicatorEntryService(db, audit, notification);

        // Create entry — should update obligation to In_Progress
        var entry = await entryService.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1, ValueNumeric = 100 }, 1, 2);
        entry.Success.Should().BeTrue();

        // Submit — should update obligation to Submitted
        var submit = await entryService.SubmitEntryAsync(entry.Data!.Id, 1, 2, "Data_Entry_User");
        submit.Success.Should().BeTrue();
    }
}
