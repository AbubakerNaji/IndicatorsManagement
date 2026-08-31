# 07 — Database

SQL Server 2022. Two databases, one EF Core `DbContext`, five migrations, an idempotent
seeder.

## The two databases

| Database | Owner | Created by |
|---|---|---|
| `IndicatorsManagement` | the application | EF Core `Database.MigrateAsync()` at startup |
| `IndicatorsManagement_Hangfire` | Hangfire | `SqlServerDatabaseBootstrapper` at startup, schema by Hangfire |

They are kept apart so that job churn — Hangfire writes constantly — never contends with
business data, and so the job store can be dropped and rebuilt without touching records.

**Hangfire creates its schema but not its database.** Against a fresh server it fails
with SQL error 4060 before the app finishes starting. That is why
[SqlServerDatabaseBootstrapper](../src/IndicatorsManagement.Infrastructure/Data/SqlServerDatabaseBootstrapper.cs)
runs first — it connects to `master` and issues `CREATE DATABASE` if the database is
absent, then clears the connection pool so no cached login failure is replayed. Without
it the API cannot start on a clean machine (finding **B1**, fixed).

## Naming

Domain tables are **snake_case**: `indicators`, `reporting_periods`,
`indicator_entry_dimensions`, `system_configuration`.
ASP.NET Identity tables keep their defaults: `AspNetUsers`, `AspNetRoles`,
`AspNetUserRoles`, and the rest. Columns are PascalCase throughout, matching the CLR
properties.

The mixed convention is inherited from Identity and is not worth fighting; just do not be
surprised by it.

## Tables

**Master data** — `entities`, `indicators`, `dimensions`, `dimension_values`,
`reporting_periods`, `validation_rules`, `system_configuration`

**Assignment** — `indicator_assignments`, `submission_obligations`

**Transactional** — `indicator_entries`, `indicator_entry_dimensions`, `attachments`,
`version_history`

**V2.1** — `publication_history`, `target_values`, `reopen_requests`

**Support** — `notifications`, `user_sessions`, `draft_recovery`, `audit_logs`

**Identity** — `AspNetUsers` (extended with `EntityId`, `FullNameAr`, `Phone`,
`IsActive`, `CreatedAt`, `UpdatedAt`), `AspNetRoles`, `AspNetUserRoles`,
`AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`

**Infrastructure** — `__EFMigrationsHistory`, and `SerilogLogs` when
`Serilog:WriteToDatabase` is enabled.

A verified fresh deployment lands **28 tables** in `IndicatorsManagement` and 11 in the
Hangfire database.

## Type mapping rules

| Rule | Where | Why |
|---|---|---|
| Enums as **strings** via `.HasConversion<string>()` | every enum property | The database stays readable, and reordering enum members cannot silently reinterpret existing rows. [ADR-0004](adr/0004-enums-as-strings-in-database.md) |
| `decimal(18,4)` via `.HasPrecision(18, 4)` | `ValueNumeric`, `MinValue`, `MaxValue`, `TargetValue.Value` | Uniform money/measure precision; avoids the default `decimal(18,2)` truncating rates |
| Explicit `HasMaxLength` on every string | throughout | Prevents accidental `nvarchar(max)` and keeps rows indexable |
| `DateOnly` for calendar dates | `StartDate`, `EndDate`, `DueDate` | A reporting period is a date range, not an instant |
| `DateTime` UTC for events | `CreatedAt`, `SubmittedAt`, … | Always `DateTime.UtcNow` at the call site |

## Delete behaviour

Default is **`NoAction`**. Cascade is used only where the child cannot exist without the
parent:

| Relationship | Behaviour |
|---|---|
| `Dimension` → `DimensionValue` | `Cascade` |
| `Indicator` → `Dimension` | `Cascade` |
| `Indicator` → `ValidationRule` | `Cascade` |
| `IndicatorEntry` → `IndicatorEntryDimension` | `Cascade` |
| `IndicatorEntry` → `Attachment` | `Cascade` |
| `IndicatorEntry` → `VersionHistory` | `Cascade` |
| `IndicatorAssignment` → `SubmissionObligation` | `Cascade` |
| `ApplicationUser` → `Notification`, `UserSession`, `DraftRecovery` | `Cascade` |
| `Indicator.CreatedBy` → user | `SetNull` |
| everything else | `NoAction` |

`NoAction` by default is what makes the multi-path FK graph on `indicator_entries` — five
separate FKs to `AspNetUsers` — legal in SQL Server, which refuses multiple cascade paths.

## Indexes

### The one that matters

```sql
CREATE UNIQUE INDEX IX_indicator_entries_IndicatorId_EntityId_ReportingPeriodId
  ON indicator_entries (IndicatorId, EntityId, ReportingPeriodId)
  WHERE [IsDeleted] = 0 AND [WorkflowState] != 'Rejected';
```

This is the system's core invariant in physical form: **one active entry per indicator,
entity, and period**. The filter is doing real work — excluding soft-deleted rows lets an
entry be deleted and re-entered, and excluding `Rejected` lets an entity try again after
a rejection. It also depends on `WorkflowState` being stored as a string; with an integer
column the filter could not read `'Rejected'`.

### Other unique indexes

| Table | Columns |
|---|---|
| `entities` | `NameAr` |
| `indicators` | `Code` |
| `dimensions` | `IndicatorId, DimensionNameAr` |
| `reporting_periods` | `PeriodType, Year, Month, Quarter, HalfYear` |
| `submission_obligations` | `IndicatorAssignmentId, ReportingPeriodId` |
| `system_configuration` | `ConfigKey` |
| `target_values` | `IndicatorId, Year, EntityId, DimensionValueId` |

### Non-unique

`entities(ParentEntityId, Status, Type)` · `indicators(IsActive, PublicationFrequency)` ·
`reporting_periods(PeriodType, Year)`, `(StartDate, EndDate)`, `(IsOpen)` ·
`indicator_assignments(IndicatorId, EntityId)` ·
`indicator_entries(IndicatorId)`, `(EntityId)`, `(ReportingPeriodId)`,
`(WorkflowState)`, `(EnteredBy)` ·
`audit_logs(UserId)`, `(CreatedAt)`, `(EntityType, EntityId)`, `(ActionType)` ·
`notifications(UserId, IsRead)` · `user_sessions(SessionToken)`, `(ExpiresAt)` ·
`draft_recovery(UserId, IndicatorId, EntityId, ReportingPeriodId)`, `(ExpiresAt)` ·
`publication_history(IndicatorEntryId)`, `(PerformedAt)` ·
`reopen_requests(IndicatorEntryId)`, `(Status)` ·
`AspNetUsers(EntityId)`, `(IsActive)`

`user_sessions(SessionToken)` is non-unique and indexes a column up to 2000 characters —
past SQL Server's 1700-byte index key limit for nonclustered indexes, so it is created but
cannot be used for seeks on long tokens. Combined with a lookup on every request, this is
finding **P1**.

## Migrations

| Migration | Adds |
|---|---|
| `20260330143708_InitialCreate` | Full schema |
| `20260331065132_AddDraftRecoveryNavProperties` | `DraftRecovery` navigation properties |
| `20260331070418_ExpandSessionTokenLength` | `SessionToken` widened to 2000 |
| `20260331122945_V2_1_PublicationTargetReopen` | `publication_history`, `target_values`, `reopen_requests` |
| `20260411202639_ResetSeedData_AddFundNetworkTypes` | `Fund` and `Network` entity types; seed reset |

`DatabaseSeeder.SeedAsync` calls `MigrateAsync()` on every startup, so a deploy applies
pending migrations automatically. Convenient; also means a bad migration takes the app
down on boot rather than failing a separate step (finding **O2**).

### Adding a migration

```bash
dotnet ef migrations add <DescriptiveName> \
  --project src/IndicatorsManagement.Infrastructure \
  --startup-project src/IndicatorsManagement.Api

dotnet ef migrations script --idempotent \
  --project src/IndicatorsManagement.Infrastructure \
  --startup-project src/IndicatorsManagement.Api \
  --output migration.sql          # review before applying to a real environment
```

Name migrations for what they do. Never edit an applied migration — add a new one.

## Seeding

[DatabaseSeeder.cs](../src/IndicatorsManagement.Infrastructure/Data/DatabaseSeeder.cs)
runs on every startup. Each step guards with an existence check (`if (await
context.X.AnyAsync()) return;`), so re-running is safe.

Order matters — entities before indicators before assignments:

| Step | Produces | Verified count |
|---|---|---|
| Roles | The 7 role names | 7 |
| System configuration | Thresholds, timeouts, limits | 6 |
| Entities | Ministry + 14 children | **15** |
| Indicators | From `SeedData.GetEntitiesWithIndicators()` | **120** |
| Assignments | One per indicator, to its owning entity | **120** |
| Reporting periods | 2024–2026 × (12 + 4 + 2 + 1) | **57** |
| Admin user | `admin` / `Admin@123456`, Super_Admin | 1 |

Counts above were read out of a freshly seeded database, not from the source.

[SeedData.cs](../src/IndicatorsManagement.Infrastructure/Data/SeedData.cs) (777 lines)
holds the 120 indicators with full Arabic metadata, transcribed from
[reference/indicators-guide-tables.ar.md](reference/indicators-guide-tables.ar.md). Ten
tests in `SeedDataTests.cs` assert the counts and code uniqueness.

> **The seeded admin password is hardcoded** as `"Admin@123456"` in `SeedAdminUserAsync`.
> `.env.example` defines `ADMIN_PASSWORD`, but nothing reads it. Change the password
> immediately after first login. Finding **S8**.

## Soft deletes

`IsDeleted` exists on `IndicatorEntry` and `Attachment` only.

There is **no global query filter** — every query must remember `.Where(e => !e.IsDeleted)`
itself. The services do this consistently today, but nothing enforces it, and a new query
that forgets will silently return deleted rows. A `HasQueryFilter` on both entities would
make the guarantee structural (finding **C5**).

## Working with the local database

```bash
docker compose -f docker-compose.dev.yml up -d          # start
docker compose -f docker-compose.dev.yml logs -f db     # watch
docker compose -f docker-compose.dev.yml down           # stop, keep data
docker compose -f docker-compose.dev.yml down -v        # stop and wipe

# psql-equivalent
docker exec -it indicators-db-dev /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$DB_SA_PASSWORD" -C -No -d IndicatorsManagement
```

To start completely over: `down -v`, `up -d`, then run the API — migrations and seeding
rebuild everything.
