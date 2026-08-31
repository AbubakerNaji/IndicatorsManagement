# Enforce entity-scoped authorization on by-id endpoints

## Why

Any authenticated user — including a `Viewer`, the lowest-privileged role — can read,
modify, and download any other entity's data by supplying its id. Entity ids and entry ids
are sequential integers, so enumeration is trivial.

Entities report commercially and politically sensitive figures to the Ministry *before*
publication. Draft and under-review data is precisely the data that must not circulate.
Today `GET /api/v1/indicator-entries/{id}` returns it to anyone who asks, and
`GET /api/v1/attachments/{id}/download` performs no authorization check at all beyond
requiring a valid token.

The list endpoint scopes correctly, which is what disguised the problem: `GET
/indicator-entries` forces `entityId` to the caller's own for entity-scoped roles, so the
UI never shows another entity's data and the gap is invisible in normal use.

The mechanism intended to prevent this — `PolicyNames.EntityScoped` and
`EntityAccessHandler` — was written, registered in `Program.cs`, and **never applied to a
single endpoint**. `grep -r "Authorize(Policy" src` returns zero matches.

Recorded as finding **S5** in `Docs/13-review-findings.md`, the highest-severity open
issue in the codebase.

## What Changes

- Every service method that loads an object by id accepts the caller's entity id and role,
  and refuses objects belonging to another entity unless the caller holds a ministry-level
  role (`Super_Admin`, `Ministry_Admin`) or a role whose remit is cross-entity by design
  (`Auditor` for audit data).
- Refusals return the existing **"not found"** failure rather than a distinct "forbidden",
  so the API does not confirm that an id exists to a caller who may not see it.
- Attachment endpoints resolve authorization through the parent `IndicatorEntry`.
- `GET /dashboard/entity/{id}` and the `/users/{id}` endpoints are scoped the same way.
- **BREAKING for API clients that relied on reading other entities' objects by id.** No
  such legitimate client exists — the frontend only ever requests ids it received from a
  scoped list — but any integration doing so will begin receiving 404.
- Regression tests assert, for every affected endpoint, that a caller from entity A is
  refused entity B's object. These tests are written **first**.
- `EntityAccessHandler` and `PolicyNames.EntityScoped` are removed as dead code once the
  service-layer checks are in place, so nothing suggests a protection that does not exist.

## Capabilities

### New Capabilities

- `entity-scoped-access`: the rule that a user may only reach objects belonging to their
  own entity, which roles are exempt, and how a refusal is reported.

### Modified Capabilities

None. No existing spec files are present in `openspec/specs/` — this is the first change
proposed under OpenSpec, so `entity-scoped-access` is authored new rather than as a delta.

## Impact

**Application** — `IIndicatorEntryService`, `IDashboardService`, `IUserService` interface
signatures gain caller-context parameters. A new `IAttachmentService` is introduced so
that attachment logic leaves the controller (finding **A2**, partially addressed here
because attachments cannot be secured without it).

**Infrastructure** — `IndicatorEntryService`, `DashboardService`, `UserService`
implementations gain the ownership checks; new `AttachmentService`.

**Api** — `IndicatorEntriesController`, `AttachmentsController`, `DashboardController`,
`UsersController` pass the caller's claims through. `Api/Authorization/EntityAccessHandler.cs`,
`EntityAccessRequirement.cs`, and the `EntityScoped` policy registration are deleted.

**Tests** — a new authorization test suite (finding **T2**). This requires
`WebApplicationFactory` and therefore `Program` being visible to the test project.

**Docs** — `05-api-reference.md` (remove the ⚠️ markers), `08-security.md` (rewrite the
"three mechanisms, two in use" section), `13-review-findings.md` (close **S5**, close
**T2**, note partial progress on **A2**).

**No database changes.** No migration required.

**Not affected** — the `Auditor` role's access to `audit_logs` is deliberately
cross-entity and stays as it is.
