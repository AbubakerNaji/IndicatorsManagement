# Design — Enforce entity-scoped authorization

## Context

Three authorization mechanisms exist in the codebase today:

1. **Role attributes** — `[Authorize(Roles = "…")]` on controllers and actions. Working
   and used everywhere. It answers *"may this kind of user do this kind of thing?"*
2. **Ad-hoc service scoping** — the caller's `EntityId` claim is passed into
   `CreateEntryAsync` and `GetUserTasksAsync`, and list endpoints narrow by it. Correct
   where present, absent everywhere else.
3. **Named policies** — nine registered in `Program.cs`, including `EntityScoped` backed
   by `EntityAccessHandler`. **Never applied.** `grep -r "Authorize(Policy" src` → zero.

The missing question is *"may this user touch **this** object?"* — object-level, not
type-level. Mechanism 1 cannot express it. Mechanism 3 was intended to and cannot, for a
structural reason explained below. Mechanism 2 is the only one that can, and it was
applied inconsistently.

No database change is involved. `IndicatorEntry.EntityId` already carries the ownership
fact; nothing reads it at the right moment.

## Goals / Non-Goals

**Goals**

- Every by-id endpoint refuses objects outside the caller's entity.
- Ownership is decided from the JWT `EntityId` claim only.
- Refusals are indistinguishable from genuine absence.
- A negative test exists per endpoint, so a regression fails the build.
- Remove the dead policy machinery so nothing implies protection that is not there.

**Non-Goals**

- Not a general permission system. No per-object ACLs, no delegation, no sharing.
- Not the `A1` refactor. Services stay in `Infrastructure`; this change must not be
  blocked behind that one.
- Not changing the role matrix. Who may do what stays exactly as documented in
  `Docs/08-security.md`.
- Not addressing `S4` (self-approval). Separation of duties is a distinct policy question.
- Not hardening `AuditLogsController` beyond leaving `Auditor` access cross-entity, which
  is intended.

## Decisions

### D1 — Enforce in the service layer, not in an authorization handler

`EntityAccessHandler` receives an `AuthorizationHandlerContext` and can read route values
and query strings. That is sufficient for `?entityId=3` and useless for
`/indicator-entries/47`, where the owning entity is a column on a row that has not been
loaded yet. The handler's current fallback is telling:

```csharp
// If no entityId in request, succeed (the service layer will scope by user's entity)
if (string.IsNullOrEmpty(routeEntityId)) { context.Succeed(requirement); … }
```

It defers to a service-layer check that, for by-id routes, does not exist. Even if the
policy had been applied to every endpoint, it would have succeeded on all of them.

The check must happen **after the object is loaded and before anything is returned**.
That is the service.

*Alternative considered — a resource-based handler* (`IAuthorizationService.AuthorizeAsync(user, entry, policy)`
called from the controller). This is the idiomatic ASP.NET Core answer and does work: the
controller loads the object, then authorizes it. Rejected because it requires every
controller to load the object itself — which means either duplicating the service's query
or exposing entities from the service purely to authorize them. Given `A1` (services hold
the queries), keeping the check next to the load is both simpler and harder to skip.

### D2 — Pass a caller context, not two loose parameters

Rather than threading `int userEntityId, string userRole` through sixteen signatures, add
a small record in `Contracts`:

```csharp
public sealed record CallerContext(int UserId, int? EntityId, string Role)
{
    public bool IsMinistryLevel => Role is Roles.SuperAdmin or Roles.MinistryAdmin;
    public bool CanAccessEntity(int entityId) => IsMinistryLevel || EntityId == entityId;
}
```

Controllers build it once from claims; services take it as a parameter. Adding a future
dimension to the decision means changing one type, not sixteen signatures.

*Alternative considered — inject `IHttpContextAccessor` into the services* and read claims
there. Rejected: it makes every service depend on HTTP, which is a worse layering
violation than the one already documented in `A1`, and it makes the services untestable
without faking an HTTP context.

### D3 — Return "not found", not "forbidden"

A distinct `403` confirms that the id exists. Against sequential integer ids, that turns
the API into an enumeration oracle for how many entries each entity has. Reusing the
existing `ApiResponse.Fail("الإدخال غير موجود")` costs nothing and reveals nothing.

Note this is already the shape of the code — the existing null check returns exactly that
message — so the ownership check simply joins the same branch:

```csharp
if (entry is null || entry.IsDeleted || !caller.CanAccessEntity(entry.EntityId))
    return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");
```

### D4 — Introduce `IAttachmentService`

Attachments cannot be secured while `AttachmentsController` owns the queries: the
authorization decision needs the parent entry, and the controller currently reaches for
`_db.Attachments.FindAsync(id)` with no join. Extracting a service is a prerequisite here,
not scope creep. It also closes half of finding **A2**.

`AuditLogsController` is deliberately left alone — `Auditor` access is cross-entity by
design, so it gains nothing from this change and can be refactored separately.

### D5 — Delete the dead policy machinery

Once D1 is in place, `EntityAccessHandler`, `EntityAccessRequirement`, and the
`EntityScoped` policy registration are removed.

*Alternative considered — keep them and apply them to the query-string cases.* Rejected:
two mechanisms answering the same question, one of which silently succeeds in the common
case, is how this bug happened. One mechanism, applied everywhere, is the fix.

The other eight policies are also unused but are simple role aggregations that duplicate
the `[Authorize(Roles=…)]` attributes. They are left in place; removing them is unrelated
tidying and belongs in its own change.

### D6 — Tests before implementation

The negative tests are written first, watched to fail, then made to pass. This is not
ceremony: **T2** exists precisely because no test ever asked the question, and a fix
landed without tests would leave the next regression equally invisible.

Test setup uses `WebApplicationFactory<Program>` with forged JWTs per role, which requires
making `Program` visible to the test project — a `public partial class Program { }` at the
end of `Program.cs` is the least invasive way.

## Risks / Trade-offs

**A legitimate cross-entity read starts returning 404.** → The frontend only ever requests
ids obtained from a scoped list, so it cannot hit this. Before merging, grep the frontend
for any hardcoded id and check the Ministry dashboard's drill-down paths, which legitimately
cross entities but run under ministry roles.

**`EntityId` is nullable on `ApplicationUser`.** A user with no entity and a non-ministry
role would be refused everything. → That state is already broken today (they can create no
entries), but `CanAccessEntity` must handle `null` explicitly rather than defaulting to a
match. Add a test.

**Sixteen signature changes make a large diff.** → Mechanical and compiler-checked; the
build fails on every call site that was not updated. Do it in one pass rather than
spreading it over several changes.

**`Viewer` scoping is subtly different.** A `Viewer` should see published data across
entities, not just their own. → The spec handles this: `Viewer` is entity-scoped for
attachments and unpublished data, while the existing `publishedOnly` filter governs what
they see in lists. Confirm the intended `Viewer` breadth with the Ministry before
finalising; if `Viewer` is meant to be cross-entity for published data, it belongs in the
ministry-level exemption for read paths only.

**Ministry dashboard performance.** → Unchanged; ministry roles skip the check entirely.

## Migration Plan

No schema change, no data migration, no downtime. The change is a redeploy.

**Rollback** — redeploy the previous image. Because nothing persists differently, rollback
is clean.

**Verification after deploy** — log in as a `Data_Entry_User`, obtain an entry id belonging
to another entity from the database directly, and confirm the API returns 404.

## Open Questions

1. **Should `Viewer` be cross-entity for published data?** The current list behaviour
   suggests yes (it filters by publication, not by entity). Needs a decision from the
   Ministry before the spec's `Viewer` scenarios are final.
2. **Should a refused access be audited?** A cross-entity attempt is a meaningful security
   signal. Recommendation: log it to `audit_logs` with `ResultStatus = "Failure"` and a
   distinct `ActionType`. Cheap to add here, and it turns a silent 404 into evidence.
3. **Does `Ministry_Admin` need write access to every entity's entries, or only approval?**
   Today the role exemption grants both. Worth confirming it is intended.
