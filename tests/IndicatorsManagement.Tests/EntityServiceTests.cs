using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Property 2: Entity Name Uniqueness (Req 2.2)
/// Property 5: Entity Deactivation Prevents Entry Creation (Req 2.4)
/// </summary>
public class EntityServiceTests
{
    [Fact]
    public async Task CreateEntity_DuplicateName_ShouldFail()
    {
        // Property 2: duplicate entity names are rejected
        var db = TestDbContextFactory.Create(nameof(CreateEntity_DuplicateName_ShouldFail));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new EntityService(db, audit);

        var request = new CreateEntityRequest
        {
            NameAr = "وزارة الاقتصاد", // Already exists
            Type = EntityType.Ministry
        };

        var result = await service.CreateEntityAsync(request, 1);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("مستخدم مسبقاً");
    }

    [Fact]
    public async Task DeactivatedEntity_ShouldPreventEntryCreation()
    {
        // Property 5: deactivated entities cannot create entries
        var db = TestDbContextFactory.Create(nameof(DeactivatedEntity_ShouldPreventEntryCreation));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var notification = new NotificationService(db);

        // Deactivate entity
        var entityService = new EntityService(db, audit);
        await entityService.DeactivateEntityAsync(2, 1);

        // Try to create entry for deactivated entity
        var entryService = new IndicatorEntryService(db, audit, notification);
        var result = await entryService.CreateEntryAsync(
            new CreateIndicatorEntryRequest { IndicatorId = 1, ReportingPeriodId = 1 }, 1, 2);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("معطّلة");
    }
}
