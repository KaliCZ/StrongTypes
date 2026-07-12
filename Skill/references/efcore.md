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
    public Email? ContactEmail { get; init; }
    public MailAddress? BackupEmail { get; init; }
    public Positive<int>   LoginCount { get; init; }
    public NonNegative<decimal> Balance { get; init; }
}
```

Columns are the underlying type — `nvarchar` for `NonEmptyString` and for
`Email` / `MailAddress` (the address string), `int` for `Positive<int>`,
`decimal(...)` for `NonNegative<decimal>`, and the nullable form becomes a
nullable column. An email column can be `Email` (the wrapper — re-checks the
254-char cap on read) or the BCL `MailAddress`; they convert to each other
implicitly, so store whichever your domain already holds.

### Non-public backing properties

The convention wires converters for non-public properties too — the common
DDD pattern of an `internal`/`private` EF-mapped backing property behind a
computed public view. Map the backing property as usual; you still don't
call `HasConversion` (which also sidesteps the nullable-reference warning a
hand-written `HasConversion(new …Converter())` raises on a `T?` property):

```csharp
public sealed class Brand
{
    public Guid Id { get; init; }
    public NonEmptyString Name { get; init; }

    internal NonEmptyString? AliasesInternal { get; set; }   // EF-mapped storage
}

protected override void OnModelCreating(ModelBuilder modelBuilder) =>
    modelBuilder.Entity<Brand>()
        .Property(brand => brand.AliasesInternal)
        .HasColumnName("Aliases");                            // converter is automatic
```

The interval types (`FiniteInterval<T>`, `Interval<T>`, `IntervalFrom<T>`,
`IntervalUntil<T>`) auto-map too: by default `UseStrongTypes()` maps each
interval property to **two scalar endpoint columns** (plain, indexable column
references in LINQ; a nullable property adds a shadow discriminator). Bound
inclusivity is a mapping choice per bound (`IntervalBoundMode` on
`HasIntervalColumns`): the default `AlwaysInclusive` stores no flag column and
rejects exclusive-bound values on save; `AlwaysExclusive` fixes the other
convention; `Stored` adds a flag column and round-trips per-value bounds. To
store a **single JSON column** instead (`jsonb` on PostgreSQL, endpoint access
still translating to a server-side JSON path lookup, bound flags riding in the
payload), opt in per property with
`entity.HasIntervalJsonConversion(e => e.Window)`. Both shapes re-validate the
interval invariant on read. See `references/intervals.md`.

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
