# EF Core — `Kalicz.StrongTypes.EfCore`

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

## Modelling

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

## Querying

Equality, null checks, ordering, and grouping work directly on the
wrapper:

```csharp
var needle = "alice".ToNonEmpty();
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
