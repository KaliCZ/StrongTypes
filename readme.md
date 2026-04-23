# StrongTypes for C# [![Build](https://img.shields.io/github/actions/workflow/status/KaliCZ/StrongTypes/build.yml?branch=main&label=build)](https://github.com/KaliCZ/StrongTypes/actions/workflows/build.yml) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes/) [![StrongTypes downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes?label=downloads%20%28StrongTypes%29)](https://www.nuget.org/packages/Kalicz.StrongTypes/) [![StrongTypes.EfCore downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.EfCore?label=downloads%20%28StrongTypes.EfCore%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/) [![StrongTypes.FsCheck downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.FsCheck?label=downloads%20%28StrongTypes.FsCheck%29)](https://www.nuget.org/packages/Kalicz.StrongTypes.FsCheck/)

StrongTypes is not an attempt to build a full algebraic type system on top of C#. It adds small, focused value types that make everyday code safer and more expressive — things like "a string that is never empty" or "an integer that is always positive".

Every value-carrying type ships with `System.Text.Json` converters wired up out of the box (the one exception is `Result`, which stays in-process), so validation runs at the wire boundary during deserialization without any extra setup.

## Contents

- [Helpful Types](#helpful-types)
  - [`NonEmptyString`](#nonemptystring)
  - [Numeric wrappers: `Positive<T>`, `NonNegative<T>`, `Negative<T>`, `NonPositive<T>`](#numeric-wrappers)
  - [What you get for free](#what-you-get-for-free)
  - [JSON serialization](#json-serialization)
  - [EF Core persistence](#ef-core-persistence)
- [`NonEmptyEnumerable<T>`](#nonemptyenumerablet)
- [Parsing helpers](#parsing-helpers)
  - [Enums](#enums)
  - [Strings](#strings)
- [Algebraic types](#algebraic-types)
  - [Prefer nullables: `Map`, `MapTrue`, `MapFalse`](#prefer-nullables-map-maptrue-mapfalse)
  - [`Maybe<T>`](#maybet)
  - [`Result<T, TError>`](#resultt-terror)

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

`NonEmptyString` exposes the common `string` surface (`Length`, `Contains`, `StartsWith`, `Substring`, `ToUpper`, …) and implicitly converts to `string`.

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

`default(Positive<T>)` still satisfies the invariant (e.g. `default(Positive<int>)` is `1`, not an invalid `0`), so the structs survive zero-initialization without breaking their guarantee.

### What you get for free

Every strong type in this library implements the full set of equality and comparison interfaces, so you can drop them into dictionaries, sorted collections, LINQ `OrderBy`, and equality checks without writing any boilerplate:

- `IEquatable<T>` and the `==` / `!=` operators
- `IComparable<T>`, `IComparable`, and the `<`, `<=`, `>`, `>=` operators
- `GetHashCode` and `Equals(object?)` overrides consistent with value-based equality
- A sensible `ToString()` that returns the underlying value

### JSON serialization

All strong types ship with `System.Text.Json` converters attached via `[JsonConverter]` — no converter registration and no custom `JsonSerializerOptions` required. The wire format is the underlying primitive (`"hello"`, `42`, …), not an object with a `Value` property, and invalid input surfaces as a `JsonException` at the boundary.

`Result<T, TError>` is the one exception: it has no converter, because serializing a two-branch union over the wire doesn't have a single sensible shape and hasn't been a real-world need. Unwrap it (`.Match(...)`, `.ThrowIfError()`, `.Success`/`.Error`, …) before sending anything across a JSON boundary.

### EF Core persistence

If you want to store strong types directly on your EF Core entities, add the companion package [`Kalicz.StrongTypes.EfCore`](https://www.nuget.org/packages/Kalicz.StrongTypes.EfCore/). It provides the value converters needed to map `NonEmptyString`, `Positive<T>`, and friends to their underlying column types. See the package [readme](https://github.com/KaliCZ/StrongTypes/blob/main/src/StrongTypes.EfCore/readme.md) for setup details.

## `NonEmptyEnumerable<T>`

A read-only sequence guaranteed to contain at least one element. The non-empty invariant is enforced at construction and travels through operations that preserve it (`Select`, `SelectMany`, `Distinct`, `Concat`), so `.Head` is always defined — no empty-collection check needed (the value itself can still be `null` when `T` is nullable).

```csharp
var list = NonEmptyEnumerable.Create(1, 2, 3);

NonEmptyEnumerable<int> list = [1, 2, 3];

// CreateRange for runtime sequences (List<T>, LINQ queries, …).
NonEmptyEnumerable<int>  throws   = NonEmptyEnumerable.CreateRange(source);      // throws on empty/null
NonEmptyEnumerable<int>? nullable = NonEmptyEnumerable.TryCreateRange(source);   // null on empty/null
```

Or via extensions on any `IEnumerable<T>?`:

```csharp
NonEmptyEnumerable<int>? maybe = values.AsNonEmpty();   // null on empty/null
NonEmptyEnumerable<int>  must  = values.ToNonEmpty();   // throws on empty/null
```

Access the non-emptiness directly:

```csharp
int                head  = list.Head;    // always defined (may itself be null if T is nullable)
IReadOnlyList<int> tail  = list.Tail;    // everything after Head
int                count = list.Count;   // always >= 1
```

LINQ operations that preserve the invariant return `NonEmptyEnumerable<TResult>`, so the guarantee doesn't decay through a chain:

```csharp
NonEmptyEnumerable<int>    doubled  = list.Select(x => x * 2);
NonEmptyEnumerable<int>    distinct = list.Distinct();
NonEmptyEnumerable<string> allTags  = pages.SelectMany(p => p.Tags);   // p.Tags is itself non-empty
NonEmptyEnumerable<int>    extended = list.Concat(10, 20);
NonEmptyEnumerable<int>    reversed = list.Reverse();
NonEmptyEnumerable<int>    withEnds = list.Prepend(0).Append(99);
NonEmptyEnumerable<int>    combined = 1.Concat(existing, more);        // head + N tails → guaranteed non-empty
```

Operations whose result can be empty (`Where`, `Skip`, `GroupBy`, …) fall through to plain LINQ and return `IEnumerable<T>`. Re-wrap with `AsNonEmpty()` / `ToNonEmpty()` at the point where you need the guarantee again.

Non-emptiness is also exactly the precondition LINQ's aggregate helpers need. The overloads on `NonEmptyEnumerable<T>` are total — they never throw `InvalidOperationException` on empty input and, for value types, return `T` directly instead of `T?`:

```csharp
int max  = list.Max();                 // never throws, returns int (not int?)
int min  = list.Min();
int last = list.Last();
int sum  = list.Aggregate((a, b) => a + b);
int avg  = list.Average();
```

### `INonEmptyEnumerable<T>` (covariant interface)

`NonEmptyEnumerable<T>` implements `INonEmptyEnumerable<out T>`, a covariant interface — use it when you need to assign a more-derived collection to a less-derived reference:

```csharp
NonEmptyEnumerable<Dog>      dogs    = [new Dog()];
INonEmptyEnumerable<Animal>  animals = dogs;  // allowed thanks to `out T`
```

All extensions (`Select`, `Concat`, `Max`, `Last`, …) have overloads on both the concrete type and the interface, so either receiver type works in a chain.

### JSON

Serializes as a JSON array; an empty JSON array is rejected with `JsonException`. The converter is attached via `[JsonConverter]`, so no registration or custom `JsonSerializerOptions` is needed. `NonEmptyEnumerable<T?>` accepts JSON nulls as legitimate elements — `[1, null, 3]` round-trips faithfully into `NonEmptyEnumerable<int?>`.

> ⚠ **Null elements in reference-typed collections** — a JSON array like `[null]` deserializes successfully into `NonEmptyEnumerable<string>` or `NonEmptyEnumerable<NonEmptyString>` even though the element type isn't annotated nullable. The same would happen with a plain `List<string>`.

The same converter also serves `INonEmptyEnumerable<T>`, so properties typed as the interface round-trip the same way — deserialization still produces a concrete `NonEmptyEnumerable<T>` behind the interface reference.

## Parsing helpers

### Enums

Extension members on any `enum` type give you cached metadata, factories, and flag helpers. Everything hangs off the enum type itself, so you call `Roles.Parse(...)` rather than `EnumExtensions.Parse<Roles>(...)`.

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

For `[Flags]` enums you also get bit-level helpers. `AllFlagValues` lists just the single-bit members (excluding `None = 0` and composites), `AllFlagsCombined` OR-s them into an "everything on" value so you don't have to maintain a `SuperAdmin = Reader | Writer | Admin` literal.

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

`AsEnum<TEnum>` / `ToEnum<TEnum>` work through an open generic `TEnum` parameter, which the `Roles.TryParse(...)` extension member can't — use them when you only know the enum type generically.

## Algebraic types

StrongTypes is not an attempt to build a full algebraic type system. The purpose of these types is just to help where C# functionality is lacking, not to invent a framework and work fully in the algebraic types.

These types enable quite a few simplifications when it comes to parsing and validations. But I wouldn't recommend building the whole app by composing them. They're meant to bridge small local pieces of the application. Let's start by introducing some functionality so we don't need the algebraic types in the first place.

### Prefer nullables: `Map`, `MapTrue`, `MapFalse`

C# already lets you read through a null — `user?.Name?.Trim()` short-circuits without a single `if`. What was missing was *passing* a nullable into a function that expects the non-null form (e.g. a constructor). Historically that meant a ternary at every call, which is hard to chain and clutters up any expression it appears in:

```csharp
// Before — one ternary for every step
MailAddress? email = text is null ? null : new MailAddress(text);
```

`Map` on `T?` and `MapTrue` / `MapFalse` on `bool` close those gaps with a single method call. The mapper only runs when the input is present (or the bool matches), and the `null` short-circuit is implicit:

```csharp
MailAddress? email = text.Map(t => new MailAddress(t));
int? doubled = maybeInt.Map(x => x * 2);
someResult? something = featureFlagEnabled.MapTrue(CallSomeService);
// instead of
someResult? something = null;
if (featureFlagEnabled)
    someResult = CallSomeService();
```

So *you don't need `Maybe<T>` just to compose an optional logic*. With `Map` in the toolbox, plain `T?` covers most cases. Save `Maybe<T>` for the cases where nullability can't express what you need — see the HTTP PATCH example below.

And when you already have a `Maybe<T>` or some other wrapper, you can step back out into nullable-land by just using the inner value — `Maybe<T>.Value` is itself a `T?`, so the same `Map` works:

```csharp
Maybe<string> maybeName  = LookupName(id);
string?       normalized = maybeName.Value.Map(n => n.Trim().ToUpperInvariant());
// Or alternatively with standard C#
string? normalized = maybeName.Value is {} n
    ? n.Trim().ToUpperInvariant()
    : null;
```

> [!WARNING]
> `Map` / `MapTrue` / `MapFalse` are slower than the equivalent ternary. The mapper is passed as a delegate, so the JIT has to go through a function-pointer invocation instead of the direct branch it gets from a `?:`. Prefer the ternary on hot paths; reach for `Map` where readability matters more than the nanoseconds.

### `Maybe<T>`

A value type that holds either a value of `T` (`Some`) or no value (`None`). Works for both reference and value types and integrates with collection expressions, LINQ, pattern matching, and `System.Text.Json`.

The generic constraint is `where T : notnull` — `Maybe<int?>` and `Maybe<string?>` are deliberately disallowed, because permitting a nullable `T` would collapse the `None` and `Some(null)` cases and break the `is { } v` unwrap pattern. (see more below)

#### Why? HTTP PATCH with optional properties

HTTP `PATCH` has a long-standing modelling problem for nullable fields: a request needs to distinguish three intents — *don't touch this field*, *clear this field to null*, and *set it to a new value*. A plain `T?` collapses the first two cases. `Maybe<T>?` keeps them apart, because `Maybe<T>` itself is a value, so wrapping it in `T?` adds a real third state:

| JSON                     | Property value     | Intent                |
| ------------------------ |--------------------| --------------------- |
| field omitted, or `null` | `(Maybe<T>?)null`  | leave field untouched |
| `{}` or `{"Value":null}` | `Maybe<T>.None`    | clear field to `null` |
| `{"Value":x}`            | `Maybe<T>.Some(x)` | set field to `x`      |

The request DTO and PATCH handler then read straight off pattern matching, with no out-of-band sentinel values:

```csharp
public record PatchRequest(
    Maybe<string>? NullableValue
);

[HttpPatch("{id:guid}")]
public async Task<IActionResult> Patch(Guid id, PatchRequest request)
{
    var entity = await Db.FindAsync<MyEntity>(id);
    if (entity is null) return NotFound();

    // null means the property was skipped. Empty means it's deliberaly set to null.
    if (request.NullableValue is { } nv)
        entity.NullableValue = nv.Value;

    await Db.SaveChangesAsync();
    return Ok();
}
```

The `StrongTypes.Api` project in this repo uses exactly this pattern — see [`StructTypeEntityControllerBase.Patch`](src/StrongTypes.Api/Controllers/StructTypeEntityControllerBase.cs) and [`StructEntityPatchRequest`](src/StrongTypes.Api/Models/EntityModels.cs) for the production version that round-trips through both SQL Server and PostgreSQL.

#### Creating

```csharp
Maybe<int>    some   = Maybe.Some(42);   // T inferred from the argument
Maybe<int>    direct = 42;               // implicit conversion from T
Maybe<string> none   = Maybe.None;       // binds to whatever Maybe<T> the context expects
Maybe<int>    a      = nullableInt.ToMaybe();      // Some(x) when HasValue, None otherwise
Maybe<string> b      = nullableString.ToMaybe();   // Some(x) when not null, None otherwise
```

The implicit conversions from `T` and from the untyped `Maybe.None` let collection expressions mix plain values, `None` markers, and spread sequences without spelling out `Maybe<int>.Some(...)` on every element:

```csharp
int[] middle = [4, 2, 3];
Maybe<int>[] xs = [..middle, Maybe.None, 4];
IEnumerable<int> values = xs.Values();   // [4, 2, 3, 4]
```

#### Unwrapping

The idiomatic "has value" check uses the `is { } v` pattern on the `Value` extension property — `Value` returns `Nullable<T>` for value types and `T?` for reference types, and the pattern unwraps to the underlying `T` directly:

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

#### Composition

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

#### JSON

`Maybe<T>` serializes via `System.Text.Json` as `{ "Value": x }` for `Some` and `{ "Value": null }` for `None` — no converter registration or custom `JsonSerializerOptions` needed. Deserialization also accepts `{}` for `None`, so callers can omit the property entirely.

### `Result<T, TError>`

`Result<T, TError>` is a value that is *either* a success carrying a `T` *or* an error carrying a `TError` — making the failure path explicit in the type signature instead of hiding it behind exceptions. `Result<T>` is shorthand for `Result<T, Exception>`, so signatures can read `public Result<User> Load(...)` without naming the error type.

Unlike the other strong types, `Result` has no JSON converter — a two-branch union doesn't have a single natural on-the-wire shape, and no concrete use case has called for one yet. Treat `Result` as an in-process return type: unwrap it (`.Match(...)`, `.ThrowIfError()`, `.Success`/`.Error`, …) before serializing anything across an HTTP boundary.

#### Construction

Returning a `Result` is as simple as returning either branch — implicit operators handle the wrapping on both sides:

```csharp
public Result<int, string> Parse(string s) =>
    int.TryParse(s, out var n) ? n : "not a number";
```

Explicit factories are there when type inference needs a nudge:

```csharp
var ok  = Result.Success<int, string>(42);
var err = Result.Error<int, string>("bad");
```

#### Inspection

Pattern matching with `is { } v` unwraps the inner value in one expression:

```csharp
Result<int, string> r = Parse(input);

if (r.Success is { } value) Console.WriteLine($"got {value}");
if (r.Error   is { } msg)   Console.WriteLine($"failed: {msg}");
```

`IsSuccess` / `IsError` are there too when you don't need the payload.

When the error branch already carries an exception, `ThrowIfError` unwraps the success value and rethrows otherwise, preserving the original stack trace via `ExceptionDispatchInfo`:

```csharp
string contents = Result.Catch(() => File.ReadAllText(path)).ThrowIfError();
```

When `TError` is not an `Exception`, the two-parameter overload builds one from the error value — `r.ThrowIfError(e => new InvalidOperationException(e))`. For a `Result<T, IReadOnlyList<Exception>>` — the shape validation pipelines converge on after aggregating multiple failed steps — `ThrowIfError` rethrows a single exception directly and wraps multiples in an `AggregateException`.

#### Transformation

`Map`, `MapError`, `Match`, and `FlatMap` let you chain without explicit branching:

```csharp
Result<int, string> r = Parse("42");

// Map — transform the success value; errors pass through unchanged.
Result<int, string> doubled = r.Map(x => x * 2);

// Match — fold both branches into a single value.
string message = r.Match(
    success: x => $"got {x}",
    error:   e => $"oops: {e}");

// FlatMap — chain an operation that itself returns a Result.
Result<int, string> positive = r.FlatMap<int>(x =>
    x > 0 ? x : "must be positive");
```

`Match` exists because the natural-looking C# form — `r switch { T v => …, TError e => … }` — isn't possible without native discriminated unions. Type patterns dispatch on the *runtime* type of `r`, and a `Result<T, TError>` is never actually a `T` or a `TError` instance, so those arms never fire. Until the language ships DUs (proposed for .NET 11), `Match` is the only form that folds both branches with a compile-time guarantee that you've handled each one.

Every sync method has an async counterpart (`MapAsync`, `FlatMapAsync`, `MatchAsync`, …).

#### Wrapping exceptions

`Result.Catch` captures a throwing call without writing `try/catch`:

```csharp
Result<string> contents = Result.Catch(() => File.ReadAllText(path));
```

`Catch<T, TException>` restricts which exception type is captured — picking a non-cancellation type lets `OperationCanceledException` flow past, which is the opt-in for cancellation-aware pipelines.

#### Combining validations

`Result.Aggregate` combines multiple results, collecting *every* error (not just the first) — which is what you want when validating an input:

```csharp
record User(NonEmptyString Name, Positive<int> Age);

Result<User, string> ParseUser(string? nameInput, int ageInput)
{
    Result<NonEmptyString, string> name = nameInput.AsNonEmpty().ToResult("name must not be empty");
    Result<Positive<int>, string>  age  = ageInput.AsPositive().ToResult("age must be positive");

    return Result.Aggregate(name, age, (n, a) => new User(n, a), errors => string.Join("; ", errors));
}
```

`.AsNonEmpty()` / `.AsPositive()` return a nullable of the validated type; `.ToResult("…")` lifts that nullable into a `Result<T, string>` with the given error on null. The final `errorMap` parameter on `Aggregate` folds the collected `string[]` into a single message — the same thing you'd otherwise write as a chained `.MapError(...)`.

If both inputs are invalid, the aggregated error carries both messages:

```csharp
ParseUser("", -5);   // Error: "name must not be empty; age must be positive"
```

You can aggregate up to 8 results directly, or pass an `IEnumerable` when the count is dynamic — useful for validating a list of inputs:

```csharp
Result<int, string> ParseQuantity(string raw) =>
    int.TryParse(raw, out var n) && n > 0 ? n : $"'{raw}' is not a positive integer";

Result<int[], string> ParseQuantities(IEnumerable<string> inputs) =>
    Result.Aggregate(inputs.Select(ParseQuantity), errors => string.Join("; ", errors));
```

The combiner overload (shown in the `ParseUser` example) returns a built object directly; a tuple-returning form is also provided when there's no natural constructor to call.

> [!NOTE]
> **No discriminated union / `OneOf` type is included.** I didn't see a reason to reinvent one — [`mcintyre321/OneOf`](https://github.com/mcintyre321/OneOf) or [`domn1995/dunet`](https://github.com/domn1995/dunet) already cover this space well, and .NET 11 is expected to introduce native discriminated unions at the language level. If you have a concrete use case where neither option works for you, please [open a GitHub issue](https://github.com/KaliCZ/StrongTypes/issues) and let me know.

## Acknowledgments

This library is the continuation of [FuncSharp](https://github.com/MewsSystems/FuncSharp) by [Honza Siroky](https://github.com/siroky), bringing the concepts into modern C#. Licensed under the [MIT License](license.txt).
