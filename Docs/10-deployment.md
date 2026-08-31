# 10 — Deployment

## Topology

```
        :80                    :8080                      :1433
  ┌─────────────┐       ┌────────────────┐         ┌──────────────────┐
  │  frontend   │──────▶│      api       │────────▶│    SQL Server    │
  │ Nginx + SPA │       │ ASP.NET Core   │         │  Indicators…     │
  │             │       │ + Hangfire     │────────▶│  Indicators…_HF  │
  └─────────────┘       └────────────────┘         └──────────────────┘
                          volumes: uploads, logs      volume: mssql data
```

## Docker Compose

```bash
cp .env.example .env      # fill in real values — see below
docker compose up --build
```

| Service | Image | Port |
|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 |
| `api` | built from `src/IndicatorsManagement.Api/Dockerfile` | 8080 |
| `frontend` | built from `frontend/Dockerfile` | 80 |

`api` waits on the database's health check. Named volumes persist SQL data, uploads, and
logs.

`DB_SA_PASSWORD` and `JWT_SECRET_KEY` use Compose's `:?` syntax — the stack refuses to
start with a clear message if either is unset, rather than booting with an empty secret.

## Environment variables

`__` is the nesting separator. Everything the API needs:

| Variable | Required | Notes |
|---|:---:|---|
| `ASPNETCORE_ENVIRONMENT` | ✅ | `Production` / `Staging` |
| `ConnectionStrings__DefaultConnection` | ✅ | Application database |
| `ConnectionStrings__HangfireConnection` | ✅ | Job database |
| `Jwt__SecretKey` | ✅ | 32+ bytes. `openssl rand -base64 48` |
| `Jwt__Issuer`, `Jwt__Audience` | | Default to `IndicatorsManagement[.Client]` |
| `Jwt__ExpirationMinutes` | | Default 30 |
| `Cors__AllowedOrigins__0`, `__1`, … | ✅ | Exact front-end origins. Never `*` |
| `Serilog__WriteToDatabase` | | `true` to log into `SerilogLogs` |
| `Smtp__Host`, `__Port`, `__Username`, `__Password`, `__FromEmail`, `__EnableSsl` | | Email is skipped when `Host` is empty |
| `ASPNETCORE_URLS` | | Set to `http://+:8080` in the Dockerfile |

Startup validation (see [08-security.md](08-security.md)) rejects a missing connection
string, a short key, or a placeholder key outside Development — before any of it reaches
a request.

## Dockerfiles

**API** — multi-stage: `sdk:10.0` restores and publishes Release, `aspnet:10.0` runs it.
Runs as a non-root `appuser`, creates `/app/uploads` and `/app/logs`, exposes 8080.

**Frontend** — `node:22-alpine` runs `npm ci && npm run build`, then `nginx:alpine`
serves `dist/` with `nginx.conf` providing the SPA fallback.

> `VITE_*` variables are compile-time. Changing the API URL requires rebuilding the
> frontend image, not restarting the container.

## Startup behaviour in production

On every boot the API will:

1. validate configuration and exit non-zero if it is incomplete,
2. create the Hangfire database if absent,
3. **apply all pending EF Core migrations**,
4. seed anything missing (idempotent; existing data untouched),
5. register the four recurring jobs,
6. start serving.

Step 3 deserves attention: **schema changes are applied automatically at deploy time**.
Convenient for a single-instance deployment; risky at scale. Two consequences:

- A failing migration takes the application down on boot rather than failing a separate,
  observable step.
- Rolling several instances at once means several processes racing to migrate.

For anything beyond one instance, generate and apply an idempotent script in a
controlled step instead:

```bash
dotnet ef migrations script --idempotent \
  --project src/IndicatorsManagement.Infrastructure \
  --startup-project src/IndicatorsManagement.Api \
  --output migration.sql
```

Finding **O2**.

## Scaling constraints

Read this before adding a second API instance.

| Constraint | Detail |
|---|---|
| **Uploads are on local disk** | `uploads/{entryId}/` is container-local. Two instances see different files; a replaced container without the volume loses them. Move to shared or object storage. Finding **O1** |
| **Hangfire server per instance** | `AddHangfireServer` runs in-process with `ProcessorCount × 2` workers. Hangfire coordinates through the database so jobs will not double-run, but every instance competes. Consider a dedicated worker |
| **Migration race** | See above |
| **Rate limiting is per instance** | The in-memory limiter does not coordinate. N instances mean N × the limit |
| **Client IP behind a proxy** | Forwarded headers are not configured, so rate-limit partitions collapse to the proxy's IP. Add `UseForwardedHeaders`. Finding **O3** |
| **Session lookup per request** | Every authenticated request reads and writes `user_sessions`. Finding **P1** |

## Health and observability

**Health** — `GET /health` returns `200 Healthy` when SQL Server is reachable, `503`
otherwise. It checks only `DefaultConnection`; the Hangfire database is not covered.
Suitable for a liveness/readiness probe.

**Logs** — Serilog writes to console (captured by Docker), a daily rolling file under
`logs/`, and optionally the `SerilogLogs` table when `Serilog:WriteToDatabase` is `true`.
Every line carries `CorrelationId`; clients may supply `X-Correlation-Id` and it is
always echoed.

**Jobs** — the Hangfire dashboard at `/hangfire` shows queued, processing, succeeded, and
failed jobs, gated by `HangfireDashboardAuthFilter`. Confirm that filter's behaviour
before exposing the path publicly.

**Audit** — `audit_logs` is the record of who did what. See
[08-security.md](08-security.md#auditing).

## Backups

Nothing in the repository configures backups. Before go-live:

- Scheduled full + differential backups of `IndicatorsManagement`, retained off-host.
- **A tested restore.** An untested backup is a hypothesis.
- The `uploads` volume — attachments are evidence for approved figures and are not in the
  database.
- `IndicatorsManagement_Hangfire` can be recreated from scratch; job history is not
  business data.

## Release checklist

**Before**
- [ ] `dotnet build` and `dotnet test` green
- [ ] `npm run build` and `npm run lint` green
- [ ] `dotnet list package --vulnerable --include-transitive` clean
- [ ] Migrations reviewed as SQL
- [ ] Every item in [08-security.md](08-security.md#security-checklist-before-production)

**Deploy**
- [ ] Back up the database first
- [ ] Confirm every required environment variable is set
- [ ] Deploy; watch startup logs through "Application started"
- [ ] `GET /health` → `Healthy`
- [ ] Log in; create, submit, and approve one test entry
- [ ] `/hangfire` shows the four recurring jobs registered

**Rollback**
- [ ] Redeploy the previous image
- [ ] If a migration ran, roll the schema back deliberately — the app does not do it for
      you. Know the down path *before* deploying

## CI/CD

**There is none.** `CLAUDE.md` referred to `.github/workflows/ci.yml`; no `.github`
directory exists. Finding **O4**.

A minimal pipeline should run, on every push: `dotnet build`, `dotnet test`,
`dotnet list package --vulnerable --include-transitive`, `npm ci`, `npm run lint`,
`npm run build`, and both Docker builds. That is enough to keep the verified baseline in
[09-development-setup.md](09-development-setup.md) honest.
