# ADR-0001 — Record architecture decisions

**Status:** Accepted · **Date:** 2026-08-22

## Context

The system was built across two planning cycles (Phase 1 and V2.1) under a previous
tooling setup whose specification documents were removed during the 2026-08-22 review.
Those documents recorded *plans*; they did not reliably record *decisions*, and the
distinction matters — a plan says what someone intended to build, a decision says why one
option beat another.

Several choices in this codebase look wrong until you know the reason. Enum members named
`Approved_By_Entity` violate C# naming. A stateless JWT is checked against a database
table on every request. Publication status duplicates something approval already seems to
express. Without a written rationale each of these is a standing invitation for someone
to "clean it up" and break a real requirement.

The project is also worked on by AI agents, which are especially prone to normalising
unusual-looking code toward convention.

## Decision

Record significant architecture decisions as short Markdown files in `Docs/adr/`,
numbered sequentially, following the structure Michael Nygard proposed: Context,
Decision, Consequences, Alternatives considered.

A decision qualifies when it is expensive to reverse, when it will look like a mistake to
someone without the context, or when it was genuinely contested.

Accepted records are never rewritten. A decision that changes gets a new record; the old
one is marked Superseded and points forward.

## Consequences

**Good.** The reasoning survives the people. Reviewers can challenge the argument rather
than the syntax. Agents get an explicit "this is deliberate" signal. Superseded records
preserve the history of what was believed and when.

**Bad.** Records rot if decisions change without one being written. Discipline is
required, and there is no mechanism to enforce it beyond review.

**Neutral.** ADRs describe decisions, not the current state of the system. That is
`Docs/01`–`Docs/13`'s job. Do not use an ADR as a reference document.

## Alternatives considered

**Rely on git history.** Commit messages record what changed, rarely the alternatives
that were rejected. Archaeology across a squashed history is slow and usually inconclusive.

**Keep the decisions inside the architecture document.** They get buried, and the
document becomes a mix of "how it is" and "why it is", which ages badly — the first part
changes constantly and the second should not change at all.

**A wiki.** Drifts from the code, requires separate access, and cannot be reviewed in the
same pull request as the change it justifies.
