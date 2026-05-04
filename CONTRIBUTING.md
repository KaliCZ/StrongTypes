# Contributing to StrongTypes

Thanks for your interest in contributing. This document covers how to
report issues, propose changes, and — most importantly — how to make sure
new types are properly tested.

## Reporting bugs

Open a GitHub issue. Include the package version, the target framework,
and a minimal repro (a failing test is ideal).

## Requesting a feature

Open a GitHub issue describing the use case before sending a PR for
anything non-trivial. That keeps the discussion in one place and avoids
wasted work if the design needs to change.

## Pull requests

- Fork and branch off `main`.
- Keep the change focused. One concern per PR.
- Run `dotnet build` and `dotnet test` locally before pushing.
- Match the conventions in [`CLAUDE.md`](CLAUDE.md) — it covers folder
  layout, the `TryCreate` / `Create` pattern, comment style, and line
  wrapping rules. Those rules apply to every contributor.
- PRs are squash-merged.

## Testing

Every new type must ship with tests. All testing rules — which projects
to touch, what to cover, how to write each kind of test — live in
[`testing.md`](testing.md). Read it before writing tests or production
code that will need them.

## Documentation

When you add a new type or change visible behavior, update the readmes
as part of the same PR.

- **Main [`readme.md`](readme.md)** — keep it concise. It is the first
  thing a new user sees on NuGet and GitHub, so prefer a short, runnable
  example over a full API reference. If a section is growing into a
  spec, move the long form into the type's XML docs and leave a tight
  example in the readme.
- **Package readmes** — each package that ships its own readme
  ([`StrongTypes.EfCore`](src/StrongTypes.EfCore/readme.md),
  [`StrongTypes.FsCheck`](src/StrongTypes.FsCheck/readme.md),
  [`StrongTypes.OpenApi.Microsoft`](src/StrongTypes.OpenApi.Microsoft/readme.md),
  [`StrongTypes.OpenApi.Swashbuckle`](src/StrongTypes.OpenApi.Swashbuckle/readme.md),
  [`StrongTypes.Wpf`](src/StrongTypes.Wpf/readme.md),
  [`StrongTypes.AspNetCore`](src/StrongTypes.AspNetCore/readme.md))
  should reflect what the package actually does after your change. Update
  the one that matches.
- **Examples must be real.** Copy them from a passing test if you can —
  examples that drift from the API age into bug reports.

## Claude / Codex skill

The [`Skill/`](Skill/) directory ships a skill that teaches an AI agent
how to use this library in consumer projects. It's distributed as a
release asset (`strongtypes-skill.tar.gz`).

**Update the skill in the same PR as any user-visible change**:

- New package → add it to the `Packages` table and the
  "Helpers and integrations" table in [`Skill/SKILL.md`](Skill/SKILL.md),
  and add a `Skill/references/<package>.md` covering when to use it,
  when *not* to use it, and the wiring snippet.
- New strong type → add it to the catalog table and extend an existing
  reference (or add a new one).
- Changed wiring, recommended pattern, or compatibility → update the
  affected reference file *and* any snippet in `SKILL.md` that
  mentions it.

The skill must stand alone — its reader doesn't have the repo open.
Copy examples from passing tests where possible.

## Code of conduct

Be respectful. Assume good faith. Keep discussion focused on the code.
