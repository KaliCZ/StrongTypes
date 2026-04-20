# StrongTypes - Stronger Typing for C#

> **Work in Progress** - StrongTypes is the continuation of [FuncSharp](https://github.com/MewsSystems/FuncSharp), originally written by [Honza Siroky](https://github.com/siroky), bringing the concepts into modern C#. It targets .NET 10 with nullable reference types enabled and leans on modern language features to deliver extra types that enable a better developer experience.

[![Build](https://img.shields.io/github/actions/workflow/status/KaliCZ/StrongTypes/build.yml?branch=main&label=build)](https://github.com/KaliCZ/StrongTypes/actions/workflows/build.yml)
[![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)
[![NuGet Version](https://img.shields.io/nuget/v/Kalicz.StrongTypes)](https://www.nuget.org/packages/Kalicz.StrongTypes/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes)](https://www.nuget.org/packages/Kalicz.StrongTypes/)

StrongTypes is not an attempt to build a full algebraic type system on top of C#. It adds small, focused value types that make everyday code safer and more expressive — things like "a string that is never empty" or "an integer that is always positive". Every type ships with `System.Text.Json` converters wired up out of the box, so validation runs at the wire boundary during deserialization without any extra setup.

## Contents

- [`Maybe<T>`](#maybet)
- [Helpful Types](#helpful-types)
  - [`NonEmptyString`](#nonemptystring)
  - [Numeric wrappers: `Positive<T>`, `NonNegative<T>`, `Negative<T>`, `NonPositive<T>`](#numeric-wrappers)
  - [What you get for free](#what-you-get-for-free)
  - [JSON serialization](#json-serialization)
  - [EF Core persistence](#ef-core-persistence)
- [Parsing helpers](#parsing-helpers)
  - [Enums](#enums)
  - [Strings](#strings)
- [Legacy types (to be replaced)](#legacy-types-to-be-replaced)
  - [`Option<A>`](#optiona)
  - [`Try<A, E>`](#trya-e)
  - [`Coproduct`](#coproduct)

## `Maybe<T>`

A value type that holds either a value of `T` (`Some`) or no value (`None`). It works for both reference and value types, plays well with collection expressions, LINQ, pattern matching, and `System.Text.Json`, and avoids the double-wrap awkwardness that `Nullable<T>` has when `T` is itself nullable.

`Maybe<int?>` and `Maybe<string?>` are deliberately not allowed — the generic constraint is `where T : notnull`. Permitting a nullable `T` would collapse the `None` and `Some(null)` cases and break the `is { } v` pattern that powers idiomatic unwrapping (see more below).

```csharp
Maybe<int>    some   = Maybe.Some(42);   // T inferred from the argument
Maybe<int>    direct = 42;               // implicit conversion from T
Maybe<string> none   = Maybe.None;       // binds to whatever Maybe<T> the context expects
Maybe<int>    a      = nullableInt.ToMaybe();      // Some(x) when HasValue, None otherwise
Maybe<string> b      = nullableString.ToMaybe();   // Some(x) when not null, None otherwise
```

The implicit conversions from `T` and from the untyped `Maybe.None` make collection expressions read naturally — no need to spell out `Maybe<int>.Some(...)` for every element, and the `..` spread operator splices existing sequences in alongside literal `Maybe.None` markers:

```csharp
int[] middle = [4, 2, 3];
Maybe<int>[] xs = [..middle, Maybe.None, 4];
IEnumerable<int> values = xs.Values();   // [4, 2, 3, 4]
```

### Unwrapping

The idiomatic "has value" check uses the `is { } v` pattern on the `Value` extension property. `Value` is provided through C# 14 extension members split between struct- and class-constrained branches, so it returns `Nullable<T>` for value types and `T?` for reference types — and the pattern unwraps to the underlying `T` directly:

```csharp
if (maybe.Value is { } v)
{
    // v is the underlying T — int (not int?), string (not string?)
}
```

For exhaustive handling, `Match` takes both branches:

```csharp
var label = maybe.Match(
    ifSome: x => $"got {x}",
    ifNone: () => "nothing"
);
```

### Composition

`Maybe<T>` composes monadically through `Map`, `FlatMap`, and `Where`. Each operation is a no-op on `None`, so chains short-circuit cleanly without explicit null checks:

```csharp
// Map — transform the inner value when present.
Maybe<int> doubled = Maybe.Some(3).Map(x => x * 2);          // Some(6)
Maybe<int> stillNone = Maybe<int>.None.Map(x => x * 2);      // None

// FlatMap — chain an operation that itself returns a Maybe, without nesting.
Maybe<int> Parse(string s) =>
    int.TryParse(s, out var n) ? Maybe.Some(n) : Maybe<int>.None;

Maybe<int> good = Maybe.Some("42").FlatMap(Parse);           // Some(42)
Maybe<int> bad  = Maybe.Some("nope").FlatMap(Parse);         // None

// Where — keep the value only if it satisfies the predicate.
Maybe<int> even = Maybe.Some(4).Where(x => x % 2 == 0);      // Some(4)
Maybe<int> dropped = Maybe.Some(5).Where(x => x % 2 == 0);   // None
```

LINQ query syntax is supported through `Select` / `SelectMany`, and a single `None` anywhere in the chain empties the whole expression:

```csharp
var sum =
    from a in Maybe<int>.Some(2)
    from b in Maybe<int>.Some(3)
    select a + b;                                            // Some(5)

var missing =
    from a in Maybe<int>.Some(2)
    from b in Maybe<int>.None        // short-circuits here
    from c in Maybe<int>.Some(10)    // never evaluated
    select a + b + c;                                        // None
```

### JSON

`Maybe<T>` serializes via `System.Text.Json` as `{ "Value": x }` for `Some` and `{ "Value": null }` for `None`. Deserialization also accepts `{}` for `None`, so callers can omit the property entirely.

### Idiomatic usage: tri-state PATCH

HTTP `PATCH` has a long-standing modelling problem for nullable fields: a request needs to distinguish three intents — *don't touch this field*, *clear this field to null*, and *set it to a new value*. A plain `T?` collapses the first two cases. `Maybe<T>?` keeps them apart, because `Maybe<T>` itself is a value, so wrapping it in `T?` adds a real third state:

| JSON                     | Property value      | Intent                |
| ------------------------ | ------------------- | --------------------- |
| field omitted, or `null` | `null`              | leave field untouched |
| `{}` or `{"Value":null}` | `Maybe<T>.None`     | clear field to `null` |
| `{"Value":x}`            | `Maybe<T>.Some(x)`  | set field to `x`      |

The request DTO and PATCH handler then read straight off pattern matching, with no out-of-band sentinel values:

```csharp
public record PatchRequest(Maybe<string>? NullableValue);

[HttpPatch("{id:guid}")]
public async Task<IActionResult> Patch(Guid id, PatchRequest request)
{
    var entity = await Db.FindAsync<MyEntity>(id);
    if (entity is null) return NotFound();

    // request.NullableValue is null     → caller didn't send the field, skip.
    // request.NullableValue is { } nv   → caller sent it; nv.Value is the new
    //                                     string? (None unwraps to null, Some
    //                                     unwraps to the inner string).
    if (request.NullableValue is { } nv)
        entity.NullableValue = nv.Value;

    await Db.SaveChangesAsync();
    return Ok();
}
```

The `StrongTypes.Api` project in this repo uses exactly this pattern — see [`StructTypeEntityControllerBase.Patch`](src/StrongTypes.Api/Controllers/StructTypeEntityControllerBase.cs) and [`StructEntityPatchRequest`](src/StrongTypes.Api/Models/EntityModels.cs) for the production version that round-trips through both SQL Server and PostgreSQL.

## Helpful Types

### `NonEmptyString`

A string guaranteed to be non-null, non-empty, and not just whitespace. Construct it via the factory pair:

```csharp
// Returns null when the input is null/empty/whitespace — caller handles the null case.
NonEmptyString? maybe = NonEmptyString.TryCreate(input);

// Throws ArgumentException on invalid input.
NonEmptyString name = NonEmptyString.Create(input);
```

Or via the `AsNonEmpty()` extension on any `string?`:

```csharp
NonEmptyString? name = userInput.AsNonEmpty();
```

`NonEmptyString` exposes the common `string` surface (`Length`, `Contains`, `StartsWith`, `Substring`, `ToUpper`, etc.) and implicitly converts to `string`, so it drops into existing APIs without friction.

### Numeric wrappers

Four generic wrappers that enforce a sign invariant on any `INumber<T>` — `int`, `long`, `short`, `decimal`, `float`, `double`, and so on:

| Type              | Invariant                  |
| ----------------- | -------------------------- |
| `Positive<T>`     | strictly greater than zero |
| `NonNegative<T>`  | greater than or equal to zero |
| `Negative<T>`     | strictly less than zero    |
| `NonPositive<T>`  | less than or equal to zero |

Same factory pattern:

```csharp
Positive<int>?    p   = Positive<int>.TryCreate(quantity);
Positive<decimal> amt = Positive<decimal>.Create(price);

NonNegative<int>? age = NonNegative<int>.TryCreate(years);
```

Or via the `AsPositive()`, `AsNonNegative()`, `AsNegative()`, and `AsNonPositive()` extensions on any `INumber<T>` — mirroring `AsNonEmpty()` on `string?`. Each returns `null` when the invariant isn't met:

```csharp
Positive<int>?    p   = quantity.AsPositive();
NonNegative<int>? age = years.AsNonNegative();
Negative<int>?    debt = balance.AsNegative();
NonPositive<decimal>? loss = pnl.AsNonPositive();
```

When you'd rather fail loudly at the boundary than deal with `null`, the `To…` variants throw `ArgumentException` on invariant violation — same relationship as `Create` vs `TryCreate`:

```csharp
Positive<int>    p    = quantity.ToPositive();      // throws if quantity <= 0
NonNegative<int> age  = years.ToNonNegative();
Negative<int>    debt = balance.ToNegative();
NonPositive<decimal> loss = pnl.ToNonPositive();
```

The structs are laid out so that `default(Positive<T>)` still satisfies the invariant (e.g. `default(Positive<int>)` is `1`, not an invalid `0`), which means they survive zero-initialization without breaking their guarantee.

### What you get for free

Every strong type in this library implements the full set of equality and comparison interfaces, so you can drop them into dictionaries, sorted collections, LINQ `OrderBy`, and equality checks without writing any boilerplate:

- `IEquatable<T>` and the `==` / `!=` operators
- `IComparable<T>`, `IComparable`, and the `<`, `<=`, `>`, `>=` operators
- `GetHashCode` and `Equals(object?)` overrides consistent with value-based equality
- A sensible `ToString()` that returns the underlying value

### JSON serialization

All strong types ship with `System.Text.Json` converters attached via `[JsonConverter]`, so `JsonSerializer.Serialize(value)` and `JsonSerializer.Deserialize<T>(...)` just work — the wire format is the underlying primitive (`"hello"`, `42`, etc.), not an object with a `Value` property. Invalid input during deserialization surfaces as a `JsonException` at the boundary, which is where you want it.

### EF Core persistence

If you want to store strong types directly on your EF Core entities, add the companion package [`Kalicz.StrongTypes.EfCore`](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/). It provides the value converters needed to map `NonEmptyString`, `Positive<T>`, and friends to their underlying column types. See the package [readme](https://github.com/KaliCZ/StrongTypes/blob/main/src/StrongTypes.EfCore/readme.md) for setup details.

## Parsing helpers

### Enums

Extension members on any `enum` type give you cached metadata, factories, and flag helpers without the ceremony of calling `Enum.Parse`, `Enum.GetValues`, or writing your own caches. Everything hangs off the enum type itself, so you call `Roles.Parse(...)` rather than `EnumExtensions.Parse<Roles>(...)`.

```csharp
[Flags]
public enum Roles
{
    None   = 0,
    Reader = 1 << 0,
    Writer = 1 << 1,
    Admin  = 1 << 2,
}

// Factories, mirroring the framework's Parse/TryParse naming.
Roles  r1 = Roles.Parse("Reader");       // throws on failure
Roles? r2 = Roles.TryParse(userInput);   // null on failure
Roles? r3 = Roles.TryParse(userInput, ignoreCase: true);

// Same factories under the library's Create/TryCreate naming for
// consistency with NonEmptyString, Positive<T>, etc.
Roles  r4 = Roles.Create("Reader");
Roles? r5 = Roles.TryCreate(userInput);

// All declared members, cached on first read. Fine to call in hot paths.
IReadOnlyList<Roles> every = Roles.AllValues;  // [None, Reader, Writer, Admin]
```

For `[Flags]` enums you also get bit-level helpers. `AllFlagValues` gives you just the single-bit members (so `None = 0` and composite values are excluded), and `AllFlagsCombined` OR-s them together — perfect for seeding an "everything on" value at runtime without having to remember to update a `SuperAdmin = Reader | Writer | Admin` literal every time you add a flag.

```csharp
IReadOnlyList<Roles> flags = Roles.AllFlagValues;     // [Reader, Writer, Admin]
Roles                super = Roles.AllFlagsCombined;  // Reader | Writer | Admin

// Decompose a value into the single-bit flags it contains, in declaration order.
Roles user = Roles.Reader | Roles.Admin;
foreach (var flag in user.GetFlags())
{
    // flag is Reader, then Admin
}
```

The flag helpers throw `InvalidOperationException` if the enum isn't marked `[Flags]`, so a typo at the declaration fails loudly at the first call instead of silently returning the wrong thing.

### Strings

A small set of extension methods over `string?` for safe, nullable-returning parses:

```csharp
NonEmptyString? name = userInput.AsNonEmpty();
int?            id   = queryParam.AsInt();
decimal?        amt  = body.AsDecimal();
DateTime?       when = header.AsDateTime();
Roles?          role = header.AsEnum<Roles>();
```

Each `As*` helper has a `To*` sibling that throws instead of returning `null` — pick the one that matches how you want to handle bad input at the call site:

```csharp
NonEmptyString name = userInput.ToNonEmpty();   // throws ArgumentException
int            id   = queryParam.ToInt();       // throws FormatException / OverflowException
Roles          role = header.ToEnum<Roles>();   // throws ArgumentException
```

`AsEnum<TEnum>` / `ToEnum<TEnum>` are plain extensions on `string?` that sidestep a C# limitation: because `Roles.TryParse(...)` is an extension member on the enum type, it can't be called through an open generic `TEnum` parameter. These close the gap so you can parse an enum whose type you only know generically.

## Legacy types (to be replaced)

> [!WARNING]
> The types in this section are inherited from FuncSharp and will be removed in a future release. They are kept for now so existing code keeps compiling, but new code should avoid them.

### `Option<A>`

An `Option<A>` represents a value that may or may not be available. In modern C# with nullable reference types enabled, `T?` already covers this case at the language level, so `Option<A>` has become redundant.

> [!WARNING]
> `Option<A>` is superseded by [`Maybe<T>`](#maybet). New code should use `Maybe<T>`; `Option<A>` will be removed in a future release.

### `Try<A, E>`

`Try<A, E>` represents the result of an operation that can end in either success (`A`) or error (`E`), making the failure path explicit in the type signature instead of hiding it behind exceptions.

> [!WARNING]
> `Try<A, E>` will be replaced by a modern `Result<T, E>` implementation that supports pattern matching.

### `Coproduct`

`Coproduct[N]<T1, …, TN>` is a sum type (tagged union) representing exactly one of N alternatives. Useful for modelling "either-or" outcomes where an abstract class hierarchy would be too loose.

> [!WARNING]
> `Coproduct` will be replaced by a more modern `OneOf` implementation with first-class pattern matching support.

## Acknowledgments

This library is the continuation of [FuncSharp](https://github.com/MewsSystems/FuncSharp) by [Honza Siroky](https://github.com/siroky), bringing the concepts into modern C#. Licensed under the [MIT License](license.txt).
