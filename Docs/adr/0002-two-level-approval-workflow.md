# ADR-0002 — Two-level approval with terminal rejection

**Status:** Accepted · **Date:** 2026-03-30 (recorded retrospectively 2026-08-22)

## Context

Fourteen government entities report 120 indicators to the Ministry of Economy and Trade.
Two distinct kinds of error need catching, and they are caught by different people:

- **Local errors** — a transcription slip, the wrong unit, a figure that contradicts what
  the entity itself knows. The entity is the only party able to see these.
- **Cross-cutting errors** — an implausible figure relative to other entities or to
  history, or an inconsistency the Ministry can see precisely because it sees everything.

A single approval step forces a choice between the two: either the Ministry approves
everything (and becomes a bottleneck with no local knowledge), or the entity approves
everything (and nothing is checked across entities).

There is also a second distinction that a single "rejected" state cannot express. "This
figure has a typo, fix it" and "this should not have been submitted" are different
outcomes with different consequences for the reporting obligation.

## Decision

Two sequential approval levels:

```
Draft → Under_Review → Approved_By_Entity → Final_Approved
```

Entity-level approval (`Reviewer` or `Entity_Admin`) precedes Ministry-level approval
(`Ministry_Admin`). Each stamps its own actor and timestamp on the entry, so the record
shows who signed off at which level.

Two distinct backward transitions:

- **`Returned_For_Modification`** — recoverable. The entry becomes editable again by its
  author and re-enters the same flow on resubmission. A reason is mandatory.
- **`Rejected`** — **terminal**. The entry is never edited again. A reason is mandatory.

Rejection is excluded from the filtered unique index on
`(IndicatorId, EntityId, ReportingPeriodId)`, so an entity may create a fresh entry for
the same period after a rejection.

## Consequences

**Good.** Each error class is caught by the party equipped to catch it. Accountability is
explicit: four columns record who entered, who reviewed, who approved at entity level, and
who approved at ministry level. A rejected entry stays as a permanent record of the
attempt rather than being edited into something that never happened — which matters for a
system whose purpose includes an audit trail. Because `Rejected` is excluded from the
index, terminal rejection does not deadlock the reporting period.

**Bad.** Two levels mean latency: a correct figure still waits on two people. Rejection
is unforgiving — a reviewer who rejects when they meant to return has forced the entity to
re-enter the data from scratch. The UI should make that difference obvious.

**Bad.** Nothing prevents a user from approving their own entry (finding **S4**). An
`Entity_Admin` may both create and approve at entity level. Only the Ministry step is
guaranteed to involve a second person, and only because the role is distinct.

**Neutral.** Amending a `Final_Approved` entry requires a reopen mechanism. The
`reopen_requests` table exists for this; the implementation does not (finding **B5**).
Until then, final approval is effectively irreversible through the application.

## Alternatives considered

**Single approval by the Ministry.** Simpler, and it centralises quality control — but it
discards the entity's local knowledge and makes the Ministry a bottleneck across 120
indicators and 14 entities.

**Single approval by the entity.** Fast, and it puts the decision closest to the data —
but nothing then catches cross-entity inconsistency, which is the whole reason for a
central register.

**Configurable approval chains per indicator.** `Indicator.RequiresReview` hints at this.
Rejected as premature: it multiplies the states to test and the paths to explain, for a
flexibility nobody had asked for.

**Editable rejection instead of a terminal state.** Would have collapsed
`Returned_For_Modification` and `Rejected` into one state, at the cost of losing the
distinction between "fix this" and "this was wrong", and of allowing history to be
rewritten.
