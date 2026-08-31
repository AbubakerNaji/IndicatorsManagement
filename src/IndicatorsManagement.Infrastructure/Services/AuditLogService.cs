using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Data;
using IndicatorsManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IndicatorsDbContext _db;
    // S10 — serialize writes so two concurrent LogAsync calls can't race on "the latest row".
    private static readonly SemaphoreSlim ChainLock = new(1, 1);

    public AuditLogService(IndicatorsDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int? userId, string entityType, int? entityId, string actionType,
        string? oldValues = null, string? newValues = null, string? ipAddress = null,
        string resultStatus = "Success", string? errorCode = null, string? errorMessage = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            ActionType = actionType,
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            IpAddress = ipAddress,
            ResultStatus = resultStatus,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };

        await ChainLock.WaitAsync();
        try
        {
            // S10 — chain against the last stored row so tampering with any earlier row
            // breaks verification.
            var lastHash = await _db.AuditLogs
                .OrderByDescending(a => a.Id)
                .Select(a => a.RowHash)
                .FirstOrDefaultAsync() ?? string.Empty;

            log.PreviousHash = lastHash;
            log.RowHash = AuditChainHasher.ComputeHash(log, lastHash);

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        finally
        {
            ChainLock.Release();
        }
    }

    public async Task<AuditChainVerification> VerifyChainAsync()
    {
        var report = new AuditChainVerification();
        string previousHash = string.Empty;
        long count = 0;

        // Stream in Id order so we don't load the whole audit log into memory.
        await foreach (var row in _db.AuditLogs.AsNoTracking().OrderBy(a => a.Id).AsAsyncEnumerable())
        {
            count++;

            if (row.PreviousHash != previousHash)
            {
                report.TotalRows = count;
                report.IsValid = false;
                report.FirstBrokenRowId = row.Id;
                report.BreakReason = "PreviousHash does not match the prior row's RowHash (a row was inserted, deleted, or reordered)";
                return report;
            }

            var expected = AuditChainHasher.ComputeHash(row, previousHash);
            if (!string.Equals(expected, row.RowHash, StringComparison.OrdinalIgnoreCase))
            {
                report.TotalRows = count;
                report.IsValid = false;
                report.FirstBrokenRowId = row.Id;
                report.BreakReason = "RowHash does not match the row's contents (a field was mutated after logging)";
                return report;
            }

            previousHash = row.RowHash;
        }

        report.TotalRows = count;
        report.IsValid = true;
        return report;
    }
}
