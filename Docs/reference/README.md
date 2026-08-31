# Reference material

Source documents. **Do not edit these** — they are the ministry's originals, kept as the
authority behind the seeded data.

| File | What it is |
|---|---|
| [indicators-guide-tables.ar.md](indicators-guide-tables.ar.md) | دليل مؤشرات وزارة الاقتصاد والتجارة — جداول واضحة. The tabular edition, and the direct source for `SeedData.cs` |
| [indicators-guide-v1.ar.md](indicators-guide-v1.ar.md) | The earlier V1 edition of the same guide, kept for comparison |

Both are Arabic. Together they define the 120 indicators and the 14 reporting entities:
each indicator's code, name, definition (تعريف), calculation method (آلية الاحتساب), unit
(وحدة القياس), data source (مصدر البيانات), objective (الهدف), and publication frequency
(دورية النشر).

## Relationship to the code

```
Docs/reference/indicators-guide-tables.ar.md      the ministry's document
        │  transcribed by hand
        ▼
src/IndicatorsManagement.Infrastructure/Data/SeedData.cs        777 lines
        │  GetEntitiesWithIndicators()
        ▼
DatabaseSeeder.SeedAsync()                        idempotent, on every startup
        ▼
entities (15) · indicators (120) · indicator_assignments (120)
```

`tests/IndicatorsManagement.Tests/SeedDataTests.cs` asserts the counts, code uniqueness,
and required Arabic fields — so a transcription slip fails the build rather than reaching
the database.

## Changing an indicator

If the ministry revises the guide:

1. Replace the file here with the new edition.
2. Update the matching entries in `SeedData.cs`.
3. Update `SeedDataTests.cs` if counts changed.
4. **Seeding only fills empty tables** — it will not update existing rows. A change to an
   already-deployed indicator needs a data migration, not a seed edit.

See [../07-database.md](../07-database.md#seeding).
