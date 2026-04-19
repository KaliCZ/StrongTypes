# StrongTypes - Stronger Typing for C#

> **Work in Progress** - This repository is based of [FuncSharp](https://github.com/MewsSystems/FuncSharp), originally written by [Honza Siroky](https://github.com/siroky). The goal of StrongTypes is to target .NET 10 with nullable reference types enabled, leveraging modern C# language features while providing extra types that enable a better developer experience.

[![Build](https://img.shields.io/github/actions/workflow/status/KaliCZ/StrongTypes/build.yml?branch=main&label=build)](https://github.com/KaliCZ/StrongTypes/actions/workflows/build.yml)
[![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)
[![NuGet Version](https://img.shields.io/nuget/v/Kalicz.StrongTypes)](https://www.nuget.org/packages/Kalicz.StrongTypes/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes)](https://www.nuget.org/packages/Kalicz.StrongTypes/)

StrongTypes adds small, focused value types to C# that make everyday code safer and more expressive — things like "a string that is never empty" or "an integer that is always positive". Instead of validating the same invariant at every call site, you validate once at the boundary and pass the strong type onwards. The compiler then guarantees the invariant holds wherever that type appears.

## Contents

- [Strong value types](#strong-value-types)
  - [`NonEmptyString`](#nonemptystring)
  - [Numeric wrappers: `Positive<T>`, `NonNegative<T>`, `Negative<T>`, `NonPositive<T>`](#numeric-wrappers)
  - [What you get for free](#what-you-get-for-free)
  - [JSON serialization](#json-serialization)
  - [EF Core persistence](#ef-core-persistence)
- [Parsing helpers](#parsing-helpers)
- [Legacy types (to be replaced)](#legacy-types-to-be-replaced)
  - [`Option<A>`](#optiona)
  - [`Try<A, E>`](#trya-e)
  - [`Coproduct`](#coproduct)

## Strong value types

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

A small set of extension methods over `string?` for safe, nullable-returning parses:

```csharp
NonEmptyString? name = userInput.AsNonEmpty();
int?            id   = queryParam.AsInt();
decimal?        amt  = body.AsDecimal();
DateTime?       when = header.AsDateTime();
```

Plus extensions on enums for cached, allocation-free metadata and `TryCreate`/`Create` factories that match the rest of the library:

```csharp
Status? parsed = EnumExtensions.TryCreate<Status>(input);
var all        = EnumExtensions.AllValues<Status>();  // cached
```

## Legacy types (to be replaced)

> [!WARNING]
> The types in this section are inherited from FuncSharp and will be removed in a future release. They are kept for now so existing code keeps compiling, but new code should avoid them.

### `Option<A>`

An `Option<A>` represents a value that may or may not be available. In modern C# with nullable reference types enabled, `T?` already covers this case at the language level, so `Option<A>` has become redundant.

> [!WARNING]
> `Option<A>` will be replaced by a modern `Maybe<T>` implementation that supports pattern matching and integrates cleanly with nullable reference types.

### `Try<A, E>`

`Try<A, E>` represents the result of an operation that can end in either success (`A`) or error (`E`), making the failure path explicit in the type signature instead of hiding it behind exceptions.

> [!WARNING]
> `Try<A, E>` will be replaced by a modern `Result<T, E>` implementation that supports pattern matching.

### `Coproduct`

`Coproduct[N]<T1, …, TN>` is a sum type (tagged union) representing exactly one of N alternatives. Useful for modelling "either-or" outcomes where an abstract class hierarchy would be too loose.

> [!WARNING]
> `Coproduct` will be replaced by a more modern `OneOf` implementation with first-class pattern matching support.

## Acknowledgments

This library is based on [FuncSharp](https://github.com/MewsSystems/FuncSharp) by [Honza Siroky](https://github.com/siroky). Licensed under the [MIT License](license.txt).
