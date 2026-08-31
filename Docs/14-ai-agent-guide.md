# 14 — Guide for AI Agents

Read this before touching anything. It exists so an agent with no prior context can pick
up work here and be productive without breaking things.

## Orientation in five minutes

1. **[01-overview.md](01-overview.md)** — what the system is and what the Arabic terms
   mean. Skipping this leads to renaming domain concepts you did not understand.
2. **[02-architecture.md](02-architecture.md)** — the five projects, the dependency rule,
   and the one known deviation.
3. **[13-review-findings.md](13-review-findings.md)** — the open backlog. Your task is
   probably in it.
4. **[12-conventions.md](12-conventions.md)** — how to write code that matches the
   codebase.
5. Whichever of 03–11 covers your area.

`openspec/config.yaml` carries a compressed version of the same briefing and is injected
automatically when you create OpenSpec artifacts.

## Ground rules

### Never

- **Never commit a secret.** No connection string, password, API key, or production
  hostname in any tracked file. `appsettings.json` holds empty placeholders and stays that
  way. This repository already leaked live credentials once — finding **S0**.
- **Never point the database anywhere but local.** Development runs against
  `localhost:1433` from `docker-compose.dev.yml`. If a task seems to need a remote
  database, stop and ask.
- **Never edit an applied EF Core migration.** Add a new one.
- **Never weaken authorization** to make something work. If an endpoint returns 403,
  that is probably correct.
- **Never delete a finding** from [13-review-findings.md](13-review-findings.md). Mark it
  ✅ Fixed with a note.
- **Never trust these docs over the code.** They describe intent. The code is what runs.
  Verify before relying on a claim, and fix the document when it is wrong.

### Always

- **Always run `dotnet build` and `dotnet test` before saying you are done.** The baseline
  is zero warnings and 76 passing tests. Report honestly if you change that number.
- **Always add a test with a behaviour change.** For a bug fix, write the test that fails
  first.
- **Always update `Docs/`** when you change the domain model, the workflow, the role
  matrix, or the API surface. This is the last task of every change.
- **Always take `EntityId` from the JWT claim**, never from a request body.
- **Always use `Roles.*` constants**, never role name literals.
- **Always use Arabic** for user-facing messages.
- **Always use `DateTime.UtcNow`.**

## The OpenSpec workflow

This project uses [OpenSpec](https://github.com/Fission-AI/OpenSpec) for spec-driven
development. Non-trivial work is **proposed, then applied**, so the intent is written down
before the code and reviewable independently of it.

```
/opsx:propose "enforce closed reporting periods on entry creation"
    → openspec/changes/<name>/
        proposal.md   what and why
        design.md     how
        tasks.md      ordered implementation steps

/opsx:apply <name>       work through tasks.md
/opsx:archive <name>     when done — folds specs into openspec/specs/
```

Useful commands:

```bash
openspec list                       # active changes
openspec list --specs               # current specs
openspec status --change "<name>"   # artifact completion
openspec validate "<name>"          # check a proposal
openspec show "<name>"              # read one
```

**When to propose** rather than just editing:

| Situation | Propose? |
|---|---|
| Fixing a typo, renaming a local, tidying a comment | No — just do it |
| A finding marked 🟢 Low, touched while you are already in the file | No |
| Any finding marked 🟡 Medium or above | **Yes** |
| Anything changing the schema, the workflow, the role matrix, or the API surface | **Yes** |
| A change spanning more than about three files | **Yes** |
| Anything a reviewer would want to argue about before it is written | **Yes** |

Rules the project adds to OpenSpec artifacts (encoded in `openspec/config.yaml`):
proposals state the user-visible outcome first and include *Non-goals*; designs name the
migration and say whether existing seed data survives; tasks are at most two hours each,
ordered so build and tests stay green, and end with "update `Docs/`".

## Verifying your work

```bash
# backend — must be zero warnings, 76+ passing
dotnet build IndicatorsManagement.slnx
dotnet test  tests/IndicatorsManagement.Tests/

# frontend — type errors fail the build, not just the lint
cd frontend && npm run lint && npm run build

# dependencies
dotnet list package --vulnerable --include-transitive

# end to end, if you changed startup, schema, or seeding
docker compose -f docker-compose.dev.yml down -v
docker compose -f docker-compose.dev.yml up -d
dotnet run --project src/IndicatorsManagement.Api
curl http://localhost:5117/health
```

The full verified baseline is in
[09-development-setup.md](09-development-setup.md#verified-baseline). If your numbers
differ, find out why before proceeding.

## Where things are

| Looking for | Go to |
|---|---|
| The approval state machine | `Infrastructure/Services/IndicatorEntryService.cs` |
| Publication | `Infrastructure/Services/PublicationService.cs` |
| Login, JWT, sessions | `Infrastructure/Services/AuthenticationService.cs` |
| Schema mapping, indexes | `Infrastructure/Data/IndicatorsDbContext.cs` |
| Seed data (120 indicators) | `Infrastructure/Data/SeedData.cs` |
| Startup wiring | `Api/Program.cs` |
| DI registrations | `Api/Extensions/ServiceCollectionExtensions.cs` |
| Role constants | `Contracts/Constants/Roles.cs` |
| The response envelope | `Contracts/Responses/ApiResponse.cs` |
| Frontend routes | `frontend/src/App.tsx` |
| Frontend API client | `frontend/src/services/api.ts` |

## Traps

Things that have already caught someone.

**The in-memory test provider enforces nothing.** No unique indexes, no foreign keys, no
filtered indexes, no decimal precision. A passing test does not mean SQL Server will
accept the write. Finding **T1**.

**Two databases, not one.** `IndicatorsManagement` and `IndicatorsManagement_Hangfire`.
The second is created by `SqlServerDatabaseBootstrapper` because Hangfire will not create
it itself. If you touch startup order, do not move that call after `AddHangfire`.

**A valid JWT is not enough.** `SessionValidationMiddleware` also requires a live
`user_sessions` row. If requests 401 immediately after a successful login, look there.

**Enums are strings in the database.** The filtered unique index literally reads
`WorkflowState != 'Rejected'`. Renaming an enum member is a breaking schema change that
needs a data migration.

**`appsettings.Development.json` is git-ignored.** If the app will not start on a fresh
clone, that file is missing — copy the `.example`.

**Only nine authorization policies are registered, and none are used.** Do not assume
`EntityScoped` is protecting anything. It is dead code. Finding **S5**.

**Arabic is the primary language.** `NameAr` is required; `NameEn` is optional and mostly
unused by the UI. Do not "fix" a missing English string.

**RTL is global.** `<html dir="rtl">`. Use Tailwind logical properties (`ps-*`, `pe-*`,
`ms-*`, `me-*`) — `pl-*`/`pr-*` will look wrong.

**`Mapster` is referenced but never used.** Do not assume mapping is automatic; every
service maps by hand.

## Reporting

When you finish, say plainly:

- what changed, and where;
- what you verified, with the actual command output;
- what you did **not** do and why;
- any new finding you noticed, added to
  [13-review-findings.md](13-review-findings.md).

If tests fail, say so and show the output. A partially working change reported accurately
is far more useful than a green claim that does not hold.
