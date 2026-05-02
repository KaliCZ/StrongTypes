# Testing

This is the single source of truth for how tests are written in this
repo. It is imported into [`CLAUDE.md`](CLAUDE.md) so AI agents read it
every session, and linked from [`CONTRIBUTING.md`](CONTRIBUTING.md) so
human contributors see the same rules.

The repo has four test projects. Which ones a change needs to touch
depends on what the type does — see the matrix in `CONTRIBUTING.md`.

## Unit tests — `StrongTypes.Tests`

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

For a validated type, at minimum cover `TryCreate` (returns `null` for
invalid input, wraps valid input), `Create` (throws for invalid, wraps
valid), equality / comparison / `ToString` / implicit conversions where
they exist, and any extension methods that live in the same feature
folder.

## API integration tests — `StrongTypes.Api.IntegrationTests`

This project hosts the minimal API in `StrongTypes.Api` and runs every
write through both SQL Server and PostgreSQL (via Testcontainers). Tests
here verify three things at once: the JSON converter, the ASP.NET Core
request pipeline, and the EF Core value converter against both providers.

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

When adding a type that should round-trip over the wire and into a
database, add a controller in `StrongTypes.Api/Controllers`, an entity in
`StrongTypes.Api/Entities` following the existing
`IEntity<TSelf, T, TNullable>` shape, and a test class under
`Tests/ApiTests`. Cover the valid round-trip (POST + GET, asserting
persisted state on both `SqlSet` and `PgSet`), invalid payloads returning
`400`, and `null` handling for the nullable variant. If the type has a
custom JSON converter, add converter-only tests under `Tests/ConverterTests`.

## OpenAPI integration tests — `StrongTypes.OpenApi.IntegrationTests`

Verifies the schema each type produces in **both** supported OpenAPI
stacks: `Microsoft.AspNetCore.OpenApi` and Swashbuckle. The tests share
`OpenApiDocumentTestsBase`, an `abstract partial` class split across
feature-scoped files (`*.Strings.cs`, `*.Numerics.cs`,
`*.Collections.cs`, `*.Composition.cs`, `*.Annotations.cs`,
`*.Components.cs`). Two concrete subclasses
(`MicrosoftOpenApiDocumentTests`, `SwashbuckleOpenApiDocumentTests`) run
the whole suite once per pipeline.

When adding a type that appears in API contracts:

1. Expose it via an endpoint in `StrongTypes.OpenApi.TestApi.Shared` so
   both the Microsoft and Swashbuckle host apps pick it up.
2. Add tests to the matching partial of `OpenApiDocumentTestsBase`. Use
   the helpers in `Helpers/` (`SchemaNavigation`, `SchemaValueReader`,
   `SchemaWalk`, `NullableUnwrap`, `ExclusiveBounds`,
   `ComponentSchemas`) — don't hand-roll JSON traversal.
3. Cover the rendered schema shape (type, format, constraints like
   `minLength` / `minimum` / `exclusiveMinimum`), the nullable variant,
   and whether the schema is inlined or referenced.
4. If a pipeline genuinely emits a different keyword, encode that as a
   `protected virtual bool Is…Broken` flag on the base and override it
   in the affected subclass — the test still runs on both pipelines and
   asserts the keyword is *absent* on the broken side, so the suite
   catches it the day the framework starts honouring the annotation.

Use `OpenApiVersion`-aware helpers in `Helpers/ExclusiveBounds.cs` for
anything that differs between OpenAPI 3.0 and 3.1 (e.g. exclusive
bounds). Don't write version-conditional asserts inline.

## Analyzer tests — `StrongTypes.Analyzers.Tests`

For every diagnostic and code fix, write both a "reports the diagnostic"
test and a "code fix produces the expected output" test. Drive each
through the real Roslyn pipeline using `AnalyzerTester` /
`CodeFixTester` from `Infrastructure/`, with the relevant
`TestReferences` combination (`EntityFrameworkCore`, `StrongTypesEfCore`,
…) so the analyzer sees realistic compilation context.

Cover both directions: the analyzer **fires** when the offending pattern
is present and the required reference is missing, and is **silent** when
the reference is present.
