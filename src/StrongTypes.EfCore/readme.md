# Kalicz.StrongTypes.EfCore

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.EfCore?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.EfCore?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

EF Core plumbing for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).
Lets you use `NonEmptyString`, `Email`, `MailAddress`, `Positive<T>`,
`NonNegative<T>`, `Negative<T>`, and `NonPositive<T>` as regular entity
properties — they round-trip through scalar columns, and LINQ predicates over
them translate to server-side SQL.

## Install

```powershell
dotnet add package Kalicz.StrongTypes.EfCore
```

The analyzer that ships with `Kalicz.StrongTypes` will nudge you to install this
package whenever it sees a strong-type property on an EF-mapped entity.

## Register

One call, on the options builder:

```csharp
services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseStrongTypes());
```

`UseStrongTypes()` does three things:

1. Registers a convention that attaches the right `ValueConverter` to every
   strong-type property on every mapped entity — you don't call
   `HasConversion(...)` by hand, and you don't override `ConfigureConventions`.
   The convention runs before EF's property-discovery pass, so wrappers never
   get misidentified as owned entity types.
2. Registers a method-call translator so `strongType.Unwrap()` inside a LINQ
   predicate rewrites as a plain column reference, retyped with the underlying
   type's mapping so downstream string/number operators compose cleanly.
3. Registers integrity guards for column-mapped intervals: materializing a
   stored row that violates `Start <= End` throws `InvalidOperationException`
   instead of producing an invalid interval, and each property's
   `IntervalBoundMode` is applied on read and enforced on save.

Works against every relational provider (SQL Server, PostgreSQL, SQLite, …).

## Model

Just declare strong-type properties like any scalar:

```csharp
public sealed class User
{
    public Guid Id { get; init; }
    public NonEmptyString Name { get; init; }
    public NonEmptyString? Nickname { get; init; }
    public Email? ContactEmail { get; init; }
    public MailAddress? BackupEmail { get; init; }
    public Positive<int> LoginCount { get; init; }
    public NonNegative<decimal> Balance { get; init; }
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
}
```

Column shape on the database is the underlying type — `nvarchar` for
`NonEmptyString` and for `Email` / `MailAddress` (the address string), `int`
for `Positive<int>`, `decimal(18,2)` for `NonNegative<decimal>`, etc. Same for
the nullable form (`NonEmptyString?`, `Email?`, `Positive<int>?`) — EF maps it
to a nullable column.

An email column can be either `Email` (the wrapper — re-checks the 254-char cap
on read) or the BCL `MailAddress`; the two convert to each other implicitly, so
store whichever your domain already holds.

Non-public properties are wired too. An `internal`/`private` EF-mapped
backing property — the usual DDD pattern behind a computed public view —
gets its converter automatically once you map it, so you never hand-write
`HasConversion`:

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

## Intervals

The interval types (`FiniteInterval<T>`, `Interval<T>`, `IntervalFrom<T>`,
`IntervalUntil<T>`) auto-map too. By **default**, `UseStrongTypes()` maps each
interval property to **two scalar endpoint columns** as an EF Core complex type
— each endpoint its own queryable, **indexable** column, nullable exactly when
the variant's endpoint is. Endpoint access in LINQ translates to plain column
references:

```csharp
var hits = await db.Bookings.Where(b => b.Window.Start <= at && at <= b.Window.End).ToListAsync();
```

Endpoint columns are named `Start` / `End`, prefixed with the property name
(`Window_Start`) when two intervals on the same entity would clash. A nullable
interval property additionally gets a shadow discriminator column, keeping a
`null` property distinct from a stored interval whose endpoints are all `NULL`
(a fully-unbounded `Interval<T>`).

**Bound inclusivity** is configured per bound with `IntervalBoundMode` on
`HasIntervalColumns`. The default, `AlwaysInclusive`, stores no flag column;
saving a value created with `startInclusive: false` / `endInclusive: false`
throws with a message naming the fix. `AlwaysExclusive` fixes the opposite
convention (also without a column) and restores it on read — it requires
`UseStrongTypes()`. `Stored` adds a `StartInclusive` / `EndInclusive` `bit`
column and round-trips each value's own bounds; the flag is a plain column,
so `Where(b => !b.Window.EndInclusive)` translates:

```csharp
modelBuilder.Entity<Shift>()
    .HasIntervalColumns(s => s.Window, endBound: IntervalBoundMode.AlwaysExclusive);   // [start, end) windows

modelBuilder.Entity<Promo>()
    .HasIntervalColumns(p => p.Window, startBound: IntervalBoundMode.Stored, endBound: IntervalBoundMode.Stored);
```

Only `Stored` makes a bound's flag queryable. Under `AlwaysInclusive` /
`AlwaysExclusive` (and the single-JSON-column mapping) the flags aren't columns,
so a `Where` / `OrderBy` over `StartInclusive` / `EndInclusive` can't translate
and throws at query time — the endpoint values `Start` / `End` translate under
every mapping.

To rename the endpoint columns, pass `startName` / `endName` (or configure the
returned complex-property builder — `HasIntervalColumns` returns it, and it is
also how you get this mapping without `UseStrongTypes()`):

```csharp
modelBuilder.Entity<Booking>()
    .HasIntervalColumns(b => b.Window, startName: "WindowStart", endName: "WindowEnd");
```

To index an endpoint: EF Core cannot declare an index over a complex-type
member (`HasIndex(b => b.Window.Start)` is rejected), so add the index in a
migration — the endpoint is a plain column on the entity's table:

```csharp
migrationBuilder.CreateIndex(name: "IX_Bookings_WindowStart", table: "Bookings", column: "WindowStart");
```

**One JSON column** — opt in with `HasIntervalJsonConversion`, on the entity
builder or on the property builder (the latter also handles nullable
properties). The interval round-trips through its validating JSON converter
into a single column (`jsonb` on PostgreSQL, `nvarchar(max)` on SQL Server),
and endpoint access in LINQ translates to a server-side JSON path lookup
(`JSON_VALUE(...)` on SQL Server, `->>` on PostgreSQL) — still filterable and
orderable, just not index-backed:

```csharp
modelBuilder.Entity<Booking>()
    .HasIntervalJsonConversion(b => b.Window);

modelBuilder.Entity<Booking>()
    .Property(b => b.MaybeWindow)              // FiniteInterval<int>?
    .HasIntervalJsonConversion();
```

Both shapes re-validate on read: a stored row violating `Start <= End` throws
when materialized — the JSON shape through its converter, the two-column shape
through the validation `UseStrongTypes()` registers (without that registration,
two-column reads trust the database).

## Filtering

Equality, null checks, ordering, and grouping work directly on the strong type:

```csharp
// Equality against a strong-type value
var needle = NonEmptyString.Create("alice");
var user = await db.Users.SingleOrDefaultAsync(u => u.Name == needle);

// Null / not-null on the nullable form
var withNickname = await db.Users
    .Where(u => u.Nickname != null)
    .ToListAsync();

// OrderBy directly on the wrapper
var ordered = await db.Users
    .OrderBy(u => u.Name)
    .ToListAsync();
```

For anything that needs the *underlying* value — string operators like
`Contains` / `StartsWith`, arithmetic on numerics, `EF.Functions.Like` —
call `Unwrap()`:

```csharp
// StartsWith / Contains / EndsWith translate server-side via Unwrap()
var search = await db.Users
    .Where(u => u.Name.Unwrap().StartsWith("ali"))
    .ToListAsync();

// EF.Functions.Like — the canonical case
var wildcard = await db.Users
    .Where(u => EF.Functions.Like(u.Name.Unwrap(), "ali%"))
    .ToListAsync();

// Arithmetic on numeric wrappers. Cast to long if you want to guard
// against int32 overflow on wide columns.
var active = await db.Users
    .Where(u => u.LoginCount.Unwrap() * 2 > 10)
    .ToListAsync();

// Works the same for EF.Functions.Collate, string.IsNullOrEmpty, etc.
var caseInsensitive = await db.Users
    .Where(u => EF.Functions.Collate(u.Name.Unwrap(), "SQL_Latin1_General_CP1_CI_AS") == "alice")
    .ToListAsync();
```

`Unwrap()` is a marker call for EF — the in-memory implementation just returns
`.Value`, so the same expression works fine if you ever need to run a LINQ
query client-side. The translator rewrites the call to a column reference
before SQL generation.

## License

MIT. See the [StrongTypes repository](https://github.com/KaliCZ/StrongTypes).
