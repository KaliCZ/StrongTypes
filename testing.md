# Testing

This is the single source of truth for how tests are written in this
repo. [`CLAUDE.md`](CLAUDE.md) and [`CONTRIBUTING.md`](CONTRIBUTING.md)
both point here — anything testing-related lives in this file so it
can't drift out of sync.

**Every new type must ship with tests.** Which test projects a change
needs to touch depends on what the type does:

| The new type… | Required test projects |
|---|---|
| Has any behavior at all | `StrongTypes.Tests` |
| Has a `System.Text.Json` converter | `StrongTypes.Tests` + `StrongTypes.Api.IntegrationTests` |
| Is meant to be stored in a database | `StrongTypes.Api.IntegrationTests` (covers both SQL Server and PostgreSQL) |
| Appears in an ASP.NET Core request/response | `StrongTypes.OpenApi.IntegrationTests` |
| Binds from a non-body source, or affects MVC error handling | `StrongTypes.AspNetCore.IntegrationTests` |
| Carries a `TypeConverter` (every wrapper does) | `StrongTypes.Tests` (`ComponentModel/`) |
| Ships an analyzer or code fix | `StrongTypes.Analyzers.Tests` |

## Unit tests — `StrongTypes.Tests`

**Default to FsCheck property tests.** Generative coverage is the first
thing to reach for — it surfaces edge cases a handful of hand-picked rows
will miss, and it keeps test bodies focused on a single invariant across
the whole input space.

- Write `[Property]` tests from `FsCheck.Xunit` and let an `Arbitrary<T>`
  cover the input space. Register arbitraries on a test class via
  `[Properties(Arbitrary = new[] { typeof(Generators) })]`.
- All shared arbitraries live in the shipped `Generators` class at
  `src/StrongTypes.FsCheck/Generators.cs`, which the test project consumes
  via project reference. Add new arbitraries there rather than creating
  per-feature generator classes — one shelf so tests can grab anything
  with a single `[Properties(Arbitrary = new[] { typeof(Generators) })]`
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
- **Test base classes** — `IntegrationTestBase(factory)` provides `Client`,
  `SqlDb`, `PgDb`, `Ct` (the current test's `CancellationToken`),
  `SqlServerAvailable`, and the generic `AssertEntity`. The CRUD harness base
  `EntityCrudTestsBase` adds the route constants/builders and the HTTP wrappers
  (`Post`/`Put`/`Patch`/`Get`) that capture `Client` and `Ct` implicitly and
  bake in `EntityResponse` on the success path.
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

**One shared CRUD surface, two wire-shape adapters.** The create / get / update /
PATCH matrix (with the null / value / clear-nullable semantics) is the same for
every type — what differs is only the wire shape — so it lives exactly once, in
the abstract `EntityCrudTestsBase<TEntity, T, TNullable>`. Two thin adapters
supply the wire-shape-specific pieces (the request body for a value, its
strong-typed form, and the GET read assertion) and add their own invalid-payload
cases: `EntityTests<…, TWire>` for scalar strong types (numbers, and the
reference types `NonEmptyString` / `Email` / `MailAddress`, which all serialize as
a single JSON scalar), and `IntervalEntityTests<TEntity, TInterval>` for
intervals, which serialize as a JSON **object** (`{ "start": …, "end": … }`) that
does not fit the scalar `TWire` bodies. Because the shared scenarios exist once,
they cannot drift: a new create / get / update / PATCH scenario goes in
`EntityCrudTestsBase` and every type gets it automatically. Only the
invalid-payload cases legitimately differ (a malformed scalar vs. `Start > End` /
a missing required endpoint), and those stay in the adapters. A new type picks
its adapter by wire shape, not by struct-vs-class.

A `400` from an invalid body is not just a status code — the shared
`EntityCrudTestsBase` asserts the full `ValidationProblemDetails` shape
(`AssertValidationProblem`: `application/problem+json`, `status`/`title`,
a non-empty `errors` object) and the error key. Because `StrongTypes.Api`
does **not** call `AddStrongTypes`, these are the raw framework keys: a
malformed non-null value is keyed by its System.Text.Json path
(`$.value` / `$.nullableValue`), uniform across every type; a `null` value
is keyed either `$.value` (struct, or a reference converter that rejects
null at parse time) or `Value` (a reference converter that maps null
through, then trips the implicit-required check) — so the null assertion
accepts the field key with or without the `$.` prefix rather than pinning
the mechanism.

### SQL Server availability and skipping

The `mcr.microsoft.com/mssql/server` image is amd64-only, so on an ARM64
host (e.g. a Snapdragon dev box) `sqlservr` segfaults under emulation and
the container never starts. PostgreSQL has native ARM images and is
**always required**; SQL Server is too, with one narrow escape hatch: set
`STRONGTYPES_SKIP_SQLSERVER=1` to skip it entirely (the container is never
started). Without the opt-in, a SQL Server that fails to start is a hard
failure — the fixture throws and the run goes red. The flag is honoured
wherever it is set, so don't set it in CI: there is no separate CI guard.

When skipped, the fixture swaps in an in-memory stub so the dual-write
endpoints still boot, but it does **not** exercise the real SQL Server wire
path — guard every SQL-Server assertion accordingly:

- In a test deriving from `IntegrationTestBase`, assert the persisted row
  via `AssertEntity(id, value, nullableValue)` — it checks PostgreSQL
  unconditionally and SQL Server only when available.
- In a provider-parametrized test (`[Theory]` over `Providers`), call
  `SkipIfSqlServerUnavailable(provider)` as the first statement.
- For any other SQL-Server-only assertion, gate it on `SqlServerAvailable`.

Tests that touch no database (the `BindingTests` and the collection-JSON
round-trips) need neither provider's assertions and run on any host once
the PostgreSQL container is up.

## ASP.NET Core integration tests — `StrongTypes.AspNetCore.IntegrationTests`

Covers the `Kalicz.StrongTypes.AspNetCore` package against the
`StrongTypes.AspNetCore.TestApi` host (`WebApplicationFactory<Program>`,
no database, so these run on any host without containers):

- **Model binding** (`BindingTests`) — `NonEmptyEnumerable<T>` from
  `[FromForm]` / `[FromQuery]` / `[FromHeader]` / `[FromRoute]`.
- **JSON error-key normalization** (`JsonBodyErrorKeyTests`) — the
  opt-out feature that rewrites a failed body's error key from the
  System.Text.Json path (`$.value`) to the property name (`Value`). Test
  it in **both** modes from one parameterized suite: a `bool normalize`
  theory parameter picks between two `WebApplicationFactory` variants
  (`NormalizedJsonErrorKeysFactory` / `RawJsonErrorKeysFactory`, the
  latter setting `NormalizeJsonErrorKeys = false` via `ConfigureTestServices`).
  Cover a reference strong type and a struct strong type so both
  failure mechanisms (parse-time converter failure → `$.value`;
  reference null → implicit-required `Value`) are exercised. The pure
  key-rewriting logic is unit-tested separately in
  `JsonValidationErrorKeyNormalizerTests` (casing variants, nested/array
  segments, pass-through of non-`$` keys).

Don't duplicate the whole per-type `EntityTests` matrix here — the
normalization is type-agnostic (it rewrites the path string, never
inspecting the type), so a reference + struct representative is enough.

## OpenAPI integration tests — `StrongTypes.OpenApi.IntegrationTests`

Verifies the schema each type produces in **both** supported OpenAPI
stacks: `Microsoft.AspNetCore.OpenApi` and Swashbuckle. The tests share
`OpenApiDocumentTestsBase`, an `abstract partial` class split across
feature-scoped files (`*.Strings.cs`, `*.Emails.cs`, `*.Numerics.cs`,
`*.Collections.cs`, `*.Intervals.cs`, `*.Composition.cs`,
`*.Annotations.cs`, `*.Components.cs`, `*.NonBodyBinding.cs`). Three
concrete subclasses (`MicrosoftOpenApiDocumentTests`,
`MicrosoftOpenApi31DocumentTests`, `SwashbuckleOpenApiDocumentTests`) run
the whole suite once per pipeline — and, for Microsoft, once per OpenAPI
version.

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

## OpenAPI Core unit tests — `StrongTypes.OpenApi.Core.Tests`

Pipeline-independent logic in `StrongTypes.OpenApi.Core` (e.g. the
`StrongTypeInliner`) is unit-tested here against the `Microsoft.OpenApi`
object model directly — build an `OpenApiDocument` in memory, run the
operation, assert on the result. Reach for this when the behaviour is a
property of the shared Core code itself rather than of either HTTP
pipeline (for cross-cutting behaviour both pipelines must exhibit, prefer
the shared `OpenApiDocumentTestsBase` suite so it's verified end-to-end on
each). The integration suite stays HTTP/JSON-only and does not reference
Core.

## Configuration binding tests

Plain binding — a wrapper converting from a configuration string — is
unit-tested in `StrongTypes.Tests/ComponentModel/`: `TypeConverterTests`
for the converter itself, `ConfigurationBindingTests` for the
`ConfigurationBinder` / `IOptions<T>` path, and `UnconfiguredOptionsTests`
pinning what an absent key leaves behind. A new wrapper adds its rows
there.

`StrongTypes.Configuration.Tests` covers the
`Kalicz.StrongTypes.Configuration` package — `BindStrongTypes()` and its
null-property walk:

- Build the section from an inline JSON string (`AddJsonStream`) and
  resolve `IOptions<T>.Value` through a real `ServiceCollection` — no
  host, no config files.
- Assert failures via `OptionsValidationException.Failures`, and pin the
  complete failure message verbatim at least once — it is user-facing
  surface.
- Cover both directions of every claim: the checked shape fails when its
  key is absent, and the exempt shapes (nullable, value type, initialised
  default) bind without complaint.
- The walk is type-agnostic (wrappers are leaves), so a new strong type
  needs no tests here — new tests are about *shapes* of options classes
  (nesting, collections, dictionaries, annotations).
- `StrongTypes.Configuration.Tests.NullableDisabled` supplies options
  classes from an assembly with `<Nullable>disable</Nullable>`, compiled
  exactly as a real unannotated consumer would be. Add shapes there; the
  tests asserting on them stay in the main project.

## WPF binding tests — `StrongTypes.Wpf.Tests`

Proves two-way WPF binding works off the core package's
`[TypeConverter]`s alone: the project references `StrongTypes` and the
`StrongTypes.Wpf.TestApp` sample — deliberately nothing else, because
there is no WPF package to install.

- The project targets `net10.0-windows`. The Ubuntu `build` job compiles
  it but skips it at `dotnet test`; the suite runs in the dedicated
  `test-wpf` workflow on `windows-latest`.
- Run every test body through `StaThread.Run(...)` — WPF requires an STA
  thread and xUnit workers are MTA. The helper also shuts the thread's
  `Dispatcher` down; without that, leaked foreground threads make the
  test host exit non-zero after all tests pass.
- `TestSetup`'s module initializer creates the one `Application` the
  binding engine needs — never construct another in a test.
- A binding's culture is `ConverterCulture ?? element.Language`, never
  the host thread's. Anything culture-sensitive follows
  `CultureBindingTests`: run each case under several host cultures and
  assert the host is ignored.
- Binding resolves through `TypeDescriptor` and is type-agnostic, so
  representative types are enough — don't mirror the per-type API matrix
  here.

## Analyzer tests — `StrongTypes.Analyzers.Tests`

For every diagnostic and code fix, write both a "reports the diagnostic"
test and a "code fix produces the expected output" test. Drive each
through the real Roslyn pipeline using `AnalyzerTester` /
`CodeFixTester` from `Infrastructure/`, with the relevant
`TestReferences` combination (`EntityFrameworkCore`, `StrongTypesEfCore`,
…) so the analyzer sees realistic compilation context.

Cover both directions: the analyzer **fires** when the offending pattern
is present, and is **silent** in every configuration that legitimately
doesn't need it — the required reference already present, the property
already guarded by `[Required]`, and so on.
