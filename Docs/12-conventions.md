# 12 — Conventions

Rules that keep the codebase coherent. Where the codebase is inconsistent today, that is
said plainly, along with which side to write new code on.

## Backend

### Naming

| Thing | Convention | Example |
|---|---|---|
| Project | `IndicatorsManagement.<Layer>` | `IndicatorsManagement.Infrastructure` |
| Namespace | mirrors the folder path | `IndicatorsManagement.Infrastructure.Services` |
| Entity | PascalCase singular | `IndicatorEntry` |
| Table | snake_case plural | `indicator_entries` |
| Enum member | PascalCase, `_` for multi-word | `Approved_By_Entity` |
| Service interface | `I<Area>Service` | `IIndicatorEntryService` |
| DTO | `<Action><Entity>Request` / `<Entity>Response` | `CreateIndicatorEntryRequest` |
| Validator | `<Request>Validator` | `LoginRequestValidator` |
| Controller | `<Plural>Controller` | `IndicatorEntriesController` |
| Route | `api/v1/<kebab-case-plural>` | `api/v1/indicator-entries` |
| Migration | `<Verb><What>` | `ExpandSessionTokenLength` |
| Test | `Method_Condition_Expected` | `CreateEntry_DuplicateActive_ShouldFail` |

`Approved_By_Entity` is not idiomatic C#. It is deliberate: the enum member and the
database string are the same token, so a value read in SQL is greppable in the source.
Do not "fix" it.

### Bilingual fields

Always a pair: `NameAr` required, `NameEn` optional.

```csharp
public string NameAr { get; set; } = string.Empty;   // required
public string? NameEn { get; set; }                   // optional
```

Arabic is the product language. Any new user-facing field needs `*Ar`; add `*En` only if
something will display it.

### Service pattern

```csharp
public class IndicatorEntryService : IIndicatorEntryService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly INotificationService _notification;

    public IndicatorEntryService(IndicatorsDbContext db, IAuditLogService audit, INotificationService notification)
    { _db = db; _audit = audit; _notification = notification; }

    public async Task<ApiResponse<IndicatorEntryResponse>> DoSomethingAsync(int id, int userId)
    {
        var entry = await _db.IndicatorEntries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry is null)
            return ApiResponse<IndicatorEntryResponse>.Fail("الإدخال غير موجود");

        if (entry.WorkflowState != WorkflowState.Expected)
            return ApiResponse<IndicatorEntryResponse>.Fail("لا يمكن تنفيذ العملية في الحالة الحالية");

        // mutate
        entry.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "IndicatorEntry", id, "Do_Something",
            newValues: JsonSerializer.Serialize(new { … }));

        await _notification.CreateNotificationAsync(entry.EnteredBy, NotificationType.Workflow_Change, …);

        return ApiResponse<IndicatorEntryResponse>.Ok(MapToResponse(entry), "تمت العملية بنجاح");
    }
}
```

The shape, in order: **load with guards → validate state → mutate → save → audit →
notify → return**.

- Constructor injection only; fields are `private readonly`, `_camelCase`.
- Every public method is `async` and returns `ApiResponse` or `ApiResponse<T>`.
- **Expected failures return `Fail`, they do not throw.** Exceptions are for the
  unexpected, and `GlobalExceptionMiddleware` turns those into 500s.
- Messages are Arabic and written for the person reading them.
- Every state change is audited.
- Read-only queries use `.AsNoTracking()`.
- Soft-deleted rows are filtered explicitly — there is no global filter (finding **C5**).

### Controller pattern

```csharp
[ApiController]
[Route("api/v1/indicator-entries")]
[Authorize]
public class IndicatorEntriesController : ControllerBase
{
    private readonly IIndicatorEntryService _entryService;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int UserEntityId => int.TryParse(User.FindFirstValue("EntityId"), out var eid) ? eid : 0;

    [HttpPost]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.EntityAdmin},{Roles.DataEntryUser}")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateIndicatorEntryRequest request)
    {
        var result = await _entryService.CreateEntryAsync(request, UserId, UserEntityId);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetEntry), new { id = result.Data!.Id }, result);
    }
}
```

- Controllers are thin: read claims, call one service method, map success to a status
  code. No business logic, no `DbContext`.
- `[Authorize]` at class level; role attributes on actions.
- **Roles come from `Roles.*` constants, never string literals.**
- **`EntityId` comes from the JWT claim, never from the request body.** A client must not
  be able to name the entity it is writing for.

Two controllers break the "no `DbContext`" rule — `AttachmentsController` and
`AuditLogsController` (finding **A2**). Do not add a third.

### Validation

FluentValidation, assembly-scanned from `Application`:

```csharp
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

Validators handle *shape* — required, length, format. Services handle *business rules* —
"this entity is deactivated", "this transition is illegal". Do not mix them.

### Mapping

Mapster is referenced and configured but **the services map by hand** with static
`MapToResponse` methods. That is finding **C3**: pick one. Until then, follow the file
you are editing — a hand-written mapper is explicit and greppable, which is not the worst
default.

### Dates and numbers

- `DateTime.UtcNow` everywhere. Never `DateTime.Now`.
- `DateOnly` for calendar dates (period boundaries, due dates); `DateTime` for instants.
- `decimal` for all measures, mapped `HasPrecision(18, 4)`. Never `double`.

### Nullability

`<Nullable>enable</Nullable>` in every project. Required navigations are declared
`= null!`; optional ones are `?`. Do not silence a warning with `!` unless you can say
why the value cannot be null.

## Frontend

### Structure

- Pages in `src/pages/`, one per route.
- Reusable UI in `src/components/ui/`, form inputs in `src/components/form/`.
- API calls in `src/services/<area>Service.ts` — **components never call `axios`
  directly**.
- Shared types next to the service that returns them.

### Components

```tsx
export default function IndicatorManagement() {
  const [items, setItems] = useState<Indicator[]>([]);
  const [loading, setLoading] = useState(true);
  const { isOpen, openModal, closeModal } = useModal();

  useEffect(() => { void load(); }, []);

  async function load() {
    setLoading(true);
    try {
      const { data } = await indicatorService.getIndicators();
      if (data.success && data.data) setItems(data.data.items);
    } finally {
      setLoading(false);
    }
  }
  …
}
```

- Function components, `export default` for pages, named exports for shared components.
- Always handle the `loading` and empty states — a table that renders nothing while
  fetching reads as broken.
- Always check `data.success` before touching `data.data`; the envelope reports business
  failures with HTTP 200 in some paths.

### Styling

- Tailwind utilities inline; no separate CSS files.
- **Logical properties** (`ps-*`, `pe-*`, `ms-*`, `me-*`, `start-*`, `end-*`) so RTL works
  without re-mirroring. Avoid `pl-*` / `pr-*`.
- Dark mode with `dark:` variants on anything that renders a colour.
- Reuse `components/ui/` before writing new markup.

### Copy

Arabic, hardcoded in the component. No i18n framework. Match the tone of neighbouring
screens.

## Cross-cutting

### Adding a new feature end to end

1. `Domain` — entity and/or enum, if the model changes.
2. `Infrastructure/Data` — Fluent API mapping in `IndicatorsDbContext`, then
   `dotnet ef migrations add <Name>`.
3. `Contracts` — request and response DTOs.
4. `Application` — service interface, plus a validator for the request.
5. `Infrastructure/Services` — the implementation. (Ideally `Application`; see
   [02-architecture.md](02-architecture.md#known-deviation-business-logic-lives-in-infrastructure).)
6. `Api/Extensions/ServiceCollectionExtensions` — register it.
7. `Api/Controllers` — the endpoint, with role attributes.
8. `tests/` — cover the behaviour, including who may *not* do it.
9. `frontend/src/services/` — the typed client function.
10. `frontend/src/pages/` — the screen; route it in `App.tsx` with `allowedRoles`.
11. `Docs/` — update the affected documents. Not optional.

### Commits

Conventional Commits:

```
feat(entries): enforce closed reporting periods on entry creation
fix(auth): hash session tokens before storing them
docs(architecture): record the Infrastructure/Application deviation
refactor(services): move IndicatorEntryService into Application
test(authz): assert cross-entity entry access is refused
chore(deps): upgrade Swashbuckle to 10.2.3
```

Say what changed and why, not which files you touched.

### Known inconsistencies

Documented so nobody spends an afternoon rediscovering them:

| # | Inconsistency | Write new code as |
|---|---|---|
| **C1** | Only 9 of 16 entities derive from `BaseEntity` | Derive from `BaseEntity` |
| **C2** | `Entity.Status`, `ReopenRequest.Status`, `PublicationHistory.Action` are magic strings | Use an enum |
| **C3** | Mapster is referenced; mapping is hand-written | Follow the file you are in |
| **C4** | `PeriodType` and `PublicationFrequency` are identical | Do not add a third |
| **C5** | No global soft-delete query filter | Always filter `IsDeleted` explicitly |
| **C6** | Inline request classes at the bottom of `DraftsController` and `PublicationController` | Put DTOs in `Contracts` |

Full detail in [13-review-findings.md](13-review-findings.md).
