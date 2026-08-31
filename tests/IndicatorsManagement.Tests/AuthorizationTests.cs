using FluentAssertions;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Infrastructure.Services;
using Xunit;

namespace IndicatorsManagement.Tests;

/// <summary>
/// T2 — object-level authorization tests (finding S5). Confirms that the by-id
/// entry-point methods refuse cross-entity access by returning "not found" rather
/// than exposing or mutating another entity's row.
/// </summary>
public class AuthorizationTests
{
    private async Task<(IndicatorEntryService svc, Infrastructure.Data.IndicatorsDbContext db, int entryId)> BuildAsync(string name)
    {
        var db = TestDbContextFactory.Create(name);
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var notif = new NotificationService(db);
        var svc = new IndicatorEntryService(db, audit, notif);
        var created = await svc.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1, ValueNumeric = 42 },
            userId: 1, userEntityId: 2);
        created.Success.Should().BeTrue(created.Message);
        return (svc, db, created.Data!.Id);
    }

    [Fact]
    public async Task GetEntryById_CrossEntity_ReturnsNotFound()
    {
        var (svc, _, id) = await BuildAsync(nameof(GetEntryById_CrossEntity_ReturnsNotFound));
        // Caller belongs to entity 99 with an entity-scoped role — the row lives in entity 2.
        var result = await svc.GetEntryByIdAsync(id, userEntityId: 99, userRole: Roles.DataEntryUser);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("غير موجود");
    }

    [Fact]
    public async Task GetEntryById_SameEntity_Succeeds()
    {
        var (svc, _, id) = await BuildAsync(nameof(GetEntryById_SameEntity_Succeeds));
        var result = await svc.GetEntryByIdAsync(id, userEntityId: 2, userRole: Roles.DataEntryUser);
        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetEntryById_MinistryAdmin_SeesAnyEntity()
    {
        var (svc, _, id) = await BuildAsync(nameof(GetEntryById_MinistryAdmin_SeesAnyEntity));
        // Ministry_Admin is not entity-scoped — passing a bogus entity id must still succeed.
        var result = await svc.GetEntryByIdAsync(id, userEntityId: 42, userRole: Roles.MinistryAdmin);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEntry_CrossEntity_ReturnsNotFound()
    {
        var (svc, _, id) = await BuildAsync(nameof(UpdateEntry_CrossEntity_ReturnsNotFound));
        var result = await svc.UpdateEntryAsync(id,
            new UpdateIndicatorEntryRequest { ValueNumeric = 999 },
            userId: 55, userEntityId: 99, userRole: Roles.DataEntryUser);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDelete_CrossEntity_ReturnsNotFound()
    {
        var (svc, _, id) = await BuildAsync(nameof(SoftDelete_CrossEntity_ReturnsNotFound));
        var result = await svc.SoftDeleteEntryAsync(id, userId: 55, userEntityId: 99, userRole: Roles.DataEntryUser);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Submit_CrossEntity_ReturnsNotFound()
    {
        var (svc, _, id) = await BuildAsync(nameof(Submit_CrossEntity_ReturnsNotFound));
        var result = await svc.SubmitEntryAsync(id, userId: 55, userEntityId: 99, userRole: Roles.DataEntryUser);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveEntity_SelfApproval_IsBlocked()
    {
        // S4 — the same user who created the entry cannot approve it themselves.
        var (svc, _, id) = await BuildAsync(nameof(ApproveEntity_SelfApproval_IsBlocked));
        await svc.SubmitEntryAsync(id, userId: 1, userEntityId: 2, userRole: Roles.DataEntryUser);

        var result = await svc.ApproveEntityLevelAsync(id, userId: 1, userEntityId: 2, userRole: Roles.EntityAdmin, notes: null);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("لا يمكنك اعتماد");
    }
}
