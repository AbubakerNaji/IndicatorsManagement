using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Security;
using IndicatorsManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndicatorsManagement.Tests;

// S10 — verifies the tamper-evident audit hash chain.
public class AuditChainTests
{
    [Fact]
    public async Task Chain_Is_Valid_After_Sequential_Writes()
    {
        var db = TestDbContextFactory.Create(nameof(Chain_Is_Valid_After_Sequential_Writes));
        var svc = new AuditLogService(db);

        await svc.LogAsync(1, "Indicator", 1, "Create");
        await svc.LogAsync(1, "Indicator", 1, "Update", oldValues: "{\"a\":1}", newValues: "{\"a\":2}");
        await svc.LogAsync(1, "IndicatorEntry", 42, "Submit");

        var report = await svc.VerifyChainAsync();
        Assert.True(report.IsValid, report.BreakReason);
        Assert.Equal(3, report.TotalRows);
    }

    [Fact]
    public async Task Chain_Detects_Field_Mutation()
    {
        var db = TestDbContextFactory.Create(nameof(Chain_Detects_Field_Mutation));
        var svc = new AuditLogService(db);

        await svc.LogAsync(1, "Indicator", 1, "Create");
        await svc.LogAsync(1, "Indicator", 1, "Update");
        await svc.LogAsync(1, "Indicator", 1, "Approve");

        // Attacker tampers directly with a stored row (bypasses the service).
        var victim = await db.AuditLogs.OrderBy(a => a.Id).Skip(1).FirstAsync();
        victim.ActionType = "Delete";
        await db.SaveChangesAsync();

        var report = await svc.VerifyChainAsync();
        Assert.False(report.IsValid);
        Assert.Equal(victim.Id, report.FirstBrokenRowId);
    }

    [Fact]
    public async Task Chain_Detects_Row_Deletion()
    {
        var db = TestDbContextFactory.Create(nameof(Chain_Detects_Row_Deletion));
        var svc = new AuditLogService(db);

        await svc.LogAsync(1, "Indicator", 1, "Create");
        await svc.LogAsync(1, "Indicator", 1, "Update");
        await svc.LogAsync(1, "Indicator", 1, "Approve");

        // Attacker deletes the middle row.
        var victim = await db.AuditLogs.OrderBy(a => a.Id).Skip(1).FirstAsync();
        db.AuditLogs.Remove(victim);
        await db.SaveChangesAsync();

        var report = await svc.VerifyChainAsync();
        Assert.False(report.IsValid);
    }

    [Fact]
    public void Hasher_Is_Deterministic()
    {
        var row = new AuditLog
        {
            UserId = 5,
            EntityType = "Indicator",
            EntityId = 7,
            ActionType = "Approve",
            OldValuesJson = "{\"x\":1}",
            NewValuesJson = "{\"x\":2}",
            IpAddress = "10.0.0.1",
            ResultStatus = "Success",
            CreatedAt = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        var a = AuditChainHasher.ComputeHash(row, "previous");
        var b = AuditChainHasher.ComputeHash(row, "previous");
        Assert.Equal(a, b);
        Assert.Equal(64, a.Length); // SHA-256 hex

        var different = AuditChainHasher.ComputeHash(row, "different");
        Assert.NotEqual(a, different);
    }
}
