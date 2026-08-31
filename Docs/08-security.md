# 08 — Security

## Secrets policy

**No secret, credential, connection string, or hostname belongs in a committed file.**
This is the rule; [ADR-0006](adr/0006-no-secrets-in-committed-configuration.md) is the
reasoning.

### Where configuration comes from

.NET merges these in order, each overriding the last:

```
appsettings.json                    committed · empty placeholders only
appsettings.{Environment}.json      NOT committed · local development values
user-secrets                        NOT committed · per-developer, outside the repo
environment variables               how staging and production are configured
command line
```

| Setting | Local development | Staging / production |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` | `ConnectionStrings__DefaultConnection` env var |
| `ConnectionStrings:HangfireConnection` | `appsettings.Development.json` | `ConnectionStrings__HangfireConnection` env var |
| `Jwt:SecretKey` | `appsettings.Development.json` | `Jwt__SecretKey` env var |
| `Smtp:Password` | usually empty | `Smtp__Password` env var |

Double underscore (`__`) is the nesting separator in environment variable names.

### Fail-fast validation

[ConfigurationValidationExtensions](../src/IndicatorsManagement.Api/Extensions/ConfigurationValidationExtensions.cs)
runs before anything else in `Program.cs` and refuses to start when:

- either connection string is missing,
- `Jwt:SecretKey` is missing or shorter than 32 bytes (HMAC-SHA256 needs 256 bits),
- outside Development, `Jwt:SecretKey` still contains a template placeholder such as
  `CHANGE-THIS`, `local-development-only`, or `replace-me`.

The error lists everything that is wrong at once and tells you how to fix it for the
current environment. Previously a missing key surfaced as a null-reference deep inside a
provider, and a placeholder key would have shipped to production silently.

### Generating a key

```bash
openssl rand -base64 48
```

Use a different key per environment. Rotating it invalidates every issued token, which is
the point.

### Local development secrets

`appsettings.Development.json` is git-ignored and holds local-only credentials matching
`docker-compose.dev.yml`. Create it from the template:

```bash
cp src/IndicatorsManagement.Api/appsettings.Development.json.example \
   src/IndicatorsManagement.Api/appsettings.Development.json
```

Prefer user-secrets if you would rather keep even those outside the working tree — the
project is already initialised (`UserSecretsId` in the `.csproj`):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "…" --project src/IndicatorsManagement.Api
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 48)" --project src/IndicatorsManagement.Api
```

### Historical exposure

Before this review, live credentials were committed in `appsettings.json`,
`appsettings.Staging.json`, `appsettings.Production.json`, and `.mcp.json`: a database
host, a SQL login, and its password. They have been removed from the working tree.

**Removal from files is not remediation.** If those files were ever pushed to a remote,
committed to git history, or shared, the credentials must be treated as compromised —
see the checklist under finding **S0** in
[13-review-findings.md](13-review-findings.md).

## Authentication

**Flow**

1. `POST /api/v1/auth/login` with username-or-email and password. Rate limited to 5/min.
2. `UserManager` resolves the user; inactive accounts are refused.
3. Lockout is checked, then `CheckPasswordSignInAsync(..., lockoutOnFailure: true)`.
4. On success a JWT is signed with HS256 and a `user_sessions` row stores that token.
5. Failed-access count resets; the attempt is written to `audit_logs` either way.

**Token**

| Property | Value |
|---|---|
| Algorithm | HMAC-SHA256 |
| Lifetime | `Jwt:ExpirationMinutes`, default 30 |
| Clock skew | **Zero** — expiry is exact |
| Validated | issuer, audience, lifetime, signing key |
| Claims | `nameidentifier`, `name`, `emailaddress`, `role`, `FullNameAr`, `EntityId` (when set) |

**Password policy** (ASP.NET Core Identity): minimum 8 characters, requires digit,
lowercase, uppercase, and non-alphanumeric. Lockout after 5 failures for 15 minutes.
Unique email required.

**Login responses are deliberately vague** — unknown user and wrong password return the
same Arabic message, so accounts cannot be enumerated. Inactive and locked accounts do
return distinct messages, which is a small trade of secrecy for usability.

## Sessions

A valid JWT is necessary but not sufficient. `SessionValidationMiddleware` additionally
requires a matching, unexpired, non-idle `user_sessions` row, and refreshes
`LastActivity` on each request. Logout deletes the row, so a leaked token stops working
immediately instead of at natural expiry. Rationale and cost in
[ADR-0005](adr/0005-server-side-session-validation.md).

**The full JWT is stored in plaintext** in `user_sessions.SessionToken`. Anyone who can
read that table can impersonate any logged-in user. Storing a SHA-256 hash would work
identically for lookup. Finding **S2**.

## The role matrix

| Capability | Super_Admin | Ministry_Admin | Entity_Admin | Data_Entry_User | Reviewer | Auditor | Viewer |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Manage entities | ✅ | — | — | — | — | — | — |
| Manage indicators | ✅ | ✅ | — | — | — | — | — |
| Manage dimensions | ✅ | ✅ | — | — | — | — | — |
| Manage reporting periods | ✅ | — | — | — | — | — | — |
| Manage assignments | ✅ | ✅ | — | — | — | — | — |
| Manage validation rules | ✅ | ✅ | — | — | — | — | — |
| Manage users | ✅ | ✅ | own entity | — | — | — | — |
| Reset passwords | ✅ | — | ✅ | — | — | — | — |
| Create / edit entries | ✅ | — | ✅ | ✅ | — | — | — |
| Submit entries | ✅ | — | ✅ | ✅ | — | — | — |
| Approve — entity level | ✅ | — | ✅ | — | ✅ | — | — |
| Approve — ministry level | ✅ | ✅ | — | — | — | — | — |
| Reject / return | ✅ | ✅ | ✅ | — | ✅ | — | — |
| Publish / unpublish | ✅ | ✅ | — | — | — | — | — |
| Ministry dashboard | ✅ | ✅ | — | — | — | — | — |
| Read audit log | ✅ | — | — | — | — | ✅ | — |
| System configuration | read+write | read | — | — | — | — | — |
| Published data | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## Authorization: three mechanisms, two in use

1. **Role attributes** — `[Authorize(Roles = "…")]` on controllers and actions. Working,
   used everywhere, and the basis of the matrix above.
2. **Service-layer scoping** — the caller's `EntityId` claim is passed into
   `CreateEntryAsync` and `GetUserTasksAsync`; list endpoints narrow by it for
   entity-scoped roles. Correct where applied.
3. **Named policies** — nine registered in `Program.cs`, including `EntityScoped` backed
   by `EntityAccessHandler`. **Never applied to a single endpoint.** `grep -r
   "Authorize(Policy" src` returns zero matches; the handler never executes.

Mechanism 3 exists precisely to close the gaps mechanism 2 leaves, and was never wired
up. The result is finding **S5**.

### S5 — broken object-level authorization

Endpoints that take an id and never check who owns it:

| Endpoint | Consequence |
|---|---|
| `GET /indicator-entries/{id}` | Any authenticated user reads any entry, including other entities' drafts |
| `PUT /indicator-entries/{id}` | Any data-entry user edits another entity's draft |
| `DELETE /indicator-entries/{id}` | …and deletes it |
| `POST /indicator-entries/{id}/submit` · `/approve-entity` · `/reject` · `/return` | Workflow actions on another entity's entries |
| `GET /attachments/{id}/download` | **No check at all** — any user, including `Viewer`, downloads any file |
| `DELETE /attachments/{id}` | Deletes another entity's attachment |
| `POST /indicator-entries/{entryId}/attachments` | Attaches to another entity's entry |
| `GET /dashboard/entity/{id}` | Reads any entity's dashboard |
| `GET`/`PUT /users/{id}` | An `Entity_Admin` reads and edits users outside their entity |

Ids are sequential integers, so enumeration is trivial. **Treat this as the highest
priority item in [13-review-findings.md](13-review-findings.md).**

## Transport and headers

- **TLS 1.2 / 1.3 only** — Kestrel's HTTPS defaults are pinned in `Program.cs`.
- `UseHttpsRedirection()` is enabled.
- **CORS** is an allow-list from `Cors:AllowedOrigins`, with credentials permitted.
  Defaults to `http://localhost:3000` and `http://localhost:5173`. Set it explicitly per
  environment — it must never be a wildcard while `AllowCredentials()` is on.
- **No security headers are set** — no HSTS, `X-Content-Type-Options`,
  `X-Frame-Options`, `Referrer-Policy`, or CSP. Finding **S9**.

## Rate limiting

| Partition | Limit |
|---|---|
| Global, per IP | 200 requests/minute |
| `"auth"` policy on login | 5 requests/minute, no queue |
| `"general"` policy | 100/minute, queue 10 — **registered but never applied** |

Partitioning is by `RemoteIpAddress`. Behind a reverse proxy every request appears to
come from the proxy unless forwarded headers are configured — they are not, so in the
Docker deployment the global limiter effectively applies to the whole cluster at once.
Finding **O3**.

## Auditing

Two independent paths write to `audit_logs`:

1. `AuditLoggingMiddleware` — every authenticated `POST`/`PUT`/`PATCH`/`DELETE`, with
   method, path, status class, and IP.
2. `IAuditLogService.LogAsync` — explicit calls in services recording the *domain*
   action (`Submit_Entry`, `Approve_Ministry_Level`, `Reject_Entry`), with old and new
   values as JSON.

One user action therefore usually produces two rows: the HTTP fact and the business fact.
That is intentional — the first proves a request happened, the second says what it meant.

Auth events (`Login_Success`, `Login_Failed`, `Login_Failed_Locked`,
`Login_Failed_Inactive`, `Logout`, `Admin_Password_Reset`) are always recorded.

**The audit trail is not tamper-evident.** Rows are ordinary, updatable, deletable
records with no hash chain or append-only enforcement. For a ministry compliance system
that may not be sufficient — finding **S10**.

## File uploads

Extension allow-list (`.xlsx .xls .pdf .doc .docx .png .jpg .jpeg`), size limit from
`FileUploadMaxSize_MB` (default 10), stored as `uploads/{entryId}/{guid}{ext}` with a
generated name so the original filename cannot traverse paths.

Not done: content-type sniffing or magic-byte verification (a `.pdf` may hold anything),
malware scanning, and any authorization on download. Findings **S3** and **S5**.

## Dependency status

`dotnet list package --vulnerable --include-transitive` is **clean** as of this review.
`Microsoft.OpenApi` 2.4.1 (transitive via Swashbuckle 10.1.7) carried
[GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc), high severity;
Swashbuckle was upgraded to 10.2.3, which resolves it. Re-run the check in CI.

## Security checklist before production

- [ ] **Rotate every credential that was ever committed** (finding **S0**)
- [ ] Fix object-level authorization (**S5**)
- [ ] Change the seeded admin password (**S8**)
- [ ] Set `Jwt__SecretKey` to a freshly generated per-environment value
- [ ] Set `Cors__AllowedOrigins` to the real front-end origin only
- [ ] Terminate TLS with a real certificate; enable HSTS
- [ ] Add the missing security headers (**S9**)
- [ ] Hash session tokens at rest (**S2**)
- [ ] Configure forwarded headers so rate limiting sees real client IPs (**O3**)
- [ ] Move uploads to durable storage (**O1**)
- [ ] Enable `Serilog:WriteToDatabase` or ship logs off the host
- [ ] Confirm database backups and test a restore
- [ ] Run `dotnet list package --vulnerable --include-transitive` in CI
