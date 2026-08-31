# 02 — Architecture

## The shape

```
┌──────────────────────────────────────────────────────────────────┐
│  frontend/         React 19 + TypeScript + Vite + Tailwind 4     │
│                    Arabic-first RTL admin dashboard              │
└────────────────────────────┬─────────────────────────────────────┘
                             │ HTTPS · JSON · JWT Bearer
                             │ /api/v1/*
┌────────────────────────────▼─────────────────────────────────────┐
│  IndicatorsManagement.Api          ASP.NET Core Web API          │
│  controllers · middleware · authorization policies               │
│  composition root (Program.cs)                                   │
├──────────────────────────────────────────────────────────────────┤
│  IndicatorsManagement.Application  service interfaces            │
│                                     FluentValidation validators  │
├──────────────────────────────────────────────────────────────────┤
│  IndicatorsManagement.Infrastructure                             │
│  EF Core DbContext · migrations · seeding                        │
│  service implementations · Hangfire jobs · email                 │
├──────────────────────────────────────────────────────────────────┤
│  IndicatorsManagement.Contracts    request/response DTOs         │
│                                     shared constants             │
├──────────────────────────────────────────────────────────────────┤
│  IndicatorsManagement.Domain       entities · enums              │
│                                     no external dependencies     │
└────────────────────────────┬─────────────────────────────────────┘
                             │ EF Core
                ┌────────────▼────────────┐   ┌─────────────────────┐
                │ SQL Server              │   │ SQL Server          │
                │ IndicatorsManagement    │   │ ..._Hangfire        │
                └─────────────────────────┘   └─────────────────────┘
```

## Project reference graph

Verified from the `.csproj` files:

```
Domain          → (nothing)
Contracts       → Domain
Application     → Domain, Contracts
Infrastructure  → Domain, Application
Api             → Application, Infrastructure, Contracts
Tests           → all four src projects
```

There are no cycles, and `Domain` genuinely depends on nothing but
`Microsoft.Extensions.Identity.Stores` (needed because `ApplicationUser` derives from
`IdentityUser<int>`).

## The Dependency Rule

Source-code dependencies point **inward**, toward policy. `Domain` knows nothing about
EF Core, HTTP, or JSON. `Application` declares *what* the system can do
(`IIndicatorEntryService`); the outer layers decide *how*.

The rule is inverted at the one place it must be: `Api` and `Infrastructure` both depend
on `Application`'s interfaces, and `Api`'s composition root
([ServiceCollectionExtensions.cs](../src/IndicatorsManagement.Api/Extensions/ServiceCollectionExtensions.cs))
binds each interface to its Infrastructure implementation. Controllers therefore depend
on abstractions, not on EF Core.

## Where each kind of code belongs

| You are writing… | It belongs in | Because |
|---|---|---|
| An entity or enum | `Domain/Entities`, `Domain/Enums` | It is the business vocabulary |
| A request or response shape | `Contracts/Requests`, `Contracts/Responses` | It is the wire format, shared by API and clients |
| A role name, a config key | `Contracts/Constants` | Both the API and services need it |
| A service interface | `Application/Services/Interfaces` | It is the use-case boundary |
| A FluentValidation validator | `Application/Validators` | Input rules are application policy |
| An EF Core mapping or migration | `Infrastructure/Data` | Persistence detail |
| A Hangfire job | `Infrastructure/Jobs` | Scheduling detail |
| An HTTP endpoint | `Api/Controllers` | Delivery detail |
| A cross-cutting request concern | `Api/Middleware` | Pipeline detail |
| An authorization policy or handler | `Api/Authorization` | ASP.NET Core-specific |

## Known deviation: business logic lives in Infrastructure

**This is the single most significant architectural issue in the codebase.**

All sixteen service implementations — `IndicatorEntryService`, `PublicationService`,
`AuthenticationService`, and the rest — live in
[src/IndicatorsManagement.Infrastructure/Services/](../src/IndicatorsManagement.Infrastructure/Services/),
not in `Application`. Only their interfaces live in `Application`.

That means the approval state machine — the most valuable rule in the system — sits in
the layer whose job is talking to SQL Server. Concretely,
[IndicatorEntryService.cs](../src/IndicatorsManagement.Infrastructure/Services/IndicatorEntryService.cs)
holds both "an entry may only be submitted from Draft or Returned_For_Modification" and
the `Include(...).ThenInclude(...)` query that loads it.

**Consequences**

- Business rules cannot be unit-tested without an EF Core context. The existing tests
  work around this with the in-memory provider — which is why they pass while the
  filtered unique index that actually enforces the core invariant is never exercised.
- The rules are not reusable outside a SQL Server deployment.
- Nothing structurally prevents a new query optimisation from quietly changing a rule.

**Why it has not been fixed**: the move is mechanical but wide — sixteen files, their
`using` directives, the DI registrations, and the test project references. It is real
work with no user-visible benefit, so it needs to be a deliberate, isolated change.

**What to do about it**: finding **A1** in
[13-review-findings.md](13-review-findings.md). The intended end state is that
`Application` owns the services and depends on a persistence abstraction, with
`Infrastructure` providing the EF Core implementation. Until then:

> **Rule for new code**: put new business rules in a service, keep them free of EF Core
> types wherever you can, and never add a *new* reason for `Application` to depend on
> `Infrastructure`.

## Secondary deviation: controllers that touch the DbContext directly

[AttachmentsController.cs](../src/IndicatorsManagement.Api/Controllers/AttachmentsController.cs)
injects `IndicatorsDbContext` and performs queries, file I/O, and workflow checks inline —
skipping the service layer entirely. This is finding **A2**. No other controller does
this; every other one depends only on an `Application` interface.

## The request pipeline

Order matters, and this order is deliberate. From
[Program.cs](../src/IndicatorsManagement.Api/Program.cs):

```
CorrelationIdMiddleware      assign/propagate X-Correlation-Id, push to Serilog context
GlobalExceptionMiddleware    map exceptions → ApiResponse with Arabic messages
UseSerilogRequestLogging     structured request log
UseResponseCompression       Brotli, then Gzip
UseSwagger / UseSwaggerUI    Development only
UseHttpsRedirection
UseCors("AllowFrontend")     origins from Cors:AllowedOrigins
UseRateLimiter               200/min per IP globally; 5/min on auth
UseAuthentication            JWT bearer, zero clock skew
UseAuthorization             role and policy checks
SessionValidationMiddleware  the JWT must match a live UserSession row
AuditLoggingMiddleware       record every POST/PUT/PATCH/DELETE
UseHangfireDashboard         /hangfire
MapControllers
MapHealthChecks("/health")
```

`CorrelationIdMiddleware` is first so that every later log line — including the exception
handler's — carries the correlation id. `GlobalExceptionMiddleware` is second so it
catches everything downstream of it.

**Note the two-factor session model**: a valid, unexpired JWT is *not* sufficient. The
token must also match a row in `user_sessions` that has not expired and has not been idle
past `SessionTimeout_Minutes`. This makes server-side logout and forced expiry possible,
at the cost of a database round-trip per request — see finding **P1**.

## Cross-cutting concerns

| Concern | Mechanism | Where |
|---|---|---|
| Authentication | JWT bearer, 30 min, zero clock skew | `Program.cs`, `AuthenticationService` |
| Session revocation | `user_sessions` table + middleware | `SessionValidationMiddleware` |
| Authorization | Role attributes + 9 named policies | `Api/Authorization/` |
| Validation | FluentValidation, assembly-scanned | `Application/Validators/` |
| Mapping | Mapster (declared) + hand-written mappers (actual) | see finding **C3** |
| Logging | Serilog → console, rolling file, optional SQL table | `Program.cs` |
| Auditing | Middleware for all writes + explicit service calls | `AuditLoggingMiddleware`, `IAuditLogService` |
| Background work | Hangfire, SQL Server storage, 3 retries | `Infrastructure/Jobs/` |
| Errors | `ApiResponse` / `ApiResponse<T>` envelope | `Contracts/Responses/ApiResponse.cs` |

## Startup sequence

1. `ValidateRequiredConfiguration()` — fail fast if connection strings or the JWT key are
   missing, weak, or still a template placeholder.
2. Serilog, Kestrel TLS 1.2+, DbContext, Identity, JWT, policies, CORS, validators.
3. `SqlServerDatabaseBootstrapper.EnsureDatabaseExists(HangfireConnection)` — create the
   Hangfire database if absent. Hangfire creates its *schema* but never its *database*;
   without this the API cannot start against a fresh server.
4. Hangfire storage + server, Swagger, services, rate limiting, compression, health.
5. `app.Build()`, then the middleware pipeline above.
6. `DatabaseSeeder.SeedAsync` — apply EF migrations, then seed roles, config, entities,
   indicators, assignments, periods, and the admin user. Every step is idempotent.
7. Register the four recurring Hangfire jobs.
8. `app.Run()`.

Steps 1 and 3 were added during the review; see
[13-review-findings.md](13-review-findings.md) findings **S1** and **B1**.

## Design decisions worth knowing

Recorded as ADRs in [adr/](adr/):

- [0002](adr/0002-two-level-approval-workflow.md) — why approval is two levels, and why
  `Rejected` is terminal.
- [0003](adr/0003-publication-separate-from-approval.md) — why publication is a separate
  axis from workflow state.
- [0004](adr/0004-enums-as-strings-in-database.md) — why enums persist as strings.
- [0005](adr/0005-server-side-session-validation.md) — why a stateless JWT is backed by a
  stateful session table.
- [0006](adr/0006-no-secrets-in-committed-configuration.md) — the secrets policy.
