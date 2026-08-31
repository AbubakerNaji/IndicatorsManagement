# ADR-0004 — Persist enums as strings

**Status:** Accepted · **Date:** 2026-03-30 (recorded retrospectively 2026-08-22)

## Context

EF Core stores a CLR enum as its underlying integer by default. That is compact and fast,
and it has one property that is dangerous for a long-lived compliance system: **the
meaning of stored data depends on the order of members in a source file.**

Insert a new member in the middle of `WorkflowState`, or reorder them alphabetically
during a tidy-up, and every existing row silently changes meaning. Nothing fails. No
constraint is violated. `Approved_By_Entity` becomes `Final_Approved` across historical
records, and the first sign of trouble is a report that does not reconcile.

This system also carries a hard requirement that its data be auditable. Ministry staff
and auditors query the database directly. A column reading `3` requires a lookup into
source code that may have changed since the row was written.

There is a third, concrete constraint. The core invariant is enforced by a filtered
unique index:

```sql
WHERE [IsDeleted] = 0 AND [WorkflowState] != 'Rejected'
```

That filter is written in SQL. With an integer column it would read `!= 3`, which is
correct only for as long as nobody touches the enum.

## Decision

Persist every enum as its string name, configured explicitly per property:

```csharp
e.Property(x => x.WorkflowState).HasConversion<string>().HasMaxLength(30);
```

Applied to `WorkflowState`, `PublicationStatus`, `EntityType`, `PeriodType`,
`PublicationFrequency`, `DimensionType`, `ObligationStatus`, `ValidationRuleType`, and
`NotificationType`. Every one gets an explicit `HasMaxLength`.

Enum members use `Pascal_Snake` names — `Approved_By_Entity`, `Semi_Annual`,
`Not_Started` — so the C# identifier and the stored string are the same token, greppable
in either direction.

## Consequences

**Good.** Reordering or inserting enum members is safe. The database is legible without
the source: `SELECT WorkflowState FROM indicator_entries` returns words. The filtered
index expresses the business rule in the business's own vocabulary. Audit exports need no
translation table.

**Bad.** More storage — up to 30 bytes instead of 4 — and slightly slower comparisons on
large scans. At this system's scale (hundreds of thousands of rows, not billions) this is
not measurable.

**Bad, and important.** **Renaming an enum member is now a breaking schema change.** The
CLR name and the stored string are the same thing, so a rename orphans every existing row
and silently breaks the index filter. Any rename needs a data migration
(`UPDATE … SET WorkflowState = 'New' WHERE WorkflowState = 'Old'`) *and* an updated index
filter, in the same migration.

**Neutral.** The `Pascal_Snake` naming violates C# convention and will look like a mistake
to anyone who has not read this record. That is precisely why the record exists. Do not
"correct" it.

## Alternatives considered

**Integer storage (the default).** Compact and fast. Rejected: the coupling between file
order and stored meaning is an unacceptable risk for data that must remain interpretable
for years, and it would push the index filter into magic numbers.

**A lookup table per enum with foreign keys.** Fully normalised and referentially safe.
Rejected as disproportionate: nine lookup tables, nine joins, nine seeding steps, and
migrations for what is fundamentally a closed set defined in code.

**`[EnumMember]`-style explicit integer values.** Pinning each member to a literal
(`Draft = 1`) keeps integers safe against reordering. It solves the corruption risk but
none of the legibility problem, and the index filter would still read `!= 5`.
