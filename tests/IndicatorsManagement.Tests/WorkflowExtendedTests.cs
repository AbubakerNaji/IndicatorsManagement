using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Extended workflow and entry tests:
/// Property 7: User Entity Authorization (Req 4.3)
/// Property 10: Dimension Values Required (Req 4.7)
/// Property 11: Initial Version Number (Req 4.8)
/// Property 13: Attachment State Restriction (Req 5.4)
/// Property 21: File Size Validation (Req 8.2)
/// Property 22: Cascade Soft Delete for Attachments (Req 8.5)
/// Property 38: Soft Deleted Entry Exclusion (Req 34.4)
/// </summary>
public class WorkflowExtendedTests
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
    public async Task CreateEntry_WrongEntity_ShouldFail()
    {
        // Property 7: users can only create entries for their entity
        var (service, _) = await SetupAsync(nameof(CreateEntry_WrongEntity_ShouldFail));

        // User belongs to entity 2, trying to create for entity 1 (no assignment for entity 1 from user perspective)
        var result = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 1); // userEntityId=1 has no assignment

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateEntry_NoAssignment_ShouldFail()
    {
        // Property 8: entries require active assignment
        var (service, _) = await SetupAsync(nameof(CreateEntry_NoAssignment_ShouldFail));

        var result = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 99); // entity 99 has no assignment

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateEntry_VersionIsOne()
    {
        // Property 11: new entries have version_no = 1
        var (service, _) = await SetupAsync(nameof(CreateEntry_VersionIsOne));

        var result = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);

        result.Success.Should().BeTrue();
        result.Data!.VersionNo.Should().Be(1);
    }

    [Fact]
    public async Task ReturnEntry_ShouldRequireNotes()
    {
        // Property 18: return requires notes
        var (service, _) = await SetupAsync(nameof(ReturnEntry_ShouldRequireNotes));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");

        var result = await service.ReturnEntryAsync(create.Data.Id, 2, 2, "Entity_Admin", "");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ReturnEntry_ShouldTransitionToReturnedForModification()
    {
        var (service, _) = await SetupAsync(nameof(ReturnEntry_ShouldTransitionToReturnedForModification));
        var create = await service.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");

        var result = await service.ReturnEntryAsync(create.Data.Id, 2, 2, "Entity_Admin", "يحتاج تعديل");
        result.Success.Should().BeTrue();
        result.Data!.WorkflowState.Should().Be(WorkflowState.Returned_For_Modification);
    }

    [Fact]
    public async Task ReturnedEntry_CanBeModified()
    {
        // Property 12 extended: Returned_For_Modification entries can be modified
        var (service, _) = await SetupAsync(nameof(ReturnedEntry_CanBeModified));
        var create = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1, ValueNumeric = 50 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        await service.ReturnEntryAsync(create.Data.Id, 2, 2, "Entity_Admin", "يحتاج تعديل");

        var result = await service.UpdateEntryAsync(create.Data.Id, new UpdateIndicatorEntryRequest { ValueNumeric = 75 }, 1, 2, "Data_Entry_User");
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ReturnedEntry_CanBeResubmitted()
    {
        // Returned → Under_Review (resubmit)
        var (service, _) = await SetupAsync(nameof(ReturnedEntry_CanBeResubmitted));
        var create = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1, ValueNumeric = 50 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        await service.ReturnEntryAsync(create.Data.Id, 2, 2, "Entity_Admin", "يحتاج تعديل");

        var result = await service.SubmitEntryAsync(create.Data.Id, 1, 2, "Data_Entry_User");
        result.Success.Should().BeTrue();
        result.Data!.WorkflowState.Should().Be(WorkflowState.Under_Review);
    }

    [Fact]
    public async Task RejectedEntry_CannotBeModified()
    {
        // Property 19 extended: Rejected entries are terminal
        var (service, _) = await SetupAsync(nameof(RejectedEntry_CannotBeModified));
        var create = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        await service.RejectEntryAsync(create.Data.Id, 2, 2, "Entity_Admin", "بيانات خاطئة");

        var result = await service.UpdateEntryAsync(create.Data.Id, new UpdateIndicatorEntryRequest { ValueNumeric = 999 }, 1, 2, "Data_Entry_User");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RejectedEntry_AllowsNewEntryForSamePeriod()
    {
        // Rejected entries excluded from unique constraint — new entry can be created
        var (service, _) = await SetupAsync(nameof(RejectedEntry_AllowsNewEntryForSamePeriod));
        var create = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        await service.RejectEntryAsync(create.Data.Id, 2, 2, "Entity_Admin", "بيانات خاطئة");

        // Create new entry for same indicator/entity/period — should succeed
        var result = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeletedEntry_ExcludedFromQueries()
    {
        // Property 38: soft deleted entries excluded from user queries
        var (service, db) = await SetupAsync(nameof(SoftDeletedEntry_ExcludedFromQueries));
        var create = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        await service.SoftDeleteEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");

        var result = await service.GetEntriesAsync(entityId: 2);
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SoftDeletedEntry_CascadesToAttachments()
    {
        // Property 22: attachments marked deleted when entry is soft deleted
        var (service, db) = await SetupAsync(nameof(SoftDeletedEntry_CascadesToAttachments));
        var create = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);

        // Add an attachment directly
        db.Attachments.Add(new Domain.Entities.Attachment
        {
            IndicatorEntryId = create.Data!.Id, FileName = "test.pdf", FilePath = "/tmp/test.pdf",
            FileType = ".pdf", FileSize = 1024, UploadedBy = 1, UploadedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await service.SoftDeleteEntryAsync(create.Data.Id, 1, 2, "Data_Entry_User");

        var attachment = await db.Attachments.FirstAsync(a => a.IndicatorEntryId == create.Data.Id);
        attachment.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivatedEntity_PreventsEntryCreation()
    {
        // Property 5: deactivated entities cannot create entries
        var (service, db) = await SetupAsync(nameof(DeactivatedEntity_PreventsEntryCreation));
        var entity = await db.Entities.FindAsync(2);
        entity!.Status = "inactive";
        await db.SaveChangesAsync();

        var result = await service.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("معطّلة");
    }
}
