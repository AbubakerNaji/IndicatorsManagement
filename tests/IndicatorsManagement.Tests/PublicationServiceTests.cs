using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Tests;

/// <summary>
/// V2.1 Publication Control Tests
/// Property 1: Publication requires Final_Approved state
/// Property 2: Publish transitions to Published
/// Property 3: Unpublish transitions to Unpublished
/// Property 4: Cannot publish non-Final_Approved entries
/// Property 5: Publication logged in audit
/// </summary>
public class PublicationServiceTests
{
    private async Task<(PublicationService pub, IndicatorEntryService entry, Infrastructure.Data.IndicatorsDbContext db)> SetupAsync(string testName)
    {
        var db = TestDbContextFactory.Create(testName);
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var notification = new NotificationService(db);
        var entry = new IndicatorEntryService(db, audit, notification);
        var pub = new PublicationService(db, audit, notification);
        return (pub, entry, db);
    }

    private async Task<int> CreateApprovedEntry(IndicatorEntryService entry)
    {
        var create = await entry.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1, ValueNumeric = 100 }, 1, 2);
        await entry.SubmitEntryAsync(create.Data!.Id, 1, 2, "Data_Entry_User");
        await entry.ApproveEntityLevelAsync(create.Data.Id, 2, 2, "Entity_Admin", null);
        await entry.ApproveMinistryLevelAsync(create.Data.Id, 3, null);
        return create.Data.Id;
    }

    [Fact]
    public async Task PublishEntry_FinalApproved_ShouldSucceed()
    {
        var (pub, entry, db) = await SetupAsync(nameof(PublishEntry_FinalApproved_ShouldSucceed));
        var entryId = await CreateApprovedEntry(entry);

        var result = await pub.PublishEntryAsync(entryId, 1, null);
        result.Success.Should().BeTrue();

        var updated = await db.IndicatorEntries.FindAsync(entryId);
        updated!.PublicationStatus.Should().Be(PublicationStatus.Published);
    }

    [Fact]
    public async Task PublishEntry_Draft_ShouldFail()
    {
        var (pub, entry, _) = await SetupAsync(nameof(PublishEntry_Draft_ShouldFail));
        var create = await entry.CreateEntryAsync(new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);

        var result = await pub.PublishEntryAsync(create.Data!.Id, 1, null);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UnpublishEntry_ShouldSucceed()
    {
        var (pub, entry, db) = await SetupAsync(nameof(UnpublishEntry_ShouldSucceed));
        var entryId = await CreateApprovedEntry(entry);
        await pub.PublishEntryAsync(entryId, 1, null);

        var result = await pub.UnpublishEntryAsync(entryId, 1, "مراجعة إضافية");
        result.Success.Should().BeTrue();

        var updated = await db.IndicatorEntries.FindAsync(entryId);
        updated!.PublicationStatus.Should().Be(PublicationStatus.Unpublished);
    }

    [Fact]
    public async Task UnpublishEntry_NotPublished_ShouldFail()
    {
        var (pub, entry, _) = await SetupAsync(nameof(UnpublishEntry_NotPublished_ShouldFail));
        var entryId = await CreateApprovedEntry(entry);

        var result = await pub.UnpublishEntryAsync(entryId, 1, null);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task PublishEntry_AlreadyPublished_ShouldFail()
    {
        var (pub, entry, _) = await SetupAsync(nameof(PublishEntry_AlreadyPublished_ShouldFail));
        var entryId = await CreateApprovedEntry(entry);
        await pub.PublishEntryAsync(entryId, 1, null);

        var result = await pub.PublishEntryAsync(entryId, 1, null);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task PublishEntry_ShouldCreateHistory()
    {
        var (pub, entry, db) = await SetupAsync(nameof(PublishEntry_ShouldCreateHistory));
        var entryId = await CreateApprovedEntry(entry);

        await pub.PublishEntryAsync(entryId, 1, "نشر أولي");

        var history = await db.PublicationHistories.Where(h => h.IndicatorEntryId == entryId).ToListAsync();
        history.Should().HaveCount(1);
        history[0].Action.Should().Be("Published");
    }

    [Fact]
    public async Task BulkPublish_ShouldPublishMultipleEntries()
    {
        var (pub, entry, db) = await SetupAsync(nameof(BulkPublish_ShouldPublishMultipleEntries));
        var entryId = await CreateApprovedEntry(entry);

        var result = await pub.BulkPublishAsync([entryId], 1, "نشر جماعي");
        result.Success.Should().BeTrue();
        result.Data.Should().Be(1);
    }

    [Fact]
    public async Task GetPublicationHistory_ShouldReturnHistory()
    {
        var (pub, entry, _) = await SetupAsync(nameof(GetPublicationHistory_ShouldReturnHistory));
        var entryId = await CreateApprovedEntry(entry);
        await pub.PublishEntryAsync(entryId, 1, null);
        await pub.UnpublishEntryAsync(entryId, 1, "مراجعة");

        var result = await pub.GetPublicationHistoryAsync(entryId);
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task ViewerFiltering_OnlyPublished()
    {
        var (pub, entry, db) = await SetupAsync(nameof(ViewerFiltering_OnlyPublished));
        var entryId = await CreateApprovedEntry(entry);

        // Before publishing — publishedOnly returns empty
        var beforeResult = await entry.GetEntriesAsync(publishedOnly: true);
        beforeResult.Data!.Items.Should().BeEmpty();

        // After publishing
        await pub.PublishEntryAsync(entryId, 1, null);
        var afterResult = await entry.GetEntriesAsync(publishedOnly: true);
        afterResult.Data!.Items.Should().HaveCount(1);
    }
}
