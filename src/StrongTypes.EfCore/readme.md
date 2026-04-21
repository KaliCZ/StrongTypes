# Kalicz.StrongTypes.EfCore

[![Build](https://img.shields.io/github/actions/workflow/status/KaliCZ/StrongTypes/build.yml?branch=main&label=build)](https://github.com/KaliCZ/StrongTypes/actions/workflows/build.yml)<br>
[![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)<br>
[![Kalicz.StrongTypes on NuGet](https://img.shields.io/nuget/v/Kalicz.StrongTypes?label=nuget%20%28StrongTypes%29)](https://www.nuget.org/packages/Kalicz.StrongTypes/)<br>
[![Kalicz.StrongTypes downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes?label=downloads%20%28StrongTypes%29)](https://www.nuget.org/packages/Kalicz.StrongTypes/)<br>
[![Kalicz.StrongTypes.EfCore on NuGet](https://img.shields.io/nuget/v/Kalicz.StrongTypes.EfCore?label=nuget%20%28StrongTypes.EfCore%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/)<br>
[![Kalicz.StrongTypes.EfCore downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.EfCore?label=downloads%20%28StrongTypes.EfCore%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/)<br>
[![Kalicz.StrongTypes.FsCheck on NuGet](https://img.shields.io/nuget/v/Kalicz.StrongTypes.FsCheck?label=nuget%20%28StrongTypes.FsCheck%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.FsCheck/)<br>
[![Kalicz.StrongTypes.FsCheck downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.FsCheck?label=downloads%20%28StrongTypes.FsCheck%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.FsCheck/)

EF Core plumbing for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes).
Lets you use `NonEmptyString`, `Positive<T>`, `NonNegative<T>`, `Negative<T>`, and
`NonPositive<T>` as regular entity properties — they round-trip through scalar
columns, and LINQ predicates over them translate to server-side SQL.

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

`UseStrongTypes()` does two things:

1. Registers a convention that attaches the right `ValueConverter` to every
   strong-type property on every mapped entity — you don't call
   `HasConversion(...)` by hand, and you don't override `ConfigureConventions`.
   The convention runs before EF's property-discovery pass, so wrappers never
   get misidentified as owned entity types.
2. Registers a method-call translator so `strongType.Unwrap()` inside a LINQ
   predicate rewrites as a plain column reference, retyped with the underlying
   type's mapping so downstream string/number operators compose cleanly.

Works against every relational provider (SQL Server, PostgreSQL, SQLite, …).

## Model

Just declare strong-type properties like any scalar:

```csharp
public sealed class User
{
    public Guid Id { get; init; }
    public NonEmptyString Name { get; init; }
    public NonEmptyString? Nickname { get; init; }
    public Positive<int> LoginCount { get; init; }
    public NonNegative<decimal> Balance { get; init; }
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
}
```

Column shape on the database is the underlying type — `nvarchar` for
`NonEmptyString`, `int` for `Positive<int>`, `decimal(18,2)` for
`NonNegative<decimal>`, etc. Same for the nullable form
(`NonEmptyString?`, `Positive<int>?`) — EF maps it to a nullable column.

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
