using FluentAssertions;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Verifies the SeedData static class produces correct data
/// matching the official indicators guide document.
/// </summary>
public class SeedDataTests
{
    [Fact]
    public void SeedData_Should_Have_14_Entities()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        data.Should().HaveCount(14, "the guide has 14 child entities under the Ministry");
    }

    [Fact]
    public void SeedData_Should_Have_120_Total_Indicators()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        var totalIndicators = data.Sum(d => d.Indicators.Count);
        totalIndicators.Should().Be(120, "the official guide has 120 indicators total");
    }

    [Fact]
    public void SeedData_Indicator_Codes_Should_Be_Unique()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        var allCodes = data.SelectMany(d => d.Indicators).Select(i => i.Code).ToList();
        allCodes.Should().OnlyHaveUniqueItems("each indicator must have a unique code");
    }

    [Fact]
    public void SeedData_All_Indicators_Should_Have_Required_Fields()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        var allIndicators = data.SelectMany(d => d.Indicators).ToList();

        foreach (var ind in allIndicators)
        {
            ind.Code.Should().NotBeNullOrWhiteSpace($"indicator must have a code");
            ind.NameAr.Should().NotBeNullOrWhiteSpace($"indicator {ind.Code} must have NameAr");
            ind.DefinitionAr.Should().NotBeNullOrWhiteSpace($"indicator {ind.Code} must have DefinitionAr");
            ind.CalculationMethodAr.Should().NotBeNullOrWhiteSpace($"indicator {ind.Code} must have CalculationMethodAr");
            ind.UnitAr.Should().NotBeNullOrWhiteSpace($"indicator {ind.Code} must have UnitAr");
            ind.DataSourceAr.Should().NotBeNullOrWhiteSpace($"indicator {ind.Code} must have DataSourceAr");
            ind.ObjectiveAr.Should().NotBeNullOrWhiteSpace($"indicator {ind.Code} must have ObjectiveAr");
        }
    }

    [Fact]
    public void SeedData_Food_Security_Should_Have_12_Indicators()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        var foodSecurity = data.First(d => d.Entity.NameAr == "مكتب الأمن الغذائي");
        foodSecurity.Indicators.Should().HaveCount(12);
        foodSecurity.Indicators.First().Code.Should().Be("F-01");
        foodSecurity.Indicators.Last().Code.Should().Be("F-12");
    }

    [Fact]
    public void SeedData_Credit_Guarantee_Should_Have_14_Indicators()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        var creditGuarantee = data.First(d => d.Entity.NameAr == "صندوق ضمان الائتمان");
        creditGuarantee.Indicators.Should().HaveCount(14);
        creditGuarantee.Entity.Type.Should().Be(EntityType.Fund);
    }

    [Fact]
    public void SeedData_Trade_Network_Should_Be_Network_Type()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        var tradeNetwork = data.First(d => d.Entity.NameAr == "شبكة ليبيا للتجارة");
        tradeNetwork.Entity.Type.Should().Be(EntityType.Network);
        tradeNetwork.Indicators.Should().HaveCount(6);
    }

    [Fact]
    public void SeedData_All_Entities_Should_Have_Active_Status()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        foreach (var (entity, _) in data)
        {
            entity.Status.Should().Be("active");
            entity.NameAr.Should().NotBeNullOrWhiteSpace();
            entity.NameEn.Should().NotBeNullOrWhiteSpace("all entities should have English names");
        }
    }

    [Theory]
    [InlineData("مكتب الأمن الغذائي", 12)]
    [InlineData("إدارة التفتيش وحماية المستهلك", 12)]
    [InlineData("مكتب التعاون الدولي", 12)]
    [InlineData("مكتب دعم وتمكين المرأة", 4)]
    [InlineData("مصلحة السجل التجاري", 7)]
    [InlineData("مكتب الوكالات التجارية", 10)]
    [InlineData("مكتب العلامات التجارية", 9)]
    [InlineData("صندوق ضمان الائتمان", 14)]
    [InlineData("هيئة الإشراف على التأمين", 5)]
    [InlineData("الهيئة العامة للمعارض", 6)]
    [InlineData("هيئة تنمية الصادرات الليبية", 12)]
    [InlineData("شبكة ليبيا للتجارة", 6)]
    [InlineData("الهيئة العامة لتشجيع الاستثمار وشؤون الخصخصة", 6)]
    [InlineData("هيئة سوق المال الليبي", 5)]
    public void SeedData_Entity_Should_Have_Correct_Indicator_Count(string entityName, int expectedCount)
    {
        var data = SeedData.GetEntitiesWithIndicators();
        var entity = data.First(d => d.Entity.NameAr == entityName);
        entity.Indicators.Should().HaveCount(expectedCount,
            $"{entityName} should have {expectedCount} indicators per the guide");
    }

    [Fact]
    public void SeedData_PublicationFrequency_Should_Be_Valid()
    {
        var data = SeedData.GetEntitiesWithIndicators();
        var allIndicators = data.SelectMany(d => d.Indicators).ToList();
        var validFrequencies = new[] { PublicationFrequency.Monthly, PublicationFrequency.Quarterly, PublicationFrequency.Semi_Annual, PublicationFrequency.Annual };

        foreach (var ind in allIndicators)
        {
            validFrequencies.Should().Contain(ind.PublicationFrequency,
                $"indicator {ind.Code} should have a valid frequency");
        }
    }
}
