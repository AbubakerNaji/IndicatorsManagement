using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IndicatorsManagement.Domain.Entities;

namespace IndicatorsManagement.Infrastructure.Security;

/// <summary>
/// S10 — computes and verifies the tamper-evident hash chain over audit_logs.
///
/// Design: every row's <see cref="AuditLog.RowHash"/> is SHA-256 of
/// (<see cref="AuditLog.PreviousHash"/> || canonical field bytes). PreviousHash is the
/// previous row's RowHash (empty string for row #1). Mutating or deleting any historical
/// row breaks the chain from that row onwards, which the verifier detects.
///
/// This gives the audit log the "you can add, but you can't quietly rewrite" property
/// even against operators with direct DB access — as long as they can't rewrite EVERY
/// subsequent row, the chain surfaces the tamper.
/// </summary>
public static class AuditChainHasher
{
    /// <summary>Compute a row's canonical hash given the previous row's hash.</summary>
    public static string ComputeHash(AuditLog row, string previousHash)
    {
        // Canonicalization: order matters. Everything is UTF-8, ISO-8601, invariant culture.
        var canonical = new StringBuilder()
            .Append(previousHash).Append('|')
            .Append(row.UserId?.ToString(CultureInfo.InvariantCulture) ?? "").Append('|')
            .Append(row.EntityType).Append('|')
            .Append(row.EntityId?.ToString(CultureInfo.InvariantCulture) ?? "").Append('|')
            .Append(row.ActionType).Append('|')
            .Append(row.ResultStatus).Append('|')
            .Append(row.ErrorCode ?? "").Append('|')
            .Append(row.ErrorMessage ?? "").Append('|')
            .Append(row.OldValuesJson ?? "").Append('|')
            .Append(row.NewValuesJson ?? "").Append('|')
            .Append(row.IpAddress ?? "").Append('|')
            // Force UTC before formatting so DB round-trip (which loses DateTimeKind) can't
            // change the resulting string. SQL Server datetime2 stores UTC values as-is; when
            // EF materializes them Kind becomes Unspecified, which changes the 'O' suffix
            // (drops the 'Z'). Treating any non-Utc value as UTC here mirrors the writer path.
            .Append(DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture))
            .ToString();

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }
}
