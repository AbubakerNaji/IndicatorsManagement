# 11 — Testing

## Current state

One test project, [tests/IndicatorsManagement.Tests/](../tests/IndicatorsManagement.Tests/).
**76 tests, all passing**, roughly one second to run.

```bash
dotnet test tests/IndicatorsManagement.Tests/
```

Stack: xUnit 2.9 · FluentAssertions 8.3 · EF Core InMemory 10.0.5 · coverlet.

| File | Tests | Covers |
|---|---:|---|
| `WorkflowExtendedTests.cs` | 12 | State transitions, guards, illegal moves |
| `IndicatorEntryServiceTests.cs` | 11 | Create, update, soft delete, duplicate prevention |
| `SeedDataTests.cs` | 10 | 15 entities, 120 indicators, unique codes, required fields |
| `PublicationServiceTests.cs` | 9 | Publish, unpublish, bulk, history |
| `IndicatorServiceExtendedTests.cs` | 5 | Pagination, code immutability, dimensions |
| `AssignmentServiceTests.cs` | 4 | Assignment creation and obligation generation |
| `NotificationServiceTests.cs` | 4 | Creation, read, read-all |
| `ValidationRuleServiceTests.cs` | 3 | Rule CRUD and enforcement |
| `EntityServiceTests.cs` | 2 | Duplicate names, deactivation blocking entries |
| `IndicatorServiceTests.cs` | 2 | Basic CRUD |

`TestDbContextFactory` builds an in-memory context per test — each with its own database
name for isolation — and `SeedBasicData` provides one user, two entities, one indicator,
one period, and one assignment.

## What is actually verified

Good coverage of the **service-layer state machine**: that `Draft` can be submitted, that
`Under_Review` cannot be edited, that rejection needs a reason, that a duplicate entry for
the same triple is refused, that a deactivated entity blocks creation.

That is the most valuable thing to test, and it is tested.

## What is not covered

### The in-memory provider does not enforce the schema

This is the important caveat. `Microsoft.EntityFrameworkCore.InMemory` is not a
relational database:

- **Unique indexes are not enforced** — including the filtered unique index that is the
  system's core invariant. `IndicatorEntryServiceTests` proves the *service* rejects
  duplicates; nothing proves the *database* would.
- Filtered indexes, FK constraints, cascade behaviour, `decimal(18,4)` precision, and raw
  SQL are all unverified.

So the tests confirm the application logic and say nothing about the schema. Closing this
means running the same tests against real SQL Server via Testcontainers — finding **T1**.

### Not tested at all

| Area | Status |
|---|---|
| Controllers / HTTP pipeline | ❌ No `WebApplicationFactory` tests |
| Authentication and JWT issuance | ❌ |
| **Authorization — roles and entity scoping** | ❌ Which is why **S5** went unnoticed |
| Middleware (session, audit, correlation, exceptions) | ❌ |
| Hangfire jobs | ❌ |
| FluentValidation validators | ❌ |
| Migrations (apply and roll back) | ❌ |
| `AttachmentsController` and `AuditLogsController` | ❌ Not reachable from service tests |
| Anything in the frontend | ❌ No test framework installed |

**No test would have caught S5**, the object-level authorization gap, because nothing
exercises "user from entity A requests entity B's data". That is the single most valuable
test to add.

## Conventions

`MethodUnderTest_Condition_ExpectedOutcome`:

```csharp
[Fact]
public async Task CreateEntry_DuplicateActiveEntry_ShouldFail()
{
    var db = TestDbContextFactory.Create(nameof(CreateEntry_DuplicateActiveEntry_ShouldFail));
    await TestDbContextFactory.SeedBasicData(db);
    var service = new IndicatorEntryService(db, auditStub, notificationStub);

    await service.CreateEntryAsync(request, userId: 1, userEntityId: 2);
    var second = await service.CreateEntryAsync(request, userId: 1, userEntityId: 2);

    second.Success.Should().BeFalse();
    second.Message.Should().Contain("يوجد إدخال فعّال");
}
```

Rules:

- **Use `nameof(TestMethod)` as the in-memory database name.** Sharing a name across
  tests shares state and produces order-dependent failures.
- Assert with FluentAssertions.
- Assert on `Success` plus a message fragment — messages are Arabic and user-facing, so
  match a distinctive substring rather than the whole string.
- One behaviour per test.

## Priorities for new tests

In descending order of value:

1. **Authorization** — for every by-id endpoint, assert that a user from another entity is
   refused. This is the gap that hid **S5**.
2. **Integration tests over real SQL Server** — Testcontainers plus
   `WebApplicationFactory`, so the filtered unique index, FK behaviour, and decimal
   precision are actually exercised (**T1**).
3. **Validators** — `FluentValidation.TestHelper` makes these cheap and they are entirely
   untested.
4. **Background jobs** — due-date and overdue selection logic against a fixed clock.
5. **Frontend** — Vitest + React Testing Library, starting with `ProtectedRoute`,
   `authSlice`, and the entry wizard.

### Suggested integration test setup

```csharp
// Testcontainers.MsSql
var sql = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest").Build();
await sql.StartAsync();

var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    b.UseSetting("ConnectionStrings:DefaultConnection", sql.GetConnectionString()));
```

`Program` needs to be visible to the test project — add
`<InternalsVisibleTo Include="IndicatorsManagement.Tests" />` or a
`public partial class Program { }` at the end of `Program.cs`.

## Coverage

coverlet is referenced but no threshold is set and no report is produced:

```bash
dotnet test tests/IndicatorsManagement.Tests/ --collect:"XPlat Code Coverage"
```

Once CI exists (**O4**), publish the report and set a floor that ratchets upward rather
than a number nobody meets.

## Definition of done for a change

- [ ] `dotnet build IndicatorsManagement.slnx` — zero warnings
- [ ] `dotnet test` — all green
- [ ] New behaviour has a test; a bug fix has a test that failed before it
- [ ] `cd frontend && npm run build && npm run lint` if the frontend changed
- [ ] Affected files under `Docs/` updated
