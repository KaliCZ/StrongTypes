# Skill draft strategy

Goal: ship `.claude/skills/strongtypes/SKILL.md` — a single SKILL.md that
lets Claude Code / Codex users pick up the Kalicz.StrongTypes package fast
without re-reading the whole readme.

## Approach

Work in this `.skill-draft/` folder one section per file. Commit after every
section so a new session can resume mid-flight. When all sections are done,
concatenate them into `.claude/skills/strongtypes/SKILL.md` and delete
`.skill-draft/`.

## Sections (each becomes a commit)

1. Overview — what StrongTypes is, when to reach for it.
2. Design philosophy — the "nullable T vs Maybe<T> vs Result<T, TError>"
   decision tree. This is the biggest value-add over the readme.
3. Implicit operators — first-class section. `return value;` beats
   `Result.Success(value)` almost everywhere.
4. NonEmptyString — factories + full passthrough surface.
5. Numeric wrappers — Positive / NonNegative / Negative / NonPositive.
6. NonEmptyEnumerable — creation, invariant-preserving LINQ, total aggregates.
7. Parsing helpers — Digit, EnumExtensions, string.AsX/ToX.
8. Map helpers — T?.Map, bool.MapTrue/MapFalse.
9. Maybe<T> — construction, unwrap patterns, monadic API, JSON shape.
10. Result<T, TError> — construction via implicit ops, access, composition.
11. Collection extensions — IEnumerable helpers and NonEmptyEnumerable LINQ.
12. Integrations — JSON, EF Core, FsCheck.
13. Anti-patterns — the "do not do this" list (overusing Result, wrapping
    plain updates in Maybe, reinventing factories the library already ships).

## Final assembly

The final SKILL.md gets a short YAML front-matter (`name`, `description`)
so Claude Code can discover it. The rest is just markdown — no code runs.
