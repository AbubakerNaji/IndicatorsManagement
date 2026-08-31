# 05 — API Reference

Base URL: `/api/v1`. Interactive Swagger UI at `/swagger` in Development only.

## Conventions

**Envelope.** Every endpoint returns `ApiResponse` or `ApiResponse<T>`:

```jsonc
{
  "success": true,
  "message": "تم إنشاء الإدخال بنجاح",   // Arabic, user-facing, may be null
  "data": { },                            // absent on the non-generic form
  "errors": ["…"]                         // present on validation failures
}
```

**Paged results** wrap the payload again:

```jsonc
{ "success": true, "data": { "items": [], "totalCount": 0, "page": 1, "pageSize": 20 } }
```

**Authentication.** `Authorization: Bearer <jwt>` on everything except
`POST /auth/login`. The token must also match a live `user_sessions` row — see
[04-workflows.md](04-workflows.md#7-sessions).

**Correlation.** Send `X-Correlation-Id` to have it echoed and attached to every log
line for the request; one is generated if you do not.

**Status codes.** `200` OK · `201` Created · `400` business-rule or validation failure ·
`401` unauthenticated or session expired · `403` role not permitted · `404` not found ·
`429` rate limited · `500` unhandled.

**Rate limits.** 200 requests/minute per IP globally; 5/minute on `POST /auth/login`.

**Roles column.** "Any" means any authenticated user. Role names are the literal strings
in [Roles.cs](../src/IndicatorsManagement.Contracts/Constants/Roles.cs).

---

## Authentication — `/auth`

| Method | Path | Roles | Notes |
|---|---|---|---|
| POST | `/auth/login` | **Anonymous** | 5/min. Accepts username *or* email |
| POST | `/auth/logout` | Any | Deletes the session row |
| POST | `/auth/admin-reset-password` | Super_Admin, Entity_Admin | Sets a new password, clears lockout |

<details><summary>POST /auth/login</summary>

```jsonc
// request
{ "userNameOrEmail": "admin", "password": "Admin@123456" }

// 200
{ "success": true, "data": {
    "token": "eyJhbGciOi…",
    "expiresAt": "2026-08-22T16:16:40Z",
    "user": { "id": 1, "userName": "admin", "fullNameAr": "مدير النظام",
              "email": "admin@indicators.gov", "role": "Super_Admin",
              "entityId": null, "entityNameAr": null } } }
```

Failure returns `success: false` with a deliberately non-specific Arabic message so that
valid usernames cannot be enumerated. Lockout is 5 failed attempts / 15 minutes.

**Only the first role is returned.** `roles.FirstOrDefault()` — a user with several roles
gets an arbitrary one, and the frontend keys authorization off that single value
(finding **S6**).
</details>

---

## Indicator entries — `/indicator-entries`

The core resource.

| Method | Path | Roles |
|---|---|---|
| GET | `/indicator-entries?entityId&indicatorId&periodId&page&pageSize` | Any |
| GET | `/indicator-entries/{id}` | Any ⚠️ |
| POST | `/indicator-entries` | Super_Admin, Entity_Admin, Data_Entry_User |
| PUT | `/indicator-entries/{entryId}` | Super_Admin, Entity_Admin, Data_Entry_User ⚠️ |
| DELETE | `/indicator-entries/{entryId}` | Super_Admin, Entity_Admin, Data_Entry_User ⚠️ |
| POST | `/indicator-entries/{entryId}/submit` | Super_Admin, Entity_Admin, Data_Entry_User ⚠️ |
| POST | `/indicator-entries/{entryId}/approve-entity` | Super_Admin, Entity_Admin, Reviewer ⚠️ |
| POST | `/indicator-entries/{entryId}/approve-ministry` | Super_Admin, Ministry_Admin |
| POST | `/indicator-entries/{entryId}/reject` | Super_Admin, Ministry_Admin, Entity_Admin, Reviewer ⚠️ |
| POST | `/indicator-entries/{entryId}/return` | Super_Admin, Ministry_Admin, Entity_Admin, Reviewer ⚠️ |

> ⚠️ **These endpoints do not verify that the entry belongs to the caller's entity.**
> The list endpoint scopes correctly — for `Data_Entry_User`, `Entity_Admin`, and
> `Reviewer` it forces `entityId` to the caller's own. Every by-id endpoint then trusts
> the id. A `Data_Entry_User` at entity A can read, edit, delete, and submit entity B's
> entries by guessing sequential ids. This is finding **S5**, the most serious open
> issue in the codebase.

<details><summary>POST /indicator-entries</summary>

```jsonc
// request — EntityId comes from the caller's JWT claim, never from the body
{ "indicatorId": 1, "reportingPeriodId": 13,
  "valueNumeric": 1234.5, "valueText": null,
  "notes": "…", "source": "…",
  "dimensions": [ { "dimensionId": 3, "dimensionValueId": 11, "valueNumeric": 500 } ] }
```

Rejected with `400` when: the entity is not `"active"`; no active in-date assignment
exists; an active entry already exists for the triple; mandatory dimensions are missing.
On success the entry is `Draft`, `UnitSnapshot` is copied from the indicator, and the
obligation moves to `In_Progress`.
</details>

<details><summary>Workflow actions</summary>

`submit` takes no body. `approve-entity` and `approve-ministry` accept an optional
`{ "notes": "…" }`. `reject` and `return` **require** `{ "notes": "…" }` — an empty
reason is refused. Legal source states are in
[04-workflows.md](04-workflows.md#transition-table).
</details>

---

## Publication — `/indicator-entries/*/publish`, `/publication`

| Method | Path | Roles |
|---|---|---|
| POST | `/indicator-entries/{entryId}/publish` | Super_Admin, Ministry_Admin |
| POST | `/indicator-entries/{entryId}/unpublish` | Super_Admin, Ministry_Admin |
| POST | `/indicator-entries/bulk-publish` | Super_Admin, Ministry_Admin |
| POST | `/indicator-entries/bulk-unpublish` | Super_Admin, Ministry_Admin |
| GET | `/indicator-entries/{entryId}/publication-history` | Any |
| GET | `/publication/statistics` | Super_Admin, Ministry_Admin |

Single actions accept `{ "reason": "…" }`; bulk actions take
`{ "entryIds": [1,2,3], "reason": "…" }`.

---

## Attachments

| Method | Path | Roles |
|---|---|---|
| POST | `/indicator-entries/{entryId}/attachments` | Super_Admin, Entity_Admin, Data_Entry_User ⚠️ |
| GET | `/attachments/{id}/download` | Any ⚠️ |
| DELETE | `/attachments/{id}` | Super_Admin, Entity_Admin, Data_Entry_User ⚠️ |

`multipart/form-data`, field name `file`. Accepted extensions: `.xlsx .xls .pdf .doc
.docx .png .jpg .jpeg`. Max size from `FileUploadMaxSize_MB` (default 10). Upload and
delete are allowed only while the entry is `Draft` or `Returned_For_Modification`.

> ⚠️ Same object-level authorization gap (**S5**): download performs **no** check at all,
> so any authenticated user — including a `Viewer` — can fetch any attachment, including
> ones hanging off another entity's unapproved draft. Extension is validated but content
> is not (**S3**), and files are written to local disk (**O1**).

This controller talks to `IndicatorsDbContext` directly instead of going through a
service — finding **A2**.

---

## Indicators — `/indicators`

| Method | Path | Roles |
|---|---|---|
| GET | `/indicators?isActive&page&pageSize` | Any |
| GET | `/indicators/{id}` | Any |
| POST | `/indicators` | Super_Admin, Ministry_Admin |
| PUT | `/indicators/{id}` | Super_Admin, Ministry_Admin |
| DELETE | `/indicators/{id}` | Super_Admin |
| POST | `/indicators/{indicatorId}/dimensions` | Super_Admin, Ministry_Admin |
| PUT | `/indicators/dimensions/{dimensionId}` | Super_Admin, Ministry_Admin |
| DELETE | `/indicators/dimensions/{dimensionId}` | Super_Admin, Ministry_Admin |

`Code` is immutable after creation — `UpdateIndicator` ignores any attempt to change it
(there is a test for this).

---

## Entities — `/entities`

| Method | Path | Roles |
|---|---|---|
| GET | `/entities` | Any |
| GET | `/entities/{id}` | Any |
| POST | `/entities` | Super_Admin |
| PUT | `/entities/{id}` | Super_Admin |
| DELETE | `/entities/{id}` | Super_Admin |

`DELETE` **deactivates** (`Status = "inactive"`); it never removes the row. A deactivated
entity cannot have new entries created against it.

---

## Assignments — `/indicator-assignments`

| Method | Path | Roles |
|---|---|---|
| GET | `/indicator-assignments?indicatorId&entityId&page&pageSize` | Any |
| POST | `/indicator-assignments` | Super_Admin, Ministry_Admin |
| PUT | `/indicator-assignments/{id}` | Super_Admin, Ministry_Admin |
| POST | `/indicator-assignments/{id}/generate-obligations` | Super_Admin, Ministry_Admin |

---

## Reporting periods — `/reporting-periods`

| Method | Path | Roles |
|---|---|---|
| GET | `/reporting-periods?periodType&year` | Any |
| POST | `/reporting-periods/generate` | Super_Admin |

---

## Users — `/users`

Controller-level: `[Authorize(Roles = Super_Admin, Ministry_Admin, Entity_Admin)]`.

| Method | Path | Notes |
|---|---|---|
| GET | `/users?entityId&page&pageSize` | `Entity_Admin` is forced to their own entity ✅ |
| GET | `/users/{id}` | ⚠️ No scoping — any admin reads any user |
| POST | `/users` | `Entity_Admin` is forced to their own entity ✅ |
| PUT | `/users/{id}` | ⚠️ No scoping |
| DELETE | `/users/{id}` | Deactivates (`IsActive = false`) |

---

## Dashboards — `/dashboard`

| Method | Path | Roles |
|---|---|---|
| GET | `/dashboard/ministry` | Super_Admin, Ministry_Admin |
| GET | `/dashboard/entity/{id}` | Any ⚠️ |
| GET | `/dashboard/tasks` | Any — scoped to the caller ✅ |

> ⚠️ `/dashboard/entity/{id}` accepts any id from any authenticated user. Part of **S5**.

---

## Notifications, search, config, audit, drafts

| Method | Path | Roles |
|---|---|---|
| GET | `/notifications?type&page&pageSize` | Any — own only ✅ |
| PUT | `/notifications/{id}/read` | Any |
| PUT | `/notifications/read-all` | Any |
| GET | `/search?q&page&pageSize` | Any |
| GET | `/config` | Super_Admin, Ministry_Admin |
| PUT | `/config` | Super_Admin |
| GET | `/validation-rules?indicatorId` | Super_Admin, Ministry_Admin |
| POST · PUT · DELETE | `/validation-rules[/{id}]` | Super_Admin, Ministry_Admin |
| GET | `/audit-logs?userId&actionType&entityType&from&to&page&pageSize` | Super_Admin, Auditor |
| GET | `/audit-logs/entity/{entityType}/{entityId}` | Super_Admin, Auditor |
| POST | `/drafts/save` | Any |
| GET | `/drafts/recover` | Any — own only ✅ |
| DELETE | `/drafts/{id}` | Any |

`AuditLogsController` also queries `IndicatorsDbContext` directly (finding **A2**).

---

## Operational endpoints

| Path | Auth | Purpose |
|---|---|---|
| `GET /health` | none | SQL Server connectivity. `200 Healthy` / `503` |
| `GET /swagger` | none | Development only |
| `/hangfire` | `HangfireDashboardAuthFilter` | Job dashboard |

---

## Authorization: what is actually enforced

Three mechanisms exist. Only two are used.

1. **Controller/action role attributes** — `[Authorize(Roles = "…")]`. This is the real,
   working mechanism, applied throughout.
2. **Service-layer entity scoping** — the caller's `EntityId` claim is passed into
   `CreateEntryAsync` and `GetUserTasksAsync`, and the list endpoints narrow by it.
   Applied inconsistently, and never on by-id endpoints.
3. **Named authorization policies** — nine are registered in
   [Program.cs](../src/IndicatorsManagement.Api/Program.cs), including `EntityScoped`
   backed by [EntityAccessHandler](../src/IndicatorsManagement.Api/Authorization/EntityAccessHandler.cs).
   **Zero endpoints use them.** `grep -r "Authorize(Policy" src` returns nothing. The
   handler is dead code.

Mechanism 3 was built to solve exactly the problem that mechanism 2 leaves open — and
was never wired up. That is why the ⚠️ markers above cluster on by-id routes. See
finding **S5** in [13-review-findings.md](13-review-findings.md) and the role matrix in
[08-security.md](08-security.md).
