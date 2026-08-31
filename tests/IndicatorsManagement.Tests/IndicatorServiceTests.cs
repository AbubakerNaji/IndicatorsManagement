using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Property 1: Indicator Code Uniqueness (Req 1.2)
/// Property 4: Indicator Deletion Protection (Req 1.5)
/// </summary>
public class IndicatorServiceTests
{
    [Fact]
    public async Task CreateIndicator_DuplicateCode_ShouldFail()
    {
        // Property 1: duplicate codes are rejected
        var db = TestDbContextFactory.Create(nameof(CreateIndicator_DuplicateCode_ShouldFail));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorService(db, audit);

        var request = new CreateIndicatorRequest
        {
            Code = "IND-001", // Already exists
            NameAr = "مؤشر جديد", DefinitionAr = "تعريف", CalculationMethodAr = "طريقة",
            UnitAr = "عدد", DataSourceAr = "مصدر", PublicationFrequency = PublicationFrequency.Monthly
        };

        var result = await service.CreateIndicatorAsync(request, 1);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("مستخدم مسبقاً");
    }

    [Fact]
    public async Task DeleteIndicator_WithAssignments_ShouldFail()
    {
        // Property 4: indicators with assignments cannot be deleted
        var db = TestDbContextFactory.Create(nameof(DeleteIndicator_WithAssignments_ShouldFail));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorService(db, audit);

        var result = await service.DeleteIndicatorAsync(1, 1);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("تكليفات");
    }
}
