using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IndicatorsManagement.Tests;

public static class TestDbContextFactory
{
    public static IndicatorsDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<IndicatorsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new IndicatorsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task SeedBasicData(IndicatorsDbContext db)
    {
        // Seed user
        db.Users.Add(new ApplicationUser
        {
            Id = 1, UserName = "testuser", NormalizedUserName = "TESTUSER",
            Email = "test@test.com", NormalizedEmail = "TEST@TEST.COM",
            FullNameAr = "مستخدم تجريبي", EntityId = 2, IsActive = true, SecurityStamp = Guid.NewGuid().ToString()
        });

        // Seed entity
        db.Entities.Add(new Entity { Id = 1, NameAr = "وزارة الاقتصاد", Type = Domain.Enums.EntityType.Ministry, Status = "active" });
        db.Entities.Add(new Entity { Id = 2, NameAr = "مصلحة الجمارك", Type = Domain.Enums.EntityType.Authority, ParentEntityId = 1, Status = "active" });

        // Seed indicator
        db.Indicators.Add(new Indicator
        {
            Id = 1, Code = "IND-001", NameAr = "مؤشر تجريبي", DefinitionAr = "تعريف",
            CalculationMethodAr = "طريقة", UnitAr = "عدد", DataSourceAr = "مصدر",
            PublicationFrequency = Domain.Enums.PublicationFrequency.Monthly, IsActive = true, CreatedBy = 1
        });

        // Seed reporting period
        db.ReportingPeriods.Add(new ReportingPeriod
        {
            Id = 1, PeriodType = Domain.Enums.PeriodType.Monthly, Year = 2026, Month = 1,
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31),
            DisplayNameAr = "يناير 2026", IsOpen = true
        });

        // Seed assignment
        db.IndicatorAssignments.Add(new IndicatorAssignment
        {
            Id = 1, IndicatorId = 1, EntityId = 2, StartDate = new DateOnly(2025, 1, 1),
            ReportingFrequency = Domain.Enums.PeriodType.Monthly, IsActive = true
        });

        await db.SaveChangesAsync();
    }
}
