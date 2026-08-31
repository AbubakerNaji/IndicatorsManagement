# 09 — Development Setup

Everything runs locally. No remote database, no shared environment.

## Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` |
| Node.js | 20+ (22 recommended) | `node --version` |
| Docker | with Compose v2 | `docker compose version` |

Apple Silicon: the SQL Server 2022 image is `linux/amd64` and runs under emulation.
It works — verified on Darwin 25.5 / arm64 — but the first start takes about a minute.

## First run

### 1. Environment file

```bash
cp .env.example .env
```

The defaults are fine for local work. `DB_SA_PASSWORD` must satisfy SQL Server's policy:
8+ characters from at least three of uppercase, lowercase, digits, symbols.

### 2. Start the database

```bash
docker compose -f docker-compose.dev.yml up -d
```

This starts SQL Server alone on `localhost:1433`. The API and frontend run on the host.

Wait for it to report healthy:

```bash
docker inspect --format '{{.State.Health.Status}}' indicators-db-dev
```

<details><summary>Port 1433 already in use</summary>

```
Bind for 0.0.0.0:1433 failed: port is already allocated
```

Find the holder:

```bash
lsof -nP -iTCP:1433 -sTCP:LISTEN
docker ps -a --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
```

A stopped container can keep the binding — remove it (`docker rm <name>`), or use another
port:

```bash
echo "DB_HOST_PORT=14330" >> .env
docker compose -f docker-compose.dev.yml up -d
```

Then change `localhost,1433` to `localhost,14330` in both connection strings in
`appsettings.Development.json`.
</details>

### 3. Backend settings

```bash
cp src/IndicatorsManagement.Api/appsettings.Development.json.example \
   src/IndicatorsManagement.Api/appsettings.Development.json
```

The file is git-ignored and contains local-only credentials that match
`docker-compose.dev.yml`. `appsettings.json` itself holds no secrets and never should.

Prefer user-secrets instead? The project is already initialised:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=IndicatorsManagement;User ID=sa;Password=Local_Dev_P@ssw0rd_2026;TrustServerCertificate=True;Encrypt=False;" \
  --project src/IndicatorsManagement.Api
```

### 4. Run the API

```bash
dotnet restore IndicatorsManagement.slnx
dotnet build   IndicatorsManagement.slnx
dotnet run --project src/IndicatorsManagement.Api
```

On first start it will, in order: validate configuration, create the Hangfire database,
apply all migrations, and seed 15 entities, 120 indicators, 120 assignments, 57 reporting
periods, 7 roles, 6 config keys, and the admin user. Expect roughly a minute under
emulation; subsequent starts are seconds because every seed step is idempotent.

Ready when you see:

```
[INF] Now listening on: http://localhost:5117
[INF] Application started. Press Ctrl+C to shut down.
```

Verify:

```bash
curl http://localhost:5117/health            # → Healthy

curl -X POST http://localhost:5117/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userNameOrEmail":"admin","password":"Admin@123456"}'
```

Swagger: <http://localhost:5117/swagger> · Hangfire: <http://localhost:5117/hangfire>

### 5. Run the frontend

```bash
cd frontend
cp .env.example .env.local     # VITE_API_URL=http://localhost:5117
npm install
npm run dev
```

<http://localhost:5173> — sign in as `admin` / `Admin@123456`.

> Change that password immediately on any machine that is not your own laptop. It is
> hardcoded in the seeder (finding **S8**).

## Daily commands

```bash
# backend
dotnet build IndicatorsManagement.slnx
dotnet test  tests/IndicatorsManagement.Tests/
dotnet run   --project src/IndicatorsManagement.Api
dotnet format IndicatorsManagement.slnx

# frontend
cd frontend
npm run dev
npm run build      # tsc -b && vite build — type errors fail here
npm run lint

# database
docker compose -f docker-compose.dev.yml up -d
docker compose -f docker-compose.dev.yml logs -f db
docker compose -f docker-compose.dev.yml down       # keep data
docker compose -f docker-compose.dev.yml down -v    # wipe data

docker exec -it indicators-db-dev /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$DB_SA_PASSWORD" -C -No -d IndicatorsManagement

# migrations
dotnet ef migrations add <Name> \
  --project src/IndicatorsManagement.Infrastructure \
  --startup-project src/IndicatorsManagement.Api
```

## Full stack in Docker

To run everything in containers instead:

```bash
cp .env.example .env     # set JWT_SECRET_KEY to a real value
docker compose up --build
```

Frontend <http://localhost> · API <http://localhost:8080> · Health
<http://localhost:8080/health>. See [10-deployment.md](10-deployment.md).

## Verified baseline

Reproduced from a clean database during this review:

| Check | Result |
|---|---|
| `dotnet build IndicatorsManagement.slnx` | Succeeded, 0 warnings |
| `dotnet test` | **76 passed**, 0 failed |
| `npm run build` | Succeeded, one bundle-size warning |
| `dotnet list package --vulnerable --include-transitive` | Clean |
| API startup from empty SQL Server | Both databases created, seeded, `/health` → `Healthy` |
| `POST /auth/login` as admin | Returns a valid JWT |

If your local results differ, something in your environment changed — start there.

## Troubleshooting

**`Startup configuration is incomplete for environment 'Development'`**
The guard is doing its job. Create `appsettings.Development.json` from the example, or set
user-secrets. The message lists exactly what is missing.

**`Cannot open database "IndicatorsManagement_Hangfire"`**
Should no longer happen — `SqlServerDatabaseBootstrapper` creates it. If it does, the
login lacks permission to create databases; create it by hand:
`CREATE DATABASE [IndicatorsManagement_Hangfire]`.

**`Login failed for user 'sa'`**
The password in your connection string does not match `DB_SA_PASSWORD` from when the
container's volume was created. Changing `.env` afterwards does not change the existing
password — `docker compose -f docker-compose.dev.yml down -v` and start over.

**Frontend gets CORS errors**
Add your dev origin to `Cors:AllowedOrigins` in `appsettings.Development.json`.

**401 on every request right after logging in**
`SessionValidationMiddleware` could not find the session row. Clear `localStorage` and
sign in again; if it persists, check that `user_sessions` is being written.

**Seed data looks wrong or incomplete**
Wipe and rebuild: `docker compose -f docker-compose.dev.yml down -v && up -d`, then run
the API. Seeding only fills empty tables — it never repairs partial data.

**Build warns about copying into `bin/Debug/net10.0/bin/...`**
A stale nested output directory from running `dotnet run` inside the project folder.
`rm -rf src/IndicatorsManagement.Api/bin` and rebuild.
