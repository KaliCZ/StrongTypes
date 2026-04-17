# StrongTypes — Coding Conventions

## Audience

This repository has multiple contributors. Do not rely on per-user memory,
personal preferences, or prior-session context when working here. Every
convention that matters belongs in this file (or in the code itself) so that
every collaborator — human or AI — sees the same rules.

**Do not write to the external memory system** (`~/.claude/projects/…` or
any similar per-user scratch space). No project memories, no feedback
memories, no references — nothing. If something is worth remembering
about this repo, it goes in this file so it travels with the code and
every contributor sees it.

## C# conventions

- **Primary constructors** — use them wherever possible (classes, `DbContext`
  subclasses, test fixtures, etc.).

## Folder layout — feature slices

Organize code by feature, not by technical role. An extension, a JSON
converter, and the main type for the same feature live together in the
feature's folder:

- `src/StrongTypes/Strings/NonEmptyString.cs`
- `src/StrongTypes/Strings/NonEmptyStringExtensions.cs`
- `src/StrongTypes/Strings/NonEmptyStringJsonConverter.cs`
- `src/StrongTypes/Try/TryExtensions.cs`

Extensions that *produce* a feature type go into that feature's folder
even when the input type is from somewhere else (e.g. `ToTry` on `T?`
lives under `Try/`, because the result — not the receiver — is what
defines the slice).

Namespaces stay flat at `StrongTypes` regardless of folder nesting. Folder
structure is for humans; the namespace is one flat shelf. The same applies
to the test project — keep tests in `namespace StrongTypes.Tests` even
when they live in subfolders, to avoid shadowing type names (e.g., a
nested `StrongTypes.Tests.Try` namespace would hide the `StrongTypes.Try`
class from sibling files).

## Migration from _Old

The codebase is mid-migration. Anything under a folder or filename suffixed with
`_Old` is legacy and should not be modified or extended. New code goes in
non-suffixed folders (e.g., new string types go in `src/StrongTypes/Strings/`).

When you rework an _Old type, treat the rewrite as a real review — drop the
`_Old` suffix, read the entire file, and improve anything that is wrong or
outdated, not just what the task names. Do not port code verbatim.

**Dealing with the _Old file being replaced:**

- If the old file is *entirely* about the type being reworked (e.g.,
  `NonEmptyString_Old.cs`), delete it. The new file is the replacement.
- If the old file mixes multiple concerns (e.g., `StringExtensions_Old.cs`
  contains extensions for both `string` and `NonEmptyString`), leave the
  `_Old` file in place and create a new non-suffixed file containing *only*
  the reworked pieces. The remaining legacy surface stays where it was until
  its own migration pass.
- In either case, minimally patch remaining `_Old` call sites only as much as
  is needed to keep the build green.

**Other rules:**

- Do **not** take a dependency on other _Old types (e.g., `Option<T>`). Prefer
  modern BCL primitives (nullable reference types, `Result`-shaped APIs, etc.).
- Do not delete _Old files unless explicitly asked or unless the rule above
  applies.

## Validated value types — the TryCreate / Create pattern

For value types that enforce a validation rule (e.g., `NonEmptyString`,
`PositiveInt`), follow this factory pattern:

```csharp
// Returns null when validation fails. Caller handles the null case.
public static NonEmptyString? TryCreate(string? value) { ... }

// Throws when validation fails. Thin wrapper over TryCreate.
public static NonEmptyString Create(string? value)
    => TryCreate(value) ?? throw new ArgumentException(...);
```

Rules:
- The validation logic lives in `TryCreate` only. `Create` delegates.
- Constructors are `private` — callers must go through the factories.
- Use nullable reference types. The library project does not enable them
  globally yet, so add `#nullable enable` at the top of each new file.
- Do **not** return `Option<T>` from new code.

## Tests

**Default to FsCheck property tests.** Generative coverage is the first
thing to reach for — it surfaces edge cases a handful of hand-picked rows
will miss, and it keeps test bodies focused on a single invariant across
the whole input space.

- Write `[Property]` tests from `FsCheck.Xunit` and let an `Arbitrary<T>`
  cover the input space. Register arbitraries on a test class via
  `[Properties(Arbitrary = new[] { typeof(Generators) })]`.
- All shared arbitraries live in a single `Generators` class at
  `src/StrongTypes.Tests/Generators.cs`. Add new arbitraries there rather
  than creating per-feature generator classes — one shelf so tests can
  grab anything with a single `[Properties(Arbitrary = new[] { typeof(Generators) })]`
  attribute. Weight branches with `Gen.Frequency` when one branch is the
  common case and the other needs only occasional coverage (e.g. 90%
  populated, 10% null).
- When a custom generator is non-trivial, pair it with a one-off `[Fact]`
  that samples it a few hundred times and asserts every partition branch
  appears, so a regression in the generator can't silently mask missing
  coverage.

**Fall back to `[Theory]` + `[InlineData]` (or `[MemberData]`) only when
property tests don't fit** — e.g. the input space doesn't generate cleanly,
the invariant you want to express is really a small fixed set of worked
examples, or the generator/shrinker would be more work than the test is
worth. When you do use `[Theory]`, still prefer one parameterized method
with several data rows over duplicated `[Fact]` methods whose bodies
differ only in input or expected value.

Use separate `[Fact]` methods when the test body genuinely differs — e.g.
asserting a side effect like "the factory was invoked exactly once".

## Integration tests (StrongTypes.Api)

- **Requests** — always use anonymous objects (e.g. `new { Value = "x",
  NullableValue = (string?)null }`), never the typed request records from
  the API project. The tests should verify the on-the-wire JSON contract
  the client actually sends, not the C# type graph.
- **Responses** — for simple ID-only responses, deserializing into the
  typed response record (e.g. `StringEntityResponse`) is fine. For richer
  payloads (full entity bodies, `ValidationProblemDetails`, etc.) parse as
  `JsonElement` and pull fields by name — that verifies the on-the-wire
  HTTP contract directly.
- **Test base class** — inherit from `IntegrationTestBase(factory)` to get
  `Client`, `SqlDb`, `PgDb`, `Ct` (the current test's `CancellationToken`),
  route constants/builders, HTTP wrappers (`Post`/`Put`/`Get`) that capture
  `Client` and `Ct` implicitly and bake in `StringEntityResponse` on the
  success path, `Body<T>(value, nullableValue)`, and `AssertStringEntity`.
- **CancellationTokens** — xunit.v3's `xUnit1051` analyzer requires
  `TestContext.Current.CancellationToken` be threaded into every async
  call that accepts one (`PostAsJsonAsync`, `PutAsJsonAsync`,
  `ReadFromJsonAsync`, `FindAsync([id], ct)`, `GetAsync`, …). Grab it at
  the top of each test: `var ct = TestContext.Current.CancellationToken;`.

## StrongTypes.Api — purpose

An ASP.NET Core minimal API that exists purely as an integration-test
harness: it verifies that strong types round-trip correctly through the
ASP.NET Core request pipeline and EF Core against both SQL Server and
PostgreSQL (via Testcontainers). Every write endpoint persists to both
`DbContext`s and every read endpoint reads from one specific provider,
so tests can assert the full wire-to-DB path on each.

Current state: uses plain `string` / `string?`. Strong-type converters
(EF Core value converters, JSON converters) will be wired in once the
parallel work on those lands.

## Pull requests

- **Always assign the person running the session** when opening a PR.
  Use the GitHub `get_me` tool to discover the authenticated user.

## Style

- Prefer `Edit` over rewriting whole files.
- Keep public API changes minimal and documented.
- Do not add files, abstractions, or configurability beyond what the task asks for.
