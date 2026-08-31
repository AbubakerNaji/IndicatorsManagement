# Architecture Decision Records

Short documents capturing decisions that shaped the system, and the reasoning behind
them. They answer "why is it like this?" so that nobody has to reconstruct the argument
from the code — or worse, "fix" something that was deliberate.

## Index

| # | Decision | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-two-level-approval-workflow.md) | Two-level approval with terminal rejection | Accepted |
| [0003](0003-publication-separate-from-approval.md) | Publication is independent of approval | Accepted |
| [0004](0004-enums-as-strings-in-database.md) | Persist enums as strings | Accepted |
| [0005](0005-server-side-session-validation.md) | Back stateless JWTs with a session table | Accepted |
| [0006](0006-no-secrets-in-committed-configuration.md) | No secrets in committed configuration | Accepted |
| [0007](0007-services-in-infrastructure.md) | Services in Infrastructure | **Superseded** — see the record |

## Writing one

Copy the shape of an existing record: **Context** (the forces), **Decision** (what was
chosen, in the active voice), **Consequences** (good and bad, honestly),
**Alternatives considered** (and why they lost).

Number sequentially. Never rewrite an accepted record — supersede it with a new one and
update the old record's status to point forward. The value of an ADR is that it says what
was believed *at the time*.

Add a record when a decision is expensive to reverse, when it will look wrong to someone
who lacks the context, or when it was genuinely contested.
