using FluentAssertions;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;

namespace IndicatorsManagement.Tests;

public class IndicatorServiceExtendedTests
{
    [Fact]
    public async Task CreateIndicator_ValidRequest_ShouldSucceed()
    {
        var db = TestDbContextFactory.Create(nameof(CreateIndicator_ValidRequest_ShouldSucceed));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorService(db, audit);

        var request = new CreateIndicatorRequest
        {
            Code = "NEW-001",
            NameAr = "مؤشر جديد",
            DefinitionAr = "تعريف المؤشر",
            CalculationMethodAr = "طريقة حساب",
            UnitAr = "نسبة مئوية",
            DataSourceAr = "الإدارة المالية",
            ObjectiveAr = "هدف المؤشر",
            PublicationFrequency = PublicationFrequency.Quarterly,
            RequiresReview = true
        };

        var result = await service.CreateIndicatorAsync(request, 1);
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Code.Should().Be("NEW-001");
        result.Data.PublicationFrequency.Should().Be(PublicationFrequency.Quarterly);
    }

    [Fact]
    public async Task UpdateIndicator_ShouldPreserveCode()
    {
        var db = TestDbContextFactory.Create(nameof(UpdateIndicator_ShouldPreserveCode));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorService(db, audit);

        var update = new UpdateIndicatorRequest
        {
            NameAr = "اسم محدث",
            DefinitionAr = "تعريف محدث",
            CalculationMethodAr = "طريقة محدثة",
            UnitAr = "عدد",
            DataSourceAr = "مصدر محدث",
            PublicationFrequency = PublicationFrequency.Annual
        };

        var result = await service.UpdateIndicatorAsync(1, update, 1);
        result.Success.Should().BeTrue();
        result.Data!.Code.Should().Be("IND-001", "code should not change on update");
        result.Data.NameAr.Should().Be("اسم محدث");
    }

    [Fact]
    public async Task GetIndicators_Pagination_ShouldWork()
    {
        var db = TestDbContextFactory.Create(nameof(GetIndicators_Pagination_ShouldWork));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorService(db, audit);

        // Add more indicators
        for (int i = 2; i <= 15; i++)
        {
            await service.CreateIndicatorAsync(new CreateIndicatorRequest
            {
                Code = $"IND-{i:D3}", NameAr = $"مؤشر {i}", DefinitionAr = "تعريف",
                CalculationMethodAr = "طريقة", UnitAr = "عدد", DataSourceAr = "مصدر",
                PublicationFrequency = PublicationFrequency.Monthly
            }, 1);
        }

        var page1 = await service.GetIndicatorsAsync(page: 1, pageSize: 5);
        page1.Data!.Items.Should().HaveCount(5);
        page1.Data.TotalCount.Should().Be(15);
        page1.Data.TotalPages.Should().Be(3);

        var page3 = await service.GetIndicatorsAsync(page: 3, pageSize: 5);
        page3.Data!.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetIndicatorById_NonExistent_ShouldFail()
    {
        var db = TestDbContextFactory.Create(nameof(GetIndicatorById_NonExistent_ShouldFail));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorService(db, audit);

        var result = await service.GetIndicatorByIdAsync(999);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteIndicator_NoAssignments_ShouldSucceed()
    {
        var db = TestDbContextFactory.Create(nameof(DeleteIndicator_NoAssignments_ShouldSucceed));
        await TestDbContextFactory.SeedBasicData(db);
        var audit = new AuditLogService(db);
        var service = new IndicatorService(db, audit);

        // Create indicator without assignments
        var created = await service.CreateIndicatorAsync(new CreateIndicatorRequest
        {
            Code = "DEL-001", NameAr = "مؤشر للحذف", DefinitionAr = "تعريف",
            CalculationMethodAr = "طريقة", UnitAr = "عدد", DataSourceAr = "مصدر",
            PublicationFrequency = PublicationFrequency.Annual
        }, 1);

        var result = await service.DeleteIndicatorAsync(created.Data!.Id, 1);
        result.Success.Should().BeTrue();
    }
}
