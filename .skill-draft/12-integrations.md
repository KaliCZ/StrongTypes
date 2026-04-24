## Integrations

### JSON (`System.Text.Json`) — zero setup

Every wrapper except `Result<T, TError>` carries a `[JsonConverter(...)]`
attribute. Consequences:

- No `JsonSerializerOptions.Converters.Add(...)` calls. No custom setup
  in `Program.cs`. It just works.
- On-the-wire format matches the underlying primitive: `"hello"`,
  `42`, `[1, 2, 3]`. The exception is `Maybe<T>`, which serialises as
  `{ "Value": x }` / `{ "Value": null }` (or accepts `{}` for `None`).
- Invalid payloads throw `JsonException` at deserialization — in
  ASP.NET Core that's *before* your endpoint method runs, which is
  usually exactly what you want.
- `Result<T, TError>` has **no** converter by design. Translate to a
  response DTO before serialising.

### EF Core — `Kalicz.StrongTypes.EfCore`

One call in `AddDbContext`:

```csharp
services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseStrongTypes());
```

`UseStrongTypes()` does two things:

1. Registers a convention that attaches the right `ValueConverter` to
   every strong-type property on every entity — before EF's
   property-discovery pass, so the wrappers don't get misread as owned
   entity types. You do **not** call `HasConversion(...)` yourself.
2. Registers a LINQ method-call translator: `property.Unwrap()` inside a
   predicate rewrites to a plain column reference, retyped with the
   underlying column's mapping.

Works for every relational provider (SQL Server, PostgreSQL, SQLite, …).

#### Modelling

Strong types sit directly on entities, including the nullable form:

```csharp
public sealed class User
{
    public Guid Id { get; init; }
    public NonEmptyString Name { get; init; }
    public NonEmptyString? Nickname { get; init; }
    public Positive<int>   LoginCount { get; init; }
    public NonNegative<decimal> Balance { get; init; }
}
```

Columns are the underlying type — `nvarchar` for `NonEmptyString`, `int`
for `Positive<int>`, `decimal(...)` for `NonNegative<decimal>`, and the
nullable form becomes a nullable column.

#### Querying

Equality, null checks, ordering, and grouping work directly on the
wrapper:

```csharp
var needle = NonEmptyString.Create("alice");
var user = await db.Users.SingleOrDefaultAsync(u => u.Name == needle);

var withNickname = await db.Users.Where(u => u.Nickname != null).ToListAsync();
var ordered = await db.Users.OrderBy(u => u.Name).ToListAsync();
```

Anything that uses the *underlying* value — `Contains`, `StartsWith`,
arithmetic, `EF.Functions.Like`, `EF.Functions.Collate` — uses
`Unwrap()`:

```csharp
await db.Users.Where(u => u.Name.Unwrap().StartsWith("ali")).ToListAsync();
await db.Users.Where(u => EF.Functions.Like(u.Name.Unwrap(), "ali%")).ToListAsync();
await db.Users.Where(u => u.LoginCount.Unwrap() * 2 > 10).ToListAsync();
```

`Unwrap()` is a marker in EF-translated expressions. It also works in
in-memory LINQ (just returns `.Value`), so a query that runs server-side
in production can also run client-side in tests without rewriting.

### FsCheck — `Kalicz.StrongTypes.FsCheck`

One attribute on the test class:

```csharp
using FsCheck.Xunit;
using StrongTypes.FsCheck;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class MyTests
{
    [Property]
    public void NonEmptyString_is_never_whitespace(NonEmptyString value)
    {
        Assert.False(string.IsNullOrWhiteSpace(value));
    }

    [Property]
    public void Positive_stays_positive(Positive<int> value)
    {
        Assert.True(value.Value > 0);
    }
}
```

#### What ships

Every scalar strong type ships three shapes — the bare type, its nullable
form (~5% null), and a `Maybe<T>` form (~5% None):

| Type                | `T`              | `T?`                       | `Maybe<T>`              |
| ------------------- | ---------------- | -------------------------- | ----------------------- |
| `NonEmptyString`    | `NonEmptyString` | `NullableNonEmptyString`   | `MaybeNonEmptyString`   |
| `Digit`             | `Digit`          | `NullableDigit`            | `MaybeDigit`            |
| `Positive<int>`     | `PositiveInt`    | `NullablePositiveInt`      | `MaybePositiveInt`      |
| `Negative<int>`     | `NegativeInt`    | `NullableNegativeInt`      | `MaybeNegativeInt`      |
| `NonNegative<int>`  | `NonNegativeInt` | `NullableNonNegativeInt`   | `MaybeNonNegativeInt`   |
| `NonPositive<int>`  | `NonPositiveInt` | `NullableNonPositiveInt`   | `MaybeNonPositiveInt`   |

Also bundled: `NonEmptyEnumerableInt`, and `MaybeBool` / `MaybeInt` /
`MaybeLong` / `MaybeDouble` / `MaybeChar` / `MaybeString` / `MaybeGuid`
(all ~5% None).

#### Inside a single test project

Even without the FsCheck package, keep shared arbitraries on a single
`Generators` class (convention in this repo lives at
`src/StrongTypes.Tests/Generators.cs`). One attribute per test class
(`[Properties(Arbitrary = new[] { typeof(Generators) })]`) picks them all
up. Weight branches with `Gen.Frequency` when one case is the common
path — a ~90 / 10 populated-vs-null split is a good default.
