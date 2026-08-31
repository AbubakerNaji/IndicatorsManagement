# IndicatorsManagement — Documentation

> نظام إدارة المؤشرات — Ministry of Economy and Trade, Libya
> Full-stack indicators collection, approval, and publication system.

This folder is the **single source of truth for how the system is meant to work**. The
code is the source of truth for how it *does* work. Where the two disagree, that is a
bug in one of them — see [13-review-findings.md](13-review-findings.md) for the ones
already known.

---

## Start here

**Human, first day on the project** → [01-overview.md](01-overview.md) →
[02-architecture.md](02-architecture.md) → [09-development-setup.md](09-development-setup.md)

**AI agent picking up a task** → [14-ai-agent-guide.md](14-ai-agent-guide.md) **first**.
It tells you what to read, what to never do, and how work is proposed and applied
through OpenSpec.

**Just need to run it** → [09-development-setup.md](09-development-setup.md)

---

## Map

| # | Document | Answers |
|---|----------|---------|
| 01 | [Overview](01-overview.md) | What is this system, who uses it, what problem does it solve, what do the Arabic domain terms mean |
| 02 | [Architecture](02-architecture.md) | How the five projects fit together, the dependency rule, where each kind of code belongs, and the one known deviation |
| 03 | [Domain model](03-domain-model.md) | Every entity, every relationship, every enum, and the invariants that hold them together |
| 04 | [Workflows](04-workflows.md) | The approval state machine, publication, obligations, drafts, notifications |
| 05 | [API reference](05-api-reference.md) | Every endpoint, its roles, its request and its response |
| 06 | [Frontend](06-frontend.md) | React app structure, routing, state, services, RTL |
| 07 | [Database](07-database.md) | Physical schema, naming, indexes, migrations, seeding |
| 08 | [Security](08-security.md) | Authentication, the role matrix, entity scoping, secrets handling, audit |
| 09 | [Development setup](09-development-setup.md) | Get a local database and both apps running |
| 10 | [Deployment](10-deployment.md) | Docker, environment variables, health, operational runbook |
| 11 | [Testing](11-testing.md) | What is tested, how to run it, how to add tests |
| 12 | [Conventions](12-conventions.md) | Naming, patterns, and the rules that keep the codebase coherent |
| 13 | [Review findings](13-review-findings.md) | The full audit: what is wrong, how bad, and what to do about it |
| 14 | [AI agent guide](14-ai-agent-guide.md) | How an autonomous agent should work in this repository |

**[adr/](adr/)** — Architecture Decision Records: *why* things are the way they are.
**[reference/](reference/)** — the ministry's original Arabic indicators guide, the
source document behind all 120 seeded indicators.

---

## The system in one paragraph

Fifteen government entities each owe the Ministry a set of statistical indicators on a
recurring schedule. A data-entry user records a value for one indicator, one entity, one
reporting period. That entry travels through a two-level approval chain — the entity
reviews and approves it, then the Ministry gives final approval. Approved data is not
automatically public: a separate publication step controls what the Viewer role can see.
Everything is versioned, audited, and announced by notification. The whole UI is
Arabic-first and right-to-left.

---

## Conventions used in these docs

- **Verified** facts were read out of the code at the time of writing.
- Paths are repository-relative and clickable: [Program.cs](../src/IndicatorsManagement.Api/Program.cs).
- Arabic terms appear with their English gloss on first use in each document.
- Code snippets are illustrative; the file path above them is authoritative.

## Keeping this current

Documentation that drifts is worse than none, because it is trusted. So:

1. Any change to the domain model, the workflow, the role matrix, or the API surface
   **must** update the corresponding document in the same change.
2. `openspec/config.yaml` carries the short briefing given to AI agents. When these docs
   change materially, update that briefing too.
3. [13-review-findings.md](13-review-findings.md) is a living backlog. Close findings
   there when they are fixed; do not delete them silently.
