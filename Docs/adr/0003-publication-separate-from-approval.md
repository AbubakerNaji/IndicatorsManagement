# ADR-0003 — Publication is independent of approval

**Status:** Accepted · **Date:** 2026-03-31 (recorded retrospectively 2026-08-22)

## Context

`Final_Approved` answers "is this figure correct?". It does not answer "may the public
see it?". Those are different questions, decided by different people, at different times:

- A figure may be approved weeks before the Ministry intends to announce it, because
  announcements are coordinated across indicators or tied to a publication calendar.
- A published figure may need withdrawing — a correction is coming, or the underlying
  source is disputed — **without** retracting the statement that it was correctly
  approved at the time.
- The `Viewer` role exists specifically to see public data. It needs a flag that means
  "public", not one that means "someone signed off".

Folding disclosure into `WorkflowState` would require states like
`Final_Approved_Unpublished` and `Final_Approved_Published`, and an unpublish transition
that moves *backwards* through an approval chain it has nothing to do with.

## Decision

Model publication as an independent axis: `IndicatorEntry.PublicationStatus`, an enum of
`Unpublished` (default) and `Published`, changed only by `Super_Admin` and
`Ministry_Admin` through dedicated endpoints.

```
WorkflowState:      Draft → Under_Review → Approved_By_Entity → Final_Approved
PublicationStatus:  Unpublished  ⇄  Published        (independent, reversible)
```

Every transition appends a row to `publication_history` recording the actor, the
timestamp, and an optional reason. `GetEntriesAsync` takes a `publishedOnly` flag that the
controller sets when the caller holds the `Viewer` role.

## Consequences

**Good.** The Ministry controls timing without touching approval. Unpublishing is a normal,
reversible operation rather than a workflow regression. `Viewer` access reduces to a single
predicate. Approval history and disclosure history are separately auditable, which is what
a compliance question actually asks about. Bulk publish and unpublish are natural, because
publication is one field.

**Bad.** Two orthogonal states mean two things to reason about, and the UI must show both
without implying one causes the other. There is a genuine risk of a `Viewer` seeing
approved-but-not-yet-announced data if `publishedOnly` is ever missed — and today
`GET /indicator-entries/{id}` does not apply it at all (finding **S5**).

**Bad.** Nothing enforces that only `Final_Approved` entries may be published. The
endpoint's role check is the only gate; a `Ministry_Admin` could in principle publish a
draft. Worth adding an explicit guard.

**Neutral.** `PublicationStatus` was added to the schema during Phase 1 for forward
compatibility and only wired up in V2.1, so early rows default to `Unpublished` — which is
the safe default.

## Alternatives considered

**Extra workflow states.** `Final_Approved_Published` and friends. Rejected: the state
count multiplies, unpublish becomes a backward transition through an approval chain, and
every transition guard has to know about disclosure.

**A separate `published_entries` table.** Cleaner conceptually, but every read path would
need a join or a union, and "is this published?" becomes an existence check rather than a
column read.

**Publish everything on final approval.** Simplest, and wrong: it removes the Ministry's
control over announcement timing, which was an explicit requirement.
