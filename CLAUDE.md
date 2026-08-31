# CLAUDE.md

Guidance for Claude Code (claude.ai/code) working in this repository.

> **Full documentation is in [Docs/](Docs/).** This file is the quick briefing; `Docs/` is
> the reference. If they disagree, `Docs/` wins — and fix this file.
>
> **Before any non-trivial task, read [Docs/14-ai-agent-guide.md](Docs/14-ai-agent-guide.md).**

## Project

**IndicatorsManagement** (نظام إدارة المؤشرات) — a bilingual (Arabic-first, English
optional) indicators management system for the Libyan Ministry of Economy and Trade.
Tracks 120 economic/statistical indicators across 15 organizational entities through a
two-level approval workflow, with dimensional breakdowns, publication control, reporting
obligations, email notifications, and a full audit trail.

## Hard rules

1. **Never commit a secret.** No connection string, password, API key, or production
   hostname in a tracked file. `appsettings.json` holds empty placeholders and stays that
   way. This repository leaked live credentials once — see finding **S0** in
   [Docs/13-review-findings.md](Docs/13-review-findings.md).
2. **The database is local.** `localhost:1433` via `docker-compose.dev.yml`. Never point
   development at a remote server.
3. **Never edit an applied EF Core migration.** Add a new one.
4. **Never weaken authorization** to make something work.
5. **Update `Docs/`** whenever you change the domain model, the workflow, the role matrix,
   or the API surface.

## Spec-driven development with OpenSpec

Non-trivial work is proposed before it is written:

```bash
/opsx:propose "what you want to build"   # → openspec/changes/<name>/{proposal,design,tasks}.md
/opsx:apply <name>                       # work the tasks
/opsx:archive <name>                     # fold specs into openspec/specs/

openspec list                            # active changes
openspec status --change "<name>"
```

Project context and per-artifact rules live in `openspec/config.yaml`. Propose for
anything schema-, workflow-, role-, or API-affecting, or spanning more than about three
files. Just do typo fixes and local tidying.

## Architecture

**Backend** — .NET 10, Clean Architecture, five projects:

| Project | Contains |
|---|---|
| `Domain` | Entities and enums. No external dependencies |
| `Contracts` | Request/response DTOs, `Roles`, `ConfigKeys` |
| `Application` | Service **interfaces**, FluentValidation validators (scanned via `Placeholder.cs`) |
| `Infrastructure` | EF Core `IndicatorsDbContext`, migrations, `DatabaseSeeder`/`SeedData`, service **implementations**, Hangfire jobs |
| `Api` | Controllers, middleware, authorization, `Program.cs` composition root |

**Known deviation:** service implementations live in `Infrastructure`, not `Application` —
finding **A1** / [ADR-0007](Docs/adr/0007-services-in-infrastructure.md). Do not deepen it.

**Frontend** — React 19 + TypeScript + Vite + Tailwind CSS 4 (TailAdmin base), in
`frontend/`. Redux Toolkit for auth, React Router 7, Axios with JWT interceptors.

## Commands

```bash
# database (start this first)
cp .env.example .env
docker compose -f docker-compose.dev.yml up -d

# backend
cp src/IndicatorsManagement.Api/appsettings.Development.json.example \
   src/IndicatorsManagement.Api/appsettings.Development.json
dotnet build IndicatorsManagement.slnx
dotnet test  tests/IndicatorsManagement.Tests/     # 76 tests
dotnet run   --project src/IndicatorsManagement.Api

# frontend
cd frontend && npm install && npm run dev          # build | lint | preview

# full stack
docker compose up --build                          # :80 · :8080 · /health
```

Verified baseline: build clean (0 warnings), **76/76 tests pass**, frontend builds, no
vulnerable packages. See [Docs/09-development-setup.md](Docs/09-development-setup.md).

## Key patterns

**Backend**
- Solution is `.slnx` (XML), not `.sln`
- Two databases: `IndicatorsManagement` (EF migrations) and `IndicatorsManagement_Hangfire`
  (created by `SqlServerDatabaseBootstrapper` — Hangfire will not create its own database)
- Tables snake_case (`indicator_entries`); enums stored as **strings**
  ([ADR-0004](Docs/adr/0004-enums-as-strings-in-database.md)); decimals `(18,4)`
- FK delete behaviour `NoAction` by default; `Cascade` only for true compositions
- Soft delete via `IsDeleted` on `IndicatorEntry` and `Attachment`; **no global query
  filter**, so filter explicitly. Filtered unique index excludes soft-deleted and
  `Rejected` rows
- Bilingual `*Ar` (required) / `*En` (optional) pairs
- Services return `ApiResponse<T>`; expected failures return `Fail`, they do not throw;
  messages are Arabic
- Routes `api/v1/<kebab-plural>`; `[Authorize]` at class level, `Roles.*` constants never
  literals; **`EntityId` comes from the JWT claim, never the request body**
- Workflow: `Draft → Under_Review → Approved_By_Entity → Final_Approved`, with
  `Rejected` (terminal) and `Returned_For_Modification` branches.
  `PublicationStatus` is a separate axis ([ADR-0003](Docs/adr/0003-publication-separate-from-approval.md))
- JWT 30 min, zero clock skew, **plus** a live `user_sessions` row checked on every
  request ([ADR-0005](Docs/adr/0005-server-side-session-validation.md))
- Pipeline: CorrelationId → GlobalException → Serilog → Compression → CORS → RateLimiter
  → Auth → Session → AuditLog
- Rate limits: 5/min on auth, 200/min global per IP
- Mapster is referenced but **unused** — all mapping is hand-written (finding **C3**)

**Frontend**
- `<html dir="rtl">` — use Tailwind logical properties (`ps-*`, `pe-*`, `ms-*`, `me-*`),
  not `pl-*`/`pr-*`
- API calls go in `src/services/*Service.ts`; components never call axios directly
- Route guards are cosmetic — the API is the only real authorization
- Strict TypeScript: unused locals and parameters fail `npm run build`
- Modals: `useModal()` + `<Modal>` (see `UserManagement.tsx`)

## Seeding (idempotent, runs on every startup)

7 roles · 6 config keys · **15 entities** (Ministry + 14) · **120 indicators** with full
Arabic metadata · **120 assignments** · **57 reporting periods** (2024–2026) · admin user
`admin` / `Admin@123456`.

> The admin password is hardcoded in `DatabaseSeeder`; `ADMIN_PASSWORD` in `.env.example`
> is **not** read (finding **S8**). Change it after first login.

Source: [Docs/reference/indicators-guide-tables.ar.md](Docs/reference/indicators-guide-tables.ar.md)
→ `SeedData.cs`.

## Current state

The system builds, tests green, and runs locally end to end. There is **no CI pipeline**
(finding **O4**) — an earlier version of this file claimed one existed.

**The highest-priority open issue is S5**: by-id endpoints do not verify object ownership,
so any authenticated user can read and modify other entities' data. Read
[Docs/13-review-findings.md](Docs/13-review-findings.md) before planning work.

## Documentation map

| Need | Read |
|---|---|
| What the system is, Arabic glossary | [Docs/01-overview.md](Docs/01-overview.md) |
| Layers, dependency rule, pipeline | [Docs/02-architecture.md](Docs/02-architecture.md) |
| Entities, relationships, invariants | [Docs/03-domain-model.md](Docs/03-domain-model.md) |
| Approval, publication, jobs, sessions | [Docs/04-workflows.md](Docs/04-workflows.md) |
| Every endpoint and its roles | [Docs/05-api-reference.md](Docs/05-api-reference.md) |
| React structure, routing, RTL | [Docs/06-frontend.md](Docs/06-frontend.md) |
| Schema, indexes, migrations, seeding | [Docs/07-database.md](Docs/07-database.md) |
| Auth, role matrix, secrets | [Docs/08-security.md](Docs/08-security.md) |
| Local setup and troubleshooting | [Docs/09-development-setup.md](Docs/09-development-setup.md) |
| Docker, env vars, runbook | [Docs/10-deployment.md](Docs/10-deployment.md) |
| Test strategy and gaps | [Docs/11-testing.md](Docs/11-testing.md) |
| Naming and code patterns | [Docs/12-conventions.md](Docs/12-conventions.md) |
| **Open backlog** | [Docs/13-review-findings.md](Docs/13-review-findings.md) |
| **How to work here as an agent** | [Docs/14-ai-agent-guide.md](Docs/14-ai-agent-guide.md) |
| Why things are this way | [Docs/adr/](Docs/adr/) |
