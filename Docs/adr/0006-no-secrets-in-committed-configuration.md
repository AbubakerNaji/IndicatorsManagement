# ADR-0006 — No secrets in committed configuration

**Status:** Accepted · **Date:** 2026-08-22

## Context

Before the 2026-08-22 review, four committed files carried a live database host, SQL
login, and password in plaintext: `appsettings.json`, `appsettings.Staging.json`,
`appsettings.Production.json`, and `.mcp.json`. The JWT signing key in `appsettings.json`
was a placeholder — `CHANGE-THIS-TO-A-SECURE-KEY-AT-LEAST-32-CHARS-LONG!!` — long enough
to satisfy the only check in the code, so an unmodified deployment would have signed real
tokens with a key printed in the repository.

Two things made this worse than a single oversight:

- `.gitignore` listed `appsettings.Production.json`, and the file was present anyway. The
  rule gave false confidence rather than protection.
- Nothing failed. There was no signal at build time, at startup, or at any point that
  configuration was unsafe. The system ran perfectly with a published signing key.

The underlying problem is that `appsettings.json` is *designed* to be committed, so any
secret placed there is distributed to everyone with repository access, forever, including
through forks and CI caches.

## Decision

**No secret, credential, connection string, or production hostname appears in any tracked
file.**

Configuration is layered, and only the first layer is committed:

| Layer | Committed | Holds |
|---|---|---|
| `appsettings.json` | ✅ | Structure and non-secret defaults. Secret keys present but **empty** |
| `appsettings.{Environment}.json` | ❌ git-ignored | Local development values |
| user-secrets | ❌ outside the repo | Per-developer alternative |
| Environment variables | ❌ | How staging and production are configured |

Supporting rules:

1. **Templates are committed, values are not.** `appsettings.Development.json.example`,
   `.env.example`, and `frontend/.env.example` show the shape; the real files are ignored.
2. **The local database is local.** Development points at `localhost:1433` from
   `docker-compose.dev.yml`. No shared or remote database in any default.
3. **Startup refuses to run on unsafe configuration.**
   `ValidateRequiredConfiguration()` is the first statement in `Program.cs` and requires
   both connection strings and a JWT key of at least 32 bytes. Outside Development it
   additionally rejects any key still containing a template fragment
   (`CHANGE-THIS`, `local-development-only`, `replace-me`, `your-secret-key`). It reports
   every problem at once with instructions for the current environment.
4. **Compose fails loudly.** `${DB_SA_PASSWORD:?…}` and `${JWT_SECRET_KEY:?…}` stop the
   stack with a readable message rather than starting with empty secrets.
5. **`.gitignore` covers all three environment files**, and `!appsettings.*.json.example`
   keeps the templates.

## Consequences

**Good.** A leak now requires deliberately bypassing several mechanisms. A misconfigured
deployment fails at startup with an actionable message instead of running insecurely.
Local development works from a clean clone in three documented commands. Each environment
holds only its own secrets.

**Bad.** A clone does not run until `appsettings.Development.json` is created. Mitigated
by the committed template, the setup guide, and an error message that names the exact
command — but it is a real extra step.

**Bad.** Environment-variable configuration is more awkward to inspect than a file, and
the `ConnectionStrings__DefaultConnection` double-underscore convention surprises people
who have not met it.

**Neutral.** `appsettings.Development.json` still contains a password — for a container on
`localhost` that holds nothing but seed data. That is a deliberate trade of purity for a
working first-run experience. It is git-ignored, and user-secrets is documented for anyone
who wants nothing in the working tree at all.

**Important.** This decision prevents *future* exposure. It does not undo the past.
Credentials that were committed must be rotated and, if the history was ever pushed,
purged — see finding **S0** in [../13-review-findings.md](../13-review-findings.md).

## Alternatives considered

**A secrets manager (Azure Key Vault, HashiCorp Vault).** The right answer at scale, and
`AddAzureKeyVault` would slot in as another configuration layer. Rejected for now as
infrastructure this deployment does not have; the environment-variable layer is the
migration path.

**Encrypted secrets in the repository (SOPS, git-crypt).** Keeps everything in one place
and versioned. Rejected: it moves the problem to distributing the decryption key, and a
committed ciphertext is permanent — a future key compromise retroactively exposes all
history.

**`dotnet user-secrets` as the only local mechanism.** Strictly better hygiene, since
nothing sensitive touches the working tree. Rejected as the *sole* path because it is
invisible: a new developer sees no file, gets no hint, and must find the documentation.
It is supported and documented as the preferred option for those who want it.

**Do nothing beyond deleting the secrets.** Rejected outright. Deletion without a
mechanism is how the situation arose — the `.gitignore` entry for
`appsettings.Production.json` was exactly that kind of half-measure.
