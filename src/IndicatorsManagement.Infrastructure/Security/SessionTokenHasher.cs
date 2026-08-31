using System.Security.Cryptography;
using System.Text;

namespace IndicatorsManagement.Infrastructure.Security;

/// <summary>
/// Hashes session tokens with SHA-256 for at-rest storage in user_sessions.
/// Hash is deterministic (no salt) so lookups by (userId, hashedToken) work in a single query.
/// This addresses finding S2 — the previous version stored the raw JWT, so read access
/// to user_sessions was equivalent to session takeover.
/// </summary>
public static class SessionTokenHasher
{
    public static string Hash(string token)
    {
        if (string.IsNullOrEmpty(token)) return string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes); // 64 chars — fits within SQL Server index key limits (P1).
    }
}
