## 1. Test harness

- [ ] 1.1 Add `public partial class Program { }` to the end of `Program.cs` so the test project can reference it
- [ ] 1.2 Add `Microsoft.AspNetCore.Mvc.Testing` to `IndicatorsManagement.Tests.csproj`
- [ ] 1.3 Create `AuthorizationTestFixture`: a `WebApplicationFactory<Program>` that swaps in a SQLite or in-memory store and seeds two entities, plus one user per role
- [ ] 1.4 Add a helper that mints a valid JWT for a given (userId, entityId, role) using the test host's signing key

## 2. Failing tests first

- [ ] 2.1 Write cross-entity tests for `GET`, `PUT`, `DELETE /indicator-entries/{id}` — assert `404` and no state change
- [ ] 2.2 Write cross-entity tests for the four workflow actions (`submit`, `approve-entity`, `reject`, `return`)
- [ ] 2.3 Write cross-entity tests for the three attachment endpoints, including a `Viewer` downloading an unpublished entry's attachment
- [ ] 2.4 Write cross-entity tests for `GET /dashboard/entity/{id}` and `GET`/`PUT /users/{id}`
- [ ] 2.5 Write positive tests: same-entity access succeeds, and `Ministry_Admin` reaches every entity
- [ ] 2.6 Write the indistinguishability test — a foreign id and a nonexistent id return identical status, `success`, and `message`
- [ ] 2.7 Write the null-`EntityId` test: a non-ministry user with no entity is refused
- [ ] 2.8 Run `dotnet test` and confirm the new tests **fail** for the right reason before writing any implementation

## 3. Caller context

- [ ] 3.1 Add `CallerContext` record to `Contracts` with `IsMinistryLevel` and `CanAccessEntity` (handling null `EntityId` explicitly)
- [ ] 3.2 Add a `ControllerBase` extension that builds a `CallerContext` from `User` claims
- [ ] 3.3 Unit-test `CanAccessEntity` across all seven roles and the null-entity case

## 4. Indicator entries

- [ ] 4.1 Add `CallerContext` to `IIndicatorEntryService` by-id method signatures
- [ ] 4.2 Add the ownership check to `GetEntryByIdAsync`, folding it into the existing not-found branch
- [ ] 4.3 Add the ownership check to `UpdateEntryAsync` and `SoftDeleteEntryAsync`
- [ ] 4.4 Add the ownership check to `SubmitEntryAsync`, `ApproveEntityLevelAsync`, `RejectEntryAsync`, `ReturnEntryAsync`
- [ ] 4.5 Update `IndicatorEntriesController` to pass the context; `dotnet build` and `dotnet test`

## 5. Attachments

- [ ] 5.1 Add `IAttachmentService` to `Application/Services/Interfaces` — upload, download, delete
- [ ] 5.2 Implement `AttachmentService` in `Infrastructure/Services`, moving the logic out of the controller unchanged apart from the added checks
- [ ] 5.3 Authorize every attachment operation through the parent `IndicatorEntry`
- [ ] 5.4 Add the `Viewer`-plus-unpublished rule to the download path
- [ ] 5.5 Reduce `AttachmentsController` to a thin controller with no `DbContext`; register the service; `dotnet build` and `dotnet test`

## 6. Dashboard and users

- [ ] 6.1 Add the ownership check to `GetEntityDashboardAsync`
- [ ] 6.2 Add the ownership check to `GetUserByIdAsync` and `UpdateUserAsync` for `Entity_Admin`
- [ ] 6.3 Update `DashboardController` and `UsersController`; `dotnet build` and `dotnet test`

## 7. Remove dead machinery

- [ ] 7.1 Delete `Api/Authorization/EntityAccessHandler.cs` and `EntityAccessRequirement.cs`
- [ ] 7.2 Remove the `EntityScoped` policy registration and `PolicyNames.EntityScoped`
- [ ] 7.3 Remove the now-unused `IAuthorizationHandler` registration; `dotnet build` and `dotnet test`

## 8. Verify

- [ ] 8.1 `dotnet build IndicatorsManagement.slnx` — zero warnings
- [ ] 8.2 `dotnet test` — all previously passing tests plus the new suite, all green
- [ ] 8.3 Grep the frontend for hardcoded ids and exercise the Ministry dashboard drill-down against a running API
- [ ] 8.4 Manual check: sign in as a `Data_Entry_User`, request another entity's entry id, confirm `404`

## 9. Documentation

- [ ] 9.1 `Docs/05-api-reference.md` — remove the ⚠️ markers and rewrite the closing authorization section
- [ ] 9.2 `Docs/08-security.md` — rewrite "three mechanisms, two in use"; remove the S5 table
- [ ] 9.3 `Docs/13-review-findings.md` — close **S5** and **T2**, note partial progress on **A2**
- [ ] 9.4 `Docs/11-testing.md` — record the new authorization suite in the coverage table
- [ ] 9.5 Add an ADR recording why authorization lives in the service layer rather than in a policy handler
