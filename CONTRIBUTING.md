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
where the type travels: in-process only, over JSON, into a database,
out through an OpenAPI document, or all of the above.

The matrix below tells you which test projects you need to touch.

| The new type… | Required test projects |
|---|---|
| Has any behavior at all | `StrongTypes.Tests` |
| Has a `System.Text.Json` converter | `StrongTypes.Tests` + `StrongTypes.Api.IntegrationTests` |
| Is meant to be stored in a database | `StrongTypes.Api.IntegrationTests` (covers both SQL Server and PostgreSQL) |
| Appears in an ASP.NET Core request/response | `StrongTypes.OpenApi.IntegrationTests` |
| Ships an analyzer or code fix | `StrongTypes.Analyzers.Tests` |

### 1. Unit tests — `StrongTypes.Tests`

Default to **FsCheck property tests** (`[Property]` from
`FsCheck.Xunit`). Hand-picked `[Theory]` rows are a fallback for cases
the generators don't cover cleanly. See the testing section in
[`CLAUDE.md`](CLAUDE.md) for the details.

For a validated type, at minimum cover:
- `TryCreate` returns `null` for invalid input and wraps valid input.
- `Create` throws for invalid input and wraps valid input.
- Equality / comparison / `ToString` / implicit conversions, where they exist.
- Any extension methods in the same feature folder.

If you add a new `Arbitrary<T>`, register it on
[`Generators`](src/StrongTypes.Tests/Generators.cs) and pair it with a
sampling `[Fact]` that asserts each branch of the generator is reachable.

### 2. JSON + EF Core integration — `StrongTypes.Api.IntegrationTests`

This project hosts the minimal API in `StrongTypes.Api` and runs every
write through both SQL Server and PostgreSQL (via Testcontainers). Tests
here verify three things at once: the JSON converter, the ASP.NET Core
request pipeline, and the EF Core value converter against both providers.

When adding a type that should round-trip over the wire and into a
database:

1. Add a controller in
   [`StrongTypes.Api/Controllers`](src/StrongTypes.Api/Controllers) and
   an entity in
   [`StrongTypes.Api/Entities`](src/StrongTypes.Api/Entities) following
   the existing `IEntity<TSelf, T, TNullable>` shape.
2. Add a test class under
   [`Tests/ApiTests`](src/StrongTypes.Api.IntegrationTests/Tests/ApiTests)
   inheriting from `IntegrationTestBase` — it gives you `Client`,
   `SqlDb`, `PgDb`, and the assertion helpers.
3. Cover: valid round-trip (POST + GET, asserting persisted state on both
   `SqlSet` and `PgSet`), invalid payloads return `400`, and `null`
   handling for the nullable variant.
4. If the type has a custom JSON converter, add converter-only tests
   under
   [`Tests/ConverterTests`](src/StrongTypes.Api.IntegrationTests/Tests/ConverterTests).

Use anonymous objects for request bodies — never the typed records from
`StrongTypes.Api`. The point is to assert the on-the-wire JSON contract.

### 3. OpenAPI integration — `StrongTypes.OpenApi.IntegrationTests`

This project verifies the schema each type produces in both supported
OpenAPI stacks: `Microsoft.AspNetCore.OpenApi` and Swashbuckle. The tests
share a base class (`OpenApiDocumentTestsBase`) split across partial
files by feature (`*.Strings.cs`, `*.Numerics.cs`, `*.Collections.cs`,
…).

When adding a type that appears in API contracts:

1. Expose it via an endpoint in
   [`StrongTypes.OpenApi.TestApi.Shared`](src/StrongTypes.OpenApi.TestApi.Shared).
2. Add tests to the matching partial of `OpenApiDocumentTestsBase`.
3. Cover: the rendered schema shape (type, format, constraints like
   `minLength` / `minimum` / `exclusiveMinimum`), the nullable variant,
   and that the schema is inlined / referenced as expected.

The shared base means one set of assertions runs against both stacks —
you do not write Microsoft and Swashbuckle tests separately.

### 4. Analyzer tests — `StrongTypes.Analyzers.Tests`

Diagnostics and code fixes need both a "reports the diagnostic" test and
a "code fix produces the expected output" test. Follow the existing
patterns in
[`StrongTypes.Analyzers.Tests`](src/StrongTypes.Analyzers.Tests).

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
