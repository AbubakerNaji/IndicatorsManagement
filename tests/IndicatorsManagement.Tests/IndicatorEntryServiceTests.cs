using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Property 6: Initial Workflow State (Req 4.1)
/// Property 9: Active Entry Uniqueness (Req 4.6)
/// Property 12: Modification State Restriction (Req 5.1)
/// Property 15-19: Workflow Transitions
/// Property 37: Draft-Only Soft Delete (Req 34.1)
/// </summary>
public class IndicatorEntryServiceTests
{
    private async Task<(IndicatorEntryService service, Infrastructure.Data.IndicatorsDbContext db)> SetupAsync(string testName)
    {
        var db = TestDbContextFactory.Create(testName);
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var notification = new NotificationService(db);
        return (new IndicatorEntryService(db, audit, notification), db);
    }

    [Fact]
    public async Task CreateEntry_ShouldInitializeToDraft()
    {
        // Property 6: new entries are Draft with version 1
        var (service, db) = await SetupAsync(nameof(CreateEntry_ShouldInitializeToDraft));
        var request = new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 };

        var result = await service.CreateEntryAsync(request, 1, 2);
        result.Success.Should().BeTrue();
        result.Data!.WorkflowState.Should().Be(WorkflowState.Draft);
        result.Data.VersionNo.Should().Be(1);
    }

    [Fact]
    public async Task CreateEntry_Duplicate_ShouldFail()
    {
        // Property 9: only one active entry per indicator/entity/period
        var (service, _) = await SetupAsync(nameof(CreateEntry_Duplicate_ShouldFail));
        var request = new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 };

        await service.CreateEntryAsync(request, 1, 2);
        var result = await service.CreateEntryAsync(request, 1, 2);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("فعّال");
    }

    [Fact]
    public async Task UpdateEntry_InUnderReview_ShouldFail()
    {
        // Property 12: entries can only be modified in Draft or Returned_For_Modification
        var (service, db) = await SetupAsync(nameof(UpdateEntry_InUnderReview_ShouldFail));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1, ValueNumeric = 100 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");

        var result = await service.UpdateEntryAsync(create.Data.Id, new UpdateIndicatorEntryRequest { ValueNumeric = 200 }, 1, 2, "Data_Entry_User");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitEntry_ShouldTransitionToUnderReview()
    {
        // Property 15: Draft → Under_Review on submit
        var (service, _) = await SetupAsync(nameof(SubmitEntry_ShouldTransitionToUnderReview));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);

        var result = await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        result.Success.Should().BeTrue();
        result.Data!.WorkflowState.Should().Be(WorkflowState.Under_Review);
    }

    [Fact]
    public async Task ApproveEntityLevel_ShouldTransitionToApprovedByEntity()
    {
        // Property 16: Under_Review → Approved_By_Entity
        var (service, _) = await SetupAsync(nameof(ApproveEntityLevel_ShouldTransitionToApprovedByEntity));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");

        var result = await service.ApproveEntityLevelAsync(create.Data.Id, 2, 2, "Entity_Admin", null);
        result.Success.Should().BeTrue();
        result.Data!.WorkflowState.Should().Be(WorkflowState.Approved_By_Entity);
    }

    [Fact]
    public async Task ApproveMinistryLevel_ShouldTransitionToFinalApproved()
    {
        // Property 17: Approved_By_Entity → Final_Approved
        var (service, _) = await SetupAsync(nameof(ApproveMinistryLevel_ShouldTransitionToFinalApproved));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        await service.ApproveEntityLevelAsync(create.Data.Id, 2, 2, "Entity_Admin", null);

        var result = await service.ApproveMinistryLevelAsync(create.Data.Id, 3, null);
        result.Success.Should().BeTrue();
        result.Data!.WorkflowState.Should().Be(WorkflowState.Final_Approved);
    }

    [Fact]
    public async Task RejectEntry_WithoutNotes_ShouldFail()
    {
        // Property 18: rejection requires notes
        var (service, _) = await SetupAsync(nameof(RejectEntry_WithoutNotes_ShouldFail));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");

        var result = await service.RejectEntryAsync(create.Data.Id, 2, 2, "Entity_Admin", "");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RejectEntry_ShouldTransitionToRejected()
    {
        var (service, _) = await SetupAsync(nameof(RejectEntry_ShouldTransitionToRejected));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");

        var result = await service.RejectEntryAsync(create.Data.Id, 2, 2, "Entity_Admin", "بيانات غير صحيحة");
        result.Success.Should().BeTrue();
        result.Data!.WorkflowState.Should().Be(WorkflowState.Rejected);
    }

    [Fact]
    public async Task FinalApproved_ShouldNotBeModifiable()
    {
        // Property 19: Final_Approved entries cannot be modified
        var (service, _) = await SetupAsync(nameof(FinalApproved_ShouldNotBeModifiable));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        await service.ApproveEntityLevelAsync(create.Data.Id, 2, 2, "Entity_Admin", null);
        await service.ApproveMinistryLevelAsync(create.Data.Id, 3, null);

        var result = await service.UpdateEntryAsync(create.Data.Id, new UpdateIndicatorEntryRequest { ValueNumeric = 999 }, 1, 2, "Data_Entry_User");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDelete_DraftEntry_ShouldSucceed()
    {
        // Property 37: only Draft entries can be soft deleted
        var (service, db) = await SetupAsync(nameof(SoftDelete_DraftEntry_ShouldSucceed));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);

        var result = await service.SoftDeleteEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        result.Success.Should().BeTrue();

        var entry = await db.IndicatorEntries.FirstAsync(e => e.Id == create.Data.Id);
        entry.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDelete_SubmittedEntry_ShouldFail()
    {
        // Property 37: submitted entries cannot be deleted
        var (service, _) = await SetupAsync(nameof(SoftDelete_SubmittedEntry_ShouldFail));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");

        var result = await service.SoftDeleteEntryAsync(create.Data.Id, 1, 2, "Data_Entry_User");
        result.Success.Should().BeFalse();
    }
}
