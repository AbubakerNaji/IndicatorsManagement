# ADR-0007 — Service implementations in Infrastructure

**Status:** ⚠️ **Superseded in intent** — the current state is documented here, and
finding **A1** proposes changing it. Recorded 2026-08-22.

## Context

This record documents a decision that was made implicitly rather than deliberately. It is
written down because the arrangement contradicts the Clean Architecture the project
otherwise follows, and because an agent or developer who notices the contradiction needs
to know it has already been seen.

The project is organised into five layers with dependencies pointing inward: Domain,
Contracts, Application, Infrastructure, Api. `Application` declares service interfaces
(`IIndicatorEntryService`, `IPublicationService`, and fourteen more). Under the Dependency
Rule, the implementations of those interfaces — the use cases — belong in `Application`,
with `Infrastructure` supplying only the persistence mechanism behind an abstraction.

All sixteen implementations are in `Infrastructure/Services/` instead.

The apparent reason is convenience: each service takes `IndicatorsDbContext` directly in
its constructor and composes EF Core queries inline. Putting the implementations where the
`DbContext` already lives avoided defining a persistence abstraction.

## Decision — as it stands today

Service implementations live in `IndicatorsManagement.Infrastructure/Services/` and depend
on `IndicatorsDbContext` concretely. `Infrastructure` references `Application` in order to
implement its interfaces. The Api composition root binds each interface to its
Infrastructure implementation.

## Consequences

**Good — the honest case.** There is exactly one layer of indirection between a use case
and the database, so a query and the rule it serves are readable side by side. No
repository abstraction had to be designed, and no leaky `IQueryable`-returning interface
had to be maintained. For a team of this size delivering to a deadline, this is a real
saving.

**Bad.** The approval state machine — the most valuable logic in the system — lives in the
layer whose responsibility is talking to SQL Server. `IndicatorEntryService` holds both
"an entry may only be submitted from `Draft` or `Returned_For_Modification`" and the
`Include(...).ThenInclude(...)` chain that loads it.

**Bad, and this is the one that bites.** Business rules cannot be tested without an EF Core
context. The test suite works around this with the in-memory provider, which enforces no
unique index, no foreign key, and no filtered index. The system's core invariant is
implemented twice — in the service and as a filtered unique index — and the tests exercise
only the service half (finding **T1**).

**Bad.** The rules are not reusable outside a SQL Server deployment, and nothing
structurally prevents a future query optimisation from quietly altering a rule.

**Neutral.** `Application` still owns the interfaces, so controllers depend on
abstractions and the Api layer is correctly decoupled. The violation is confined to one
boundary rather than spread through the codebase — which is why it is fixable as a single,
mechanical change.

## The intended end state

Finding **A1** in [../13-review-findings.md](../13-review-findings.md):

1. Move `Infrastructure/Services/*.cs` into `Application/Services/`.
2. Define a persistence abstraction in `Application` — an `IIndicatorsDbContext` exposing
   the `DbSet`s is the smallest change that works; repositories are the fuller version.
3. Have `Infrastructure` provide the EF Core implementation and reverse the project
   reference direction.
4. Update DI registration and the test project.

This is wide but mechanical, and it has no user-visible effect — which is exactly why it
should be proposed as its own OpenSpec change rather than folded into feature work.

**Until then:** put new business rules in a service, keep them free of EF Core types where
you can, and **do not add a new reason for `Application` to depend on `Infrastructure`.**

## Alternatives considered

**Leave it permanently and document it.** Defensible — plenty of successful systems put
services next to the data access. Rejected as the final answer specifically because of the
testing consequence: without the move, there is no way to test the rules against anything
other than a fake database.

**Full repository pattern.** One repository per aggregate, no `DbContext` outside
`Infrastructure`. The textbook answer, and the most work. Worth considering during the A1
change, but the minimal `IIndicatorsDbContext` abstraction delivers most of the benefit for
a fraction of the churn.

**Move only the services with meaningful business rules** — `IndicatorEntryService`,
`PublicationService`, `AuthenticationService` — and leave the CRUD ones. Rejected: a
partial boundary is harder to explain and enforce than either extreme, and "which side is
this service on?" becomes a question asked forever.
