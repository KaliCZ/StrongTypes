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

## Testing — the rules for new types

**Every new type must ship with tests.** What kinds of tests depend on
where the type travels — the matrix below maps capabilities to the test
projects that must be touched. The full conventions for writing each
kind of test live in [`testing.md`](testing.md); read that before
writing tests or production code that will need them.

| The new type… | Required test projects |
|---|---|
| Has any behavior at all | `StrongTypes.Tests` |
| Has a `System.Text.Json` converter | `StrongTypes.Tests` + `StrongTypes.Api.IntegrationTests` |
| Is meant to be stored in a database | `StrongTypes.Api.IntegrationTests` (covers both SQL Server and PostgreSQL) |
| Appears in an ASP.NET Core request/response | `StrongTypes.OpenApi.IntegrationTests` |
| Ships an analyzer or code fix | `StrongTypes.Analyzers.Tests` |

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
  [`StrongTypes.OpenApi.Swashbuckle`](src/StrongTypes.OpenApi.Swashbuckle/readme.md))
  should reflect what the package actually does after your change. Update
  the one that matches.
- **Examples must be real.** Copy them from a passing test if you can —
  examples that drift from the API age into bug reports.

## Code of conduct

Be respectful. Assume good faith. Keep discussion focused on the code.
