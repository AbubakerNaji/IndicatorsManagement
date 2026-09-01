using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IndicatorsManagement.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IndicatorsDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        await context.Database.MigrateAsync();
        await SeedRolesAsync(roleManager);
        await SeedSystemConfigurationAsync(context);
        await SeedEntitiesAsync(context);
        await SeedIndicatorsAsync(context);
        await SeedIndicatorAssignmentsAsync(context);
        await SeedReportingPeriodsAsync(context);
        await SeedAdminUserAsync(userManager, configuration, environment);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        string[] roles = ["Super_Admin", "Ministry_Admin", "Entity_Admin", "Data_Entry_User", "Reviewer", "Auditor", "Viewer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
        }
    }

    private static async Task SeedSystemConfigurationAsync(IndicatorsDbContext context)
    {
        if (await context.SystemConfigurations.AnyAsync()) return;

        context.SystemConfigurations.AddRange(
            new SystemConfiguration { ConfigKey = "NotificationThreshold_Days_7", ConfigValue = "7", Description = "إرسال تنبيه قبل 7 أيام من الموعد النهائي" },
            new SystemConfiguration { ConfigKey = "NotificationThreshold_Days_3", ConfigValue = "3", Description = "إرسال تنبيه قبل 3 أيام من الموعد النهائي" },
            new SystemConfiguration { ConfigKey = "NotificationThreshold_Days_1", ConfigValue = "1", Description = "إرسال تنبيه قبل يوم واحد من الموعد النهائي" },
            new SystemConfiguration { ConfigKey = "DashboardRefreshInterval_Minutes", ConfigValue = "5", Description = "فترة تحديث لوحة المتابعة (بالدقائق)" },
            new SystemConfiguration { ConfigKey = "FileUploadMaxSize_MB", ConfigValue = "10", Description = "الحد الأقصى لحجم الملف المرفق (ميغابايت)" },
            new SystemConfiguration { ConfigKey = "SessionTimeout_Minutes", ConfigValue = "30", Description = "مهلة انتهاء الجلسة (بالدقائق)" }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedEntitiesAsync(IndicatorsDbContext context)
    {
        if (await context.Entities.AnyAsync()) return;

        // Create the ministry (parent entity)
        var ministry = new Entity
        {
            NameAr = "وزارة الاقتصاد والتجارة",
            NameEn = "Ministry of Economy and Trade",
            Type = EntityType.Ministry,
            Status = "active"
        };
        context.Entities.Add(ministry);
        await context.SaveChangesAsync();

        // Create all child entities from the official indicators guide
        var entitiesWithIndicators = SeedData.GetEntitiesWithIndicators();
        foreach (var (entity, _) in entitiesWithIndicators)
        {
            entity.ParentEntityId = ministry.Id;
            context.Entities.Add(entity);
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedIndicatorsAsync(IndicatorsDbContext context)
    {
        if (await context.Indicators.AnyAsync()) return;

        var entitiesWithIndicators = SeedData.GetEntitiesWithIndicators();
        foreach (var (_, indicators) in entitiesWithIndicators)
        {
            context.Indicators.AddRange(indicators);
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedIndicatorAssignmentsAsync(IndicatorsDbContext context)
    {
        if (await context.IndicatorAssignments.AnyAsync()) return;

        var entitiesWithIndicators = SeedData.GetEntitiesWithIndicators();
        var startDate = new DateOnly(2024, 1, 1);

        foreach (var (seedEntity, seedIndicators) in entitiesWithIndicators)
        {
            // Find the persisted entity by Arabic name
            var entity = await context.Entities.FirstOrDefaultAsync(e => e.NameAr == seedEntity.NameAr);
            if (entity == null) continue;

            foreach (var seedIndicator in seedIndicators)
            {
                // Find the persisted indicator by code
                var indicator = await context.Indicators.FirstOrDefaultAsync(i => i.Code == seedIndicator.Code);
                if (indicator == null) continue;

                // Map PublicationFrequency to PeriodType for reporting frequency
                var reportingFrequency = seedIndicator.PublicationFrequency switch
                {
                    PublicationFrequency.Monthly => PeriodType.Monthly,
                    PublicationFrequency.Quarterly => PeriodType.Quarterly,
                    PublicationFrequency.Semi_Annual => PeriodType.Semi_Annual,
                    PublicationFrequency.Annual => PeriodType.Annual,
                    _ => PeriodType.Annual
                };

                context.IndicatorAssignments.Add(new IndicatorAssignment
                {
                    IndicatorId = indicator.Id,
                    EntityId = entity.Id,
                    StartDate = startDate,
                    ReportingFrequency = reportingFrequency,
                    IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedReportingPeriodsAsync(IndicatorsDbContext context)
    {
        if (await context.ReportingPeriods.AnyAsync()) return;

        var periods = new List<ReportingPeriod>();
        foreach (var year in new[] { 2024, 2025, 2026 })
        {
            // Monthly
            for (int m = 1; m <= 12; m++)
            {
                var monthNames = new[] { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
                periods.Add(new ReportingPeriod
                {
                    PeriodType = PeriodType.Monthly, Year = year, Month = m,
                    StartDate = new DateOnly(year, m, 1),
                    EndDate = new DateOnly(year, m, DateTime.DaysInMonth(year, m)),
                    DisplayNameAr = $"{monthNames[m - 1]} {year}"
                });
            }

            // Quarterly
            for (int q = 1; q <= 4; q++)
            {
                int startMonth = (q - 1) * 3 + 1;
                periods.Add(new ReportingPeriod
                {
                    PeriodType = PeriodType.Quarterly, Year = year, Quarter = q,
                    StartDate = new DateOnly(year, startMonth, 1),
                    EndDate = new DateOnly(year, startMonth + 2, DateTime.DaysInMonth(year, startMonth + 2)),
                    DisplayNameAr = $"الربع {q} - {year}"
                });
            }

            // Semi-Annual
            for (int h = 1; h <= 2; h++)
            {
                int startMonth = (h - 1) * 6 + 1;
                periods.Add(new ReportingPeriod
                {
                    PeriodType = PeriodType.Semi_Annual, Year = year, HalfYear = h,
                    StartDate = new DateOnly(year, startMonth, 1),
                    EndDate = new DateOnly(year, startMonth + 5, DateTime.DaysInMonth(year, startMonth + 5)),
                    DisplayNameAr = $"النصف {(h == 1 ? "الأول" : "الثاني")} - {year}"
                });
            }

            // Annual
            periods.Add(new ReportingPeriod
            {
                PeriodType = PeriodType.Annual, Year = year,
                StartDate = new DateOnly(year, 1, 1),
                EndDate = new DateOnly(year, 12, 31),
                DisplayNameAr = $"السنة {year}"
            });
        }

        context.ReportingPeriods.AddRange(periods);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        const string adminEmail = "admin@indicators.gov";
        if (await userManager.FindByEmailAsync(adminEmail) != null) return;

        // S8 — the admin password comes from configuration (ADMIN_PASSWORD env var or config file).
        // In Development we fall back to the well-known dev password so the first-time setup
        // still works out of the box. Outside Development we refuse to seed rather than
        // burn a public password into the database.
        var password = configuration["ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (string.IsNullOrEmpty(password))
        {
            if (environment.IsDevelopment())
            {
                password = "Admin@123456"; // Development fallback only.
            }
            else
            {
                throw new InvalidOperationException(
                    "ADMIN_PASSWORD must be provided outside Development to seed the initial administrator account.");
            }
        }

        var admin = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            FullNameAr = "مدير النظام",
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Super_Admin");
        }
    }
}
