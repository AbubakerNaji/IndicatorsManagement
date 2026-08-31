# 01 — System Overview

## What this system is

**IndicatorsManagement** (نظام إدارة المؤشرات) is the Libyan Ministry of Economy and
Trade's central register for statistical indicators. Fourteen subordinate government
entities report **120 indicators** to the Ministry on recurring schedules. Before this
system, that reporting happened through spreadsheets and email; the system replaces that
with a single database, an enforced approval chain, and a complete audit trail.

## The problem it solves

Four problems, in the order they hurt:

1. **No single source of truth.** The same indicator arrived from different entities in
   different units, at different times, with no way to tell which copy was current.
2. **No accountability trail.** When a published figure turned out to be wrong, nobody
   could reconstruct who entered it, who approved it, or what it was before.
3. **No visibility into who owes what.** The Ministry could not see which entity was late
   on which indicator until it was already late.
4. **Approved is not the same as public.** Data can be internally approved long before it
   is ready to be published externally, and the two decisions have different owners.

## Who uses it

Seven roles, described fully in [08-security.md](08-security.md):

| Role | Arabic | What they do |
|------|--------|-------------|
| `Super_Admin` | مدير النظام | Everything. Manages entities, indicators, reporting periods, configuration. |
| `Ministry_Admin` | مسؤول الوزارة | Final approval, publication, cross-entity dashboards, master data. |
| `Entity_Admin` | مسؤول الجهة | Runs one entity: its users, its entries, first-level approval. |
| `Data_Entry_User` | مدخل بيانات | Records indicator values for their entity and submits them. |
| `Reviewer` | مراجع | Reviews submitted entries; approves, returns, or rejects. |
| `Auditor` | مدقق | Read-only access to the audit log. Changes nothing. |
| `Viewer` | مشاهد | Sees published data only. |

## The core objects

Read [03-domain-model.md](03-domain-model.md) for the full model. The five that matter
most:

- **Indicator** (مؤشر) — *what* is measured. Code, Arabic name, definition, calculation
  method, unit, data source, publication frequency. Example: `F001` — عدد المخابز المرخصة
  (number of licensed bakeries).
- **Entity** (جهة) — *who* reports. The Ministry plus fourteen children: bureaus,
  authorities, departments, funds, and one network.
- **Reporting period** (فترة إبلاغ) — *when*. January 2026, Q1 2026, H1 2026, Year 2026.
- **Indicator assignment** (تكليف) — the standing obligation that entity X reports
  indicator Y at frequency Z, starting on a date.
- **Indicator entry** (إدخال) — the actual datum: one indicator, one entity, one period,
  one value, plus its position in the approval workflow.

The invariant that shapes everything: **at most one active entry exists per
(indicator, entity, period)**. It is enforced in the database by a filtered unique index
that excludes soft-deleted and rejected rows — see [07-database.md](07-database.md).

## The lifecycle of a number

```
  Data_Entry_User records a value           → Draft
  submits it                                → Under_Review
  Reviewer / Entity_Admin approves          → Approved_By_Entity
  Ministry_Admin approves                   → Final_Approved
  Ministry_Admin publishes                  → PublicationStatus = Published
```

with two ways back:

```
  Reviewer returns it for correction        → Returned_For_Modification → (edit) → Under_Review
  Reviewer or Ministry rejects it           → Rejected  (terminal; frees the slot for a new entry)
```

Publication is a **separate axis**, not a workflow state. `Final_Approved` data stays
invisible to the Viewer role until someone publishes it, and can be unpublished again
without disturbing its approval status. Full detail in [04-workflows.md](04-workflows.md).

## What is in the box

**Backend** — .NET 10, five projects, ~90 C# files, 76 passing tests.
17 controllers, 16 services, 4 recurring background jobs, 4 middleware components,
5 EF Core migrations, 26 domain tables plus ASP.NET Identity's.

**Frontend** — React 19 + TypeScript + Vite + Tailwind CSS 4, ~130 files, 20 business
pages, Arabic-first RTL throughout.

**Seed data** — the system arrives populated: 7 roles, 15 entities, 120 indicators with
full Arabic metadata, 120 assignments, reporting periods for 2024–2026, and 6 system
configuration keys. The source document is
[reference/indicators-guide-tables.ar.md](reference/indicators-guide-tables.ar.md).

## Glossary (Arabic ↔ English)

| Arabic | English | Meaning here |
|--------|---------|--------------|
| مؤشر | Indicator | A measurable statistic with a definition and a unit |
| جهة | Entity | A government body that reports indicators |
| إدخال | Entry | One recorded value for one indicator/entity/period |
| فترة إبلاغ | Reporting period | The time window a value describes |
| تكليف | Assignment | Standing obligation of an entity to report an indicator |
| التزام تسليم | Submission obligation | One concrete due instance of an assignment |
| بُعد | Dimension | An optional breakdown axis (sector, country, facility type) |
| مسودة | Draft | Entry not yet submitted |
| قيد المراجعة | Under review | Submitted, awaiting entity approval |
| معتمد من الجهة | Approved by entity | Passed entity review, awaiting Ministry |
| اعتماد نهائي | Final approved | Ministry-approved |
| مرفوض | Rejected | Terminal rejection |
| مُعاد للتعديل | Returned for modification | Sent back to the author |
| منشور | Published | Visible to the Viewer role |
| قيمة مستهدفة | Target value | The planned figure for an indicator/year |
| طلب إعادة فتح | Reopen request | Request to amend an approved entry |
| سجل التدقيق | Audit log | Immutable record of every write operation |

## Where to go next

- How the code is organised → [02-architecture.md](02-architecture.md)
- What the data looks like → [03-domain-model.md](03-domain-model.md)
- How to run it → [09-development-setup.md](09-development-setup.md)
- What is currently wrong with it → [13-review-findings.md](13-review-findings.md)
