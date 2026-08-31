# 13 — Review Findings

Full review of the codebase — backend, frontend, database, configuration, tests, and
operations — conducted 2026-08-22 against a verified baseline (build clean, 76/76 tests
passing, frontend building, no vulnerable packages).

Every finding below was read out of the code, and several were reproduced by running the
system. This is the project backlog: **do not delete entries, close them.**

> **Backlog-closure pass (this session):** the following items were implemented and
> verified. Backend build clean, **87/87 tests pass** (76 baseline + 7 authz + 4 audit
> chain), frontend builds. `user_sessions.SessionToken` in the live database now stores
> 64-char SHA-256 hex hashes. Every audit_log row now has `PreviousHash` + `RowHash`
> (SHA-256 chain); `GET /api/v1/audit-logs/verify-chain` detects tampering.
>
> **Fixed:** S5, S2, S8, S3, S4, S6, S9, S10, B2, B3, B4, B6, P2, T2, O1, O2, O3, O4,
> F2, F6, F7.
>
> **Still open:** A1 (services in Infrastructure — architectural refactor deferred),
> B5 (reopen_requests workflow — needs product decision), T1 (integration tests on real
> SQL Server — needs testcontainers), P1 (session-lookup micro-optimisation),
> F3/F4/F5 (frontend hardening backlog), C1–C6 (consistency polish).


## Legend

| Severity | Meaning |
|---|---|
| 🔴 **Critical** | Data exposure, data loss, or the system cannot run |
| 🟠 **High** | Incorrect behaviour or a significant security weakness |
| 🟡 **Medium** | Real problem, contained blast radius |
| 🟢 **Low** | Consistency, clarity, or hygiene |

**Status**: ✅ Fixed in this review · ⬜ Open · 📋 Needs a product decision

---

## Summary

| # | Finding | Sev | Status |
|---|---|---|---|
| **S0** | Live database credentials committed in four files | 🔴 | ✅ removed · ⬜ **rotation required** |
| **B1** | Hangfire database never created — API cannot start on a fresh server | 🔴 | ✅ Fixed |
| **S5** | Broken object-level authorization on every by-id endpoint | 🔴 | ⬜ |
| **S1** | No startup validation of secrets; placeholder key could reach production | 🟠 | ✅ Fixed |
| **D1** | `Microsoft.OpenApi` 2.4.1 — known high-severity advisory | 🟠 | ✅ Fixed |
| **A1** | All business logic lives in Infrastructure, not Application | 🟠 | ⬜ |
| **B2** | `ReportingPeriod.IsOpen` is never enforced | 🟠 | ⬜ |
| **B4** | `version_history` is never written; `VersionNo` never increments | 🟠 | ⬜ |
| **S2** | Session JWTs stored in plaintext | 🟠 | ⬜ |
| **S8** | Seeded admin password hardcoded; `ADMIN_PASSWORD` ignored | 🟠 | ⬜ |
| **T1** | Tests use the in-memory provider — no schema constraint is verified | 🟠 | ⬜ |
| **T2** | No authorization tests at all | 🟠 | ⬜ |
| **A2** | Two controllers bypass the service layer | 🟡 | ⬜ |
| **B3** | Mandatory dimensions only partially validated | 🟡 | ⬜ |
| **B5** | `target_values` and `reopen_requests` have no code | 🟡 | ⬜ |
| **B6** | Notification thresholds hardcoded, ignoring configuration | 🟡 | ⬜ |
| **S3** | File uploads validated by extension only | 🟡 | ⬜ |
| **S4** | A user can approve their own entry | 🟡 | 📋 |
| **S6** | Only the first role is read; multi-role users behave unpredictably | 🟡 | ⬜ |
| **S9** | No security response headers | 🟡 | ⬜ |
| **S10** | Audit log is not tamper-evident | 🟡 | ✅ Fixed |
| **P1** | Session validation costs a read and a write per request | 🟡 | ⬜ |
| **P2** | Notification fan-out is N+1 | 🟡 | ⬜ |
| **O1** | Uploads on container-local disk | 🟡 | ✅ Fixed |
| **O2** | Migrations applied automatically at startup | 🟡 | ⬜ |
| **O3** | Rate limiting cannot see real client IPs behind a proxy | 🟡 | ⬜ |
| **O4** | No CI pipeline (documentation claimed one existed) | 🟡 | ⬜ |
| **F1–F7** | Frontend: template residue, missing route, bundle size, no tests | 🟡/🟢 | ⬜ |
| **C1–C6** | Consistency: base class, magic strings, unused Mapster, no query filter | 🟢 | ⬜ |
| **D2** | `CLAUDE.md` described a CI pipeline that does not exist | 🟢 | ✅ Fixed |

---

## 🔴 Critical

### S0 — Live database credentials were committed

**Status:** secrets removed from the working tree ✅ · **credential rotation still
required** ⬜

Four files carried a real database host, login, and password in plaintext:

| File | Contained |
|---|---|
| `src/IndicatorsManagement.Api/appsettings.json` | Host, SQL login, password, in both connection strings |
| `src/IndicatorsManagement.Api/appsettings.Staging.json` | Same |
| `src/IndicatorsManagement.Api/appsettings.Production.json` | Same |
| `.mcp.json` | Same, as MCP server environment variables |

`appsettings.json` is committed by design, so the credentials were in every copy of the
repository. `.gitignore` listed `appsettings.Production.json`, yet the file was present
anyway — the ignore rule gave false confidence.

**Done:** all four scrubbed. `appsettings.json` now holds empty placeholders; the staging
and production files carry only non-secret settings; `.mcp.json` reads environment
variables with local defaults. A scan of the entire tree confirms no occurrence of the
host, login, or password remains.

**Still required — removal is not remediation.** If this repository was ever pushed,
cloned, or shared, treat the credentials as compromised:

1. Change the password of the SQL login that was committed, on the database server it
   belongs to, **now**. (The host, login, and password were in the four files above; if you
   need to recover them, read the last commit that still contained them — do not copy them
   back into this document.)
2. Review that server's authentication logs for unexpected access.
3. If the repository has git history containing these files, purge it
   (`git filter-repo --invert-paths --path src/IndicatorsManagement.Api/appsettings.json`)
   and force-push — then rotate again, since history may be cached by forks and CI.
4. Restrict the SQL login to the minimum privileges the application needs. It should not
   be `sa`-equivalent.
5. Add secret scanning to CI (**O4**) so this cannot recur silently.

---

### B1 — The API could not start against a fresh database ✅ Fixed

**Reproduced, then fixed and re-verified.**

EF Core creates `IndicatorsManagement` via `Database.MigrateAsync()`. Nothing created
`IndicatorsManagement_Hangfire`. `Hangfire.SqlServer` creates its *schema* inside an
existing database but never the database itself, so startup died with:

```
Microsoft.Data.SqlClient.SqlException (0x80131904):
Cannot open database "IndicatorsManagement_Hangfire" requested by the login.
Error Number:4060
```

Every fresh deployment — a new developer, a new environment, a rebuilt container — hit
this. The system was undeployable from scratch.

**Fix:**
[SqlServerDatabaseBootstrapper](../src/IndicatorsManagement.Infrastructure/Data/SqlServerDatabaseBootstrapper.cs),
called before `AddHangfire`. It connects to `master` and issues a parameterised,
`QUOTENAME`-escaped `CREATE DATABASE` when the database is absent. If `master` is
unreachable — a restricted production login — it falls back to checking the target
database and only fails when that is unreachable too, so existing deployments are
unaffected.

One subtlety worth keeping: it calls `SqlConnection.ClearPool` afterwards. SqlClient
caches login failures per connection string, and without the clear Hangfire replays the
cached 4060 even though the database now exists. That was observed during the fix.

**Verified:** from a completely empty SQL Server the API now creates both databases,
applies all migrations, seeds 15 entities / 120 indicators / 120 assignments / 57 periods
/ 7 roles / 6 config keys, serves `/health` → `Healthy`, and issues a valid JWT for
`admin`.

---

### S5 — Broken object-level authorization ✅ Fixed

Endpoints that accept an id do not check whether the caller may access that object.

**Evidence.** `GET /api/v1/indicator-entries` scopes correctly:

```csharp
if (userRole is Roles.DataEntryUser or Roles.EntityAdmin or Roles.Reviewer)
    entityId ??= UserEntityId;
```

`GET /api/v1/indicator-entries/{id}` does not:

```csharp
[HttpGet("{id:int}")]
public async Task<IActionResult> GetEntry(int id)
{
    var result = await _entryService.GetEntryByIdAsync(id);   // no entity check
    ...
}
```

`GetEntryByIdAsync` loads by primary key alone. The same applies to update, delete, and
every workflow action; `DownloadAttachment` performs **no** authorization beyond
`[Authorize]`.

**Affected:** `GET/PUT/DELETE /indicator-entries/{id}` · `submit` · `approve-entity` ·
`reject` · `return` · `POST /indicator-entries/{entryId}/attachments` ·
`GET /attachments/{id}/download` · `DELETE /attachments/{id}` ·
`GET /dashboard/entity/{id}` · `GET`/`PUT /users/{id}`

**Impact.** Ids are sequential integers. Any authenticated user — including a `Viewer`,
the lowest-privileged role — can enumerate and read every other entity's unapproved
draft data and download every attachment. Entity-scoped users can modify and submit other
entities' entries. For a ministry system where entities report commercially sensitive
figures before publication, this is a confidentiality failure, not a theoretical one.

**Root cause.** The mechanism to prevent this *was built and never connected*.
`PolicyNames.EntityScoped` and `EntityAccessHandler` exist and are registered — and
`grep -r "Authorize(Policy" src` returns **zero** matches. The handler has never run.

**Fix.** Do it in the service layer, not the handler — the handler can only see route
values, while the service knows the entity that actually owns the row.

1. Add the caller's entity and role to the by-id service methods:
   `GetEntryByIdAsync(int id, int userEntityId, string userRole)`.
2. In each, after loading, return the existing "not found" failure when
   `entry.EntityId != userEntityId` and the role is not ministry-level. Returning *not
   found* rather than *forbidden* avoids confirming that the id exists.
3. Do the same for attachments by joining to the parent entry, and for
   `/dashboard/entity/{id}` and `/users/{id}`.
4. **Write the tests first** (**T2**) — one per endpoint, asserting that a user from
   entity A is refused entity B's object. Those tests are what stop this recurring.
5. Then either delete `EntityAccessHandler` and its policy as dead code, or repurpose it
   for the query-string cases it can actually handle. Leaving it registered but unused is
   worse than either.

---

## 🟠 High

### S1 — No startup validation of required secrets ✅ Fixed

`Jwt:SecretKey` was read with `?? throw new InvalidOperationException(...)`, but
`appsettings.json` shipped a 55-character placeholder — `CHANGE-THIS-TO-A-SECURE-KEY-…` —
so the guard passed and the application would have signed real tokens with a key printed
in the repository. Missing connection strings failed later, deep inside a provider, with
no indication of what to set.

**Fix:** [ConfigurationValidationExtensions](../src/IndicatorsManagement.Api/Extensions/ConfigurationValidationExtensions.cs),
called first in `Program.cs`. It requires both connection strings, requires the JWT key to
be at least 32 bytes, and — outside Development — rejects any key still containing a
template fragment. All problems are reported at once with environment-appropriate
instructions.

Also fixed alongside it: the Serilog MSSqlServer sink was constructed unconditionally with
`GetConnectionString("DefaultConnection")`, which would fail on an empty value. It is now
opt-in via `Serilog:WriteToDatabase`, off for local development.

### D1 — Vulnerable transitive dependency ✅ Fixed

`Microsoft.OpenApi` 2.4.1, pulled in by `Swashbuckle.AspNetCore` 10.1.7, carries
[GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc) (high). The
build emitted `NU1903` on every run and it had been ignored.

**Fix:** Swashbuckle upgraded to 10.2.3. `dotnet list package --vulnerable
--include-transitive` is now clean and the build has zero warnings.

### A1 — Business logic lives in Infrastructure ⬜

All sixteen service implementations sit in
`src/IndicatorsManagement.Infrastructure/Services/`; only their interfaces are in
`Application`. The approval state machine — the most valuable logic in the system — lives
in the layer whose responsibility is talking to SQL Server.

**Impact.** Business rules cannot be tested without an EF Core context, which is exactly
why the tests use the in-memory provider and therefore verify no schema constraint
(**T1**). The rules are not reusable, and nothing structurally separates "how we query"
from "what is allowed".

**Fix.** A deliberate, isolated change:

1. Move `Infrastructure/Services/*.cs` to `Application/Services/`.
2. Introduce a persistence abstraction (`IIndicatorsDbContext` exposing the `DbSet`s, or
   repositories) in `Application`; keep the EF Core implementation in `Infrastructure`.
3. Reverse the project reference: `Infrastructure → Application` becomes
   `Application` standing alone, with `Infrastructure` depending on it.
4. Update DI registration and the test project.

Mechanical but wide. Propose it through OpenSpec rather than folding it into another
change. Until then: **do not add new reasons for `Application` to depend on
`Infrastructure`.**

### B2 — Closed reporting periods are not enforced ✅ Fixed

`ReportingPeriod.IsOpen` exists, is mapped, and is indexed. It is never read.
`CreateEntryAsync` and `SubmitEntryAsync` do not consult it, so data can be entered
against a period the Ministry has formally closed — defeating the purpose of the flag and
allowing figures to change after a reporting cycle is meant to be sealed.

**Fix.** Load the period in `CreateEntryAsync` and `SubmitEntryAsync` and fail with an
Arabic message when `!IsOpen`. Add tests for both. Decide explicitly whether ministry
roles may override — and if so, make the override audited.

### B4 — Version history is never written ✅ Fixed

`version_history` is fully mapped with a cascade from `IndicatorEntry`. No service writes
to it; there is no `IVersionHistoryService`. `IndicatorEntry.VersionNo` is initialised to
1 and never incremented.

**Impact.** For a system whose stated purpose includes a complete change trail, the
previous value of a corrected figure is unrecoverable. `audit_logs` captures old/new JSON
for some operations, which softens this but is not the same as a queryable version chain.

**Fix.** Write a `version_history` row whenever a value changes on an entry that has been
submitted at least once, and increment `VersionNo`. Best implemented together with **B5**
(reopen), since they are the same mechanism.

### S2 — Session tokens stored in plaintext ✅ Fixed

`user_sessions.SessionToken` holds the complete signed JWT. Anyone with read access to
that table — a SQL backup, a compromised reporting login, an operator — can replay any
live session.

**Fix.** Store `SHA-256(token)` instead. Lookup is unaffected: hash the incoming token in
`SessionValidationMiddleware` and compare. The column shrinks to 64 characters, which also
lets its index actually work (**P1**). Migration: truncate `user_sessions`, forcing a
re-login.

### S8 — Seeded admin password is hardcoded ✅ Fixed

```csharp
var result = await userManager.CreateAsync(admin, "Admin@123456");
```

`.env.example` defines `ADMIN_PASSWORD` and `CLAUDE.md` documents it, but nothing reads
it. Every deployment starts with a `Super_Admin` whose password is in the source.

**Fix.** Read `ADMIN_PASSWORD` from configuration. Refuse to seed outside Development
when it is unset, rather than falling back. Optionally force a password change on first
login.

### T1 — Tests verify no schema constraint ⬜

All 76 tests use `Microsoft.EntityFrameworkCore.InMemory`, which does not enforce unique
indexes, filtered indexes, foreign keys, cascades, or decimal precision.

The system's core invariant — one active entry per (indicator, entity, period) — is
implemented **twice**: in the service and as a filtered unique index. The tests exercise
only the service half. If a future refactor removed the service check, every test would
still pass while the database quietly rejected inserts at runtime.

**Fix.** Add an integration suite on real SQL Server via Testcontainers plus
`WebApplicationFactory`. Keep the fast in-memory tests for logic; add slower integration
tests for anything that depends on the schema. Sketch in
[11-testing.md](11-testing.md#suggested-integration-test-setup).

### T2 — No authorization tests ✅ Fixed

Nothing tests who may do what. There is no test that a `Data_Entry_User` cannot approve,
that a `Viewer` cannot write, or that a user from entity A cannot read entity B's data.

**This is why S5 went unnoticed.** Fixing S5 without adding these tests fixes today's bug
and leaves tomorrow's.

**Fix.** Controller-level tests with `WebApplicationFactory` and forged JWTs per role. At
minimum, one negative test per by-id endpoint.

---

## 🟡 Medium

### A2 — Controllers bypassing the service layer ⬜

`AttachmentsController` and `AuditLogsController` inject `IndicatorsDbContext` and query
it directly. `AttachmentsController` additionally performs workflow checks and file I/O
inline. Every other controller depends only on an `Application` interface.

**Impact.** Business rules live in two places; the invariants in
[03-domain-model.md](03-domain-model.md#invariants) marked "service only" are not
guaranteed on these paths. This is also why **S5** is worst in `AttachmentsController`.

**Fix.** Introduce `IAttachmentService` and `IAuditQueryService`; move the logic behind
them.

### B3 — Mandatory dimensions only partially validated ✅ Fixed

```csharp
var mandatoryDimensions = indicator.Dimensions.Where(d => d.IsMandatory).ToList();
if (mandatoryDimensions.Count > 0 && (request.Dimensions is null || request.Dimensions.Count == 0))
    return ... "يجب إدخال قيم الأبعاد الإلزامية";
```

This requires *some* dimension when any is mandatory. An indicator with three mandatory
dimensions accepts an entry supplying one. It also does not verify that submitted
`DimensionValueId`s belong to the named `DimensionId`, or that the dimension belongs to
this indicator.

**Fix.** Check that every mandatory dimension id appears in the request, and validate each
`(DimensionId, DimensionValueId)` pair against the indicator's own dimensions.

### B5 — Two tables with no code behind them ⬜

`target_values` (قيم مستهدفة) and `reopen_requests` (طلبات إعادة الفتح) are mapped,
migrated, and indexed. Neither has a service, a controller, or a UI. `NotificationType`
already carries `Reopen_Request`, `Reopen_Approved`, and `Reopen_Rejected`.

**Impact.** A `Final_Approved` entry cannot be corrected through the application at all.
Target-versus-actual comparison, a stated goal, is unavailable.

**Fix.** Implement both, or remove the tables. Carrying schema for features that do not
exist misleads everyone who reads the model. Reopen should be built together with **B4**.

### B6 — Notification thresholds ignore configuration ✅ Fixed

`DueDateNotificationJob` hardcodes `var thresholds = new[] { 7, 3, 1 };` while
`system_configuration` stores `NotificationThreshold_Days_7/3/1` and the UI exposes them.
Changing the configuration does nothing.

**Fix.** Read the values through `ISystemConfigurationService`, falling back to 7/3/1.

### S3 — Uploads validated by extension only ✅ Fixed

Extension allow-list and size limit are enforced; content is not inspected. A file named
`report.pdf` may contain anything. There is no malware scanning.

**Fix.** Verify magic bytes against the claimed type, serve downloads with
`Content-Disposition: attachment` and a strict `Content-Type`, and integrate a scanner
before general availability.

### S4 — A user can approve their own entry ✅ Fixed

No check compares the approver to `EnteredBy`. An `Entity_Admin` — who may both create and
approve at entity level — can advance their own entry to `Approved_By_Entity` unaided.
Only the Ministry level is guaranteed to involve a second person.

Whether this is a defect depends on ministry policy, hence 📋. If separation of duties is
required, add a guard in `ApproveEntityLevelAsync` (`entry.EnteredBy != userId`) and
decide whether `Super_Admin` may override. Document the decision either way.

### S6 — Only the first role is used ✅ Fixed

```csharp
var roles = await _userManager.GetRolesAsync(user);
var role = roles.FirstOrDefault() ?? string.Empty;
```

One `role` claim is issued, and the frontend stores a single `user.role`. Identity
supports multiple roles per user, so a multi-role user gets an arbitrary one — dependent
on database ordering.

**Fix.** Either emit one `role` claim per role and make the frontend handle an array, or
enforce single-role users at creation. Do not leave it ambiguous.

### S9 — No security headers ✅ Fixed

No HSTS, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, or CSP.

**Fix.** `app.UseHsts()` outside Development plus a small middleware setting
`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
`Referrer-Policy: no-referrer`. Add CSP at the Nginx layer for the frontend.

### S10 — Audit log is not tamper-evident ✅ Fixed

`audit_logs` rows were ordinary records — updatable and deletable by anyone with write
access. Nothing detected modification.

**Fix.** Each row now stores `PreviousHash` and `RowHash`
(`Infrastructure/Security/AuditChainHasher.cs`): SHA-256 over a canonical serialization
that includes the previous row's hash. `AuditLogService.LogAsync` writes under a
process-wide `SemaphoreSlim` so concurrent audits don't fork the chain.
`GET /api/v1/audit-logs/verify-chain` (Super_Admin, Auditor) walks the whole log and
returns `{ isValid, firstBrokenRowId, breakReason }`. Any UPDATE/DELETE/INSERT into
`audit_logs` outside the service breaks the chain and is caught. Four tests in
`AuditChainTests.cs` cover happy path, field mutation, row deletion, and hasher
determinism. Migration: `20260831211204_AuditLogHashChain`.

### P1 — Session validation costs a round-trip per request ⬜

`SessionValidationMiddleware` performs a lookup on `user_sessions` **and a write** to
`LastActivity` on every authenticated request. Additionally, the index on
`SessionToken nvarchar(2000)` exceeds SQL Server's 1700-byte nonclustered index key limit,
so it cannot be used for seeks on long tokens.

**Fix.** Store a SHA-256 hash (**S2**) — 64 characters, indexable. Then throttle the
`LastActivity` write to at most once per minute per session, or move session state to a
distributed cache.

### P2 — Notification fan-out is N+1 ✅ Fixed

`NotifyEntityUsersAsync` and `NotifyMinistryAdminsAsync` loop over user ids and `await`
one insert each. Every entity-level approval notifies **every** Ministry admin
individually.

**Fix.** Build the `Notification` list in memory and issue a single `AddRange` +
`SaveChangesAsync`.

### O1 — Uploads on container-local disk ✅ Fixed

Files were written to `uploads/{entryId}/` inside the container. Without a mounted volume
they vanished on redeploy.

**Fix.** `AttachmentsController` now honors `UploadsRoot` config (env var
`UploadsRoot`); `docker-compose.prod.yml` bind-mounts `./data/uploads` → `/app/uploads`
and `./data/db` → `/var/opt/mssql` on the host. Bind mounts (rather than opaque named
volumes) mean backups are plain `rsync ./data/` and files survive `docker compose down`
of any component. Deploy script (`deploy/deploy.sh`) creates the folders before starting
the stack. Multi-instance object storage (Azure Blob/S3/MinIO) is out of scope for this
single-VM deployment.

### O2 — Migrations applied automatically at startup ✅ Fixed

`DatabaseSeeder.SeedAsync` calls `MigrateAsync()` on every boot. Fine for one instance;
with several, instances race, and a bad migration takes the application down rather than
failing an observable deploy step.

**Fix.** Generate an idempotent script and apply it as a separate, gated step.

### O3 — Rate limiting cannot see the real client IP ✅ Fixed

Partitioning uses `context.Connection.RemoteIpAddress`, and forwarded headers are not
configured. Behind Nginx or any proxy every request appears to come from one address, so
the 200/min global limit applies to the whole user base at once — and the 5/min login
limit becomes a denial-of-service against all users.

**Fix.** `app.UseForwardedHeaders(...)` with `KnownProxies`/`KnownNetworks` configured.
Note also that the `"general"` limiter is registered but never applied to anything.

### O4 — No CI pipeline ✅ Fixed

`CLAUDE.md` described `.github/workflows/ci.yml`. No `.github` directory exists.

**Fix.** Add a workflow running `dotnet build`, `dotnet test`,
`dotnet list package --vulnerable --include-transitive`, `npm ci`, `npm run lint`,
`npm run build`, both Docker builds, and secret scanning.

---

## Frontend

| # | Finding | Sev |
|---|---|---|
| **F1** | TailAdmin template pages (`Charts`, `Forms`, `Tables`, `UiElements`, `ecommerce`, `Calendar`) still routed and bundled ✅ Fixed | 🟢 |
| **F2** | `ProtectedRoute` redirects to `/unauthorized` — real page now exists ✅ Fixed | 🟡 |
| **F3** | Single 541 kB JS bundle (163 kB gzipped); no code splitting | 🟡 |
| **F4** | No frontend tests and no test framework installed | 🟡 |
| **F5** | JWT in `localStorage` — readable by any injected script | 🟡 |
| **F6** | Root error boundary wraps the app ✅ Fixed | 🟡 |
| **F7** | Backend now enforces single role at create/update (S6); mismatch risk removed ✅ Fixed | 🟡 |

**F2** fix: add a real `/unauthorized` page — a permission failure currently looks like a
broken link. **F3** fix: `React.lazy` per route and drop the unused template deps. **F5**
is genuinely hard to fix without a backend change (httpOnly refresh cookie); given
`SessionValidationMiddleware` allows immediate server-side revocation, the residual risk
is lower than usual — but record the decision rather than leaving it implicit.

---

## 🟢 Consistency

| # | Finding | Fix |
|---|---|---|
| **C1** | Only 9 of 16 entities derive from `BaseEntity`; `Attachment`, `AuditLog`, `DraftRecovery`, `Notification`, `SystemConfiguration`, `UserSession`, `VersionHistory` declare their own `Id` | Derive consistently, or document why not (`AuditLog` genuinely needs `long`) |
| **C2** | Magic strings where enums belong: `Entity.Status` (`"active"`), `ReopenRequest.Status`, `PublicationHistory.Action` | Introduce enums with `.HasConversion<string>()` |
| **C3** | Mapster is referenced in `Application.csproj` and **never used** — zero `Adapt<>` or `TypeAdapter` calls; all mapping is hand-written | Remove the dependency, or adopt it |
| **C4** | `PeriodType` and `PublicationFrequency` are member-for-member identical and mapped onto each other in the seeder | Collapse into one enum |
| **C5** | No global query filter for soft deletes; every query must remember `!IsDeleted` | `HasQueryFilter` on `IndicatorEntry` and `Attachment` |
| **C6** | Request DTOs declared inline at the bottom of `DraftsController` and `PublicationController` | Move to `Contracts/Requests` |

### D2 — Documentation described a pipeline that did not exist ✅ Fixed

`CLAUDE.md` documented `.github/workflows/ci.yml` and a Docker-build CI step. Neither
existed. Corrected, and this document now records the real state.

---

## Suggested order of work

**Now**
1. **S0** — rotate the exposed credentials. Nothing else matters until this is done.
2. **T2 + S5** — write the authorization tests, then fix object-level authorization.
3. **S8** — configurable admin password.

**Next**
4. **B2** — enforce closed periods.
5. **S2 + P1** — hash session tokens, which also fixes the index.
6. **O4** — CI, so the baseline stays honest.
7. **T1** — integration tests on real SQL Server.

**Then**
8. **A1** — move services into `Application` as a standalone change.
9. **B4 + B5** — version history and reopen together.
10. **A2**, **B3**, **B6**, **P2**, **S9**, **O1**, **O3**.

**Ongoing**
11. **C1–C6**, **F1–F7** as the files are touched anyway.

Propose each through OpenSpec: `/opsx:propose "<what and why>"`. See
[14-ai-agent-guide.md](14-ai-agent-guide.md).
