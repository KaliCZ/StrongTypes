---
name: strongtypes
description: Write idiomatic Kalicz.StrongTypes code (NonEmptyString, numeric wrappers, NonEmptyEnumerable, Maybe<T>, Result<T, TError>, EF Core and FsCheck integrations). Invoke when editing C# files that import the StrongTypes namespace, when designing DTOs / service signatures in projects that use the Kalicz.StrongTypes NuGet package, or when deciding between plain T?, Maybe<T>, and Result<T, TError>.
---

# StrongTypes — skill

This skill teaches an AI agent to use the `Kalicz.StrongTypes` C# library
idiomatically. Use it in any C# project that depends on `Kalicz.StrongTypes`
(and optionally `Kalicz.StrongTypes.EfCore` / `Kalicz.StrongTypes.FsCheck`).

## What StrongTypes is

`Kalicz.StrongTypes` is a small C# library of focused value wrappers and
algebraic types that push invariants into the type system. Core ideas:

- **Make invalid states unrepresentable at the boundary.** Once a string
  becomes a `NonEmptyString`, no downstream code has to re-check that it is
  non-null / non-whitespace. Same for `Positive<int>`, `NonEmptyEnumerable<T>`,
  etc.
- **Fail at the edge.** All wrapped types ship with `System.Text.Json`
  converters, so bad input surfaces as a `JsonException` before your endpoint
  method runs — not deep in a service call.
- **Interoperate with plain C#.** These types implement `IEquatable`,
  `IComparable`, expose implicit conversions where safe, and — critically —
  are designed to sit next to ordinary `string?` / `int` / `T?` without
  demanding a wholesale functional rewrite.

StrongTypes is *not* trying to be an F#-style algebraic language on top of
C#. `Maybe<T>` and `Result<T, TError>` exist for the few places where plain
C# genuinely can't express what you want. The rest of your code should still
look like idiomatic modern C# with nullable reference types.

## Packages

| Package                       | What it gives you                                                                                                  |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `Kalicz.StrongTypes`          | The core types (`NonEmptyString`, numeric wrappers, `NonEmptyEnumerable<T>`, `Maybe<T>`, `Result<T, TError>`, …).  |
| `Kalicz.StrongTypes.EfCore`   | EF Core value converters + a `.Unwrap()` LINQ translator so strong types sit directly on entity properties.        |
| `Kalicz.StrongTypes.FsCheck`  | FsCheck `Arbitrary<T>` generators — one `[Properties(Arbitrary = new[] { typeof(Generators) })]` and you're done.  |

Install the core package first. Add EfCore / FsCheck only when you hit those
stacks.

## When to reach for StrongTypes

Reach for it when:

- A primitive parameter has an invariant ("must be non-empty", "must be
  positive", "list must have at least one item"). Use the matching wrapper
  at the API / DTO boundary.
- You want JSON to reject bad input for you at deserialization time.
- You have a three-state update field (skip / clear / set). The HTTP
  `PATCH` DTO is the textbook case, but the same pattern applies to
  any in-process update method whose argument needs to mean "don't
  touch this", "clear this", or "set this to a value" — no HTTP
  required. That is the canonical `Maybe<T>?` case — see the
  design-philosophy section.
- A method's failure mode needs to be visible at the call site, translated
  to a user-facing error, or aggregated with other failures. Use
  `Result<T, TError>` — usually `TError` is an enum.

Do **not** reach for it when:

- Plain nullable (`string?`, `int?`, `T?`) already captures the state. A
  DTO field that means "no value" is `T?`, not `Maybe<T>`.
- You'd be writing `Result<T, ErrorEnum>` with a single error value and no
  caller that consumes it differently from an exception. A failing parse
  on user input that immediately becomes a 400 is cleaner as
  `input.AsNonEmpty() is not { } name` and returning early — no `Result`
  needed.

## Design philosophy — picking the right wrapper

This is the most important section. Most misuses of StrongTypes come from
reaching for `Maybe<T>` or `Result<T, TError>` when a plain `T?` would do.
Read the decision tree before writing a new DTO field or service signature.

### Decision tree for "optional / nullable" fields

1. **Can the field be absent, but never be explicitly cleared?** → use `T?`.
   Example: an `OrderUpdate` DTO's `Price` property. Null means "don't touch
   the price". You can't "remove" a price, so there are only two states,
   and `decimal?` captures them both.

2. **Can the field be absent *and* be explicitly cleared to null?** → use
   `Maybe<T>?`. The pattern applies to *any* update operation that needs
   those three states, not just HTTP. In a DTO, a service signature, a
   message payload, a builder parameter — wherever "don't touch",
   "clear", and "set" all need to coexist:
   - `null` → skip; the caller did not supply this field. Leave the
     existing value alone.
   - `Maybe<T>.None` → the caller asked to clear the field to null.
   - `Maybe.Some(x)` → the caller asked to set the field to `x`.

   The HTTP `PATCH` DTO is the most visible case (because JSON
   distinguishes "property omitted" from "property sent as null"), but
   the exact same argument applies to, e.g., an `ApplyChanges` service
   method called from another service: three distinct intents, three
   distinct states, one `Maybe<T>?` parameter.

3. **Is the field always present and meaningful?** → the bare strong type
   (`NonEmptyString`, `Positive<int>`, …). No nullable wrapping.

```csharp
public record OrderUpdate(
    decimal?         Price,          // optional update; cannot be cleared
    Maybe<string>?   Nickname,       // three-state: skip / clear / set
    NonEmptyString   OrderCode       // always required
);
```

### Decision tree for validation / parsing results

1. **Is this a single-reason validation where the caller turns the failure
   straight into an HTTP 400 or an exception?** → return `T?` from
   `TryCreate` / `As…`. Caller unwraps with `is not { } v`:

   ```csharp
   public IActionResult CreateUser(string? nameInput)
   {
       if (nameInput.AsNonEmpty() is not { } name)
           return BadRequest("name must not be empty");

       // 'name' is NonEmptyString from here on — the whole service
       // call tree now has a typed, validated name.
       return Ok(_service.Create(name));
   }
   ```

   Every downstream call sees `NonEmptyString`, not `string`. No `Result`
   is needed because the caller already knows *why* the parse failed —
   the validation rule is baked into the wrapper's name.

2. **Does the caller need to distinguish between multiple failure
   reasons, translate them to user-facing codes, or aggregate them with
   other failures?** → return `Result<T, TError>`. `TError` is normally an
   enum, occasionally a string if you don't need localisation.

   ```csharp
   public enum OrderError { PaymentFailed, OutOfStock, InvalidAddress }

   public Result<Order, OrderError> CreateOrder(OrderData data) { ... }
   ```

3. **Does an exception fit the failure better?** → use `Create` /
   `ToNonEmpty` / `ToPositive`, which throw `ArgumentException`. Don't
   invent a `Result<Order, Exception>` to smuggle an exception through.

### Summary rule

> **`T?` is the default.** Reach for `Maybe<T>?` only when the field
> genuinely has three states (skip / clear / set) — anywhere in the
> system, not just PATCH DTOs. Reach for `Result<T, TError>` only when
> the caller needs the error *reason*, not just the fact of failure.
> Everything else is a primitive or a strong wrapper.

### Why not just return `Result` from every validator?

Because the caller already has the reason:

- The rule is encoded in the method name (`AsNonEmpty` means "empty or
  whitespace failed").
- The caller that consumes the failure usually wants to turn it into a
  400 response, a `ModelState` error, or a thrown exception — and they
  already know which. Returning a `Result<T, SomeEnum>` just to put
  *one* possible error in the enum is noise.
- `Result` shines when you want to **aggregate** multiple validation
  outcomes and report them together. That's `Result.Aggregate(...)`,
  which is a deliberate top-level use case, not a default pattern.

### Result flow in practice

Services return `Result<T, TError>`. Controllers *consume* those results
and turn them into HTTP responses, but controllers themselves rarely
need to *construct* a `Result` — if a controller-level check fails, it
just returns a `BadRequest(...)` directly.

```csharp
// Controller: consume service Results; do local validation with T?.
[HttpPost]
public async Task<IActionResult> Create(CreateRequest request)
{
    if (request.Name.AsNonEmpty() is not { } name)
        return BadRequest("name required");

    Result<Order, OrderError> result = await _orders.Create(name, request.Items);
    if (result.Error is { } e)
        return Problem(MapError(e));

    return Created("...", result.Success);
}
```

## Implicit operators — prefer them over explicit factories

Every wrapper in the library ships implicit operators that let you drop a
plain value into a wrapper slot without naming the wrapper type. Use them.
They are shorter, they help type inference, and they avoid spelling out
generic parameters twice.

### Reference table

| From              | To                     | Operator   | Example                                                       |
| ----------------- | ---------------------- | ---------- | ------------------------------------------------------------- |
| `NonEmptyString`  | `string`               | implicit   | `string s = name;`                                            |
| `string`          | `NonEmptyString`       | explicit   | `(NonEmptyString)s` — throws if invalid; prefer `AsNonEmpty()`|
| `Positive<T>`     | `T`                    | implicit   | `int i = positive;` (all numeric wrappers)                    |
| `T`               | `Positive<T>`          | explicit   | `(Positive<int>)42` — throws; prefer `AsPositive()`           |
| `Digit`           | `byte`, `int`          | implicit   | `int d = digit;`                                              |
| `T`               | `Maybe<T>`             | implicit   | `Maybe<int> m = 42;`                                          |
| `Maybe.None`      | any `Maybe<T>`         | implicit   | `Maybe<int> m = Maybe.None;`                                  |
| `T`               | `Result<T, TError>`    | implicit   | `return value;` (the success branch)                          |
| `TError`          | `Result<T, TError>`    | implicit   | `return OrderError.PaymentFailed;`                            |
| `T`               | `Result<T>`            | implicit   | `return value;`                                               |
| `Exception`       | `Result<T>`            | implicit   | `return new InvalidOperationException(...);`                  |

### Return sites — just `return`

Do **not** write `Result<T, TError>.Success(value)` or
`Result.Success<T, TError>(value)` when the method already declares the
return type. Type inference picks the right branch off the implicit
operators:

```csharp
// Correct — implicit operators do the wrapping.
public Result<Order, OrderError> Place(OrderData data)
{
    if (data.Items.Count == 0)
        return OrderError.EmptyCart;                // → error branch

    return new Order(data);                         // → success branch
}

// Ternary also works.
public Result<int, string> Parse(string s)
    => int.TryParse(s, out var n) ? n : "not a number";
```

The explicit factories (`Result.Success<T, TError>(value)` /
`Result.Error<T, TError>(error)`) are a fallback for the occasional spot
where inference can't pick a branch — for example a ternary inside a
`var`-typed local where `T` and `TError` happen to be the same type. Reach
for them only when the compiler actually complains.

### `Maybe<T>` follows the same rule

```csharp
// Preferred
Maybe<int> some = 42;
Maybe<int> none = Maybe.None;

// Usually unnecessary
Maybe<int> some = Maybe.Some(42);
Maybe<int> some = Maybe<int>.Some(42);
```

`Maybe.Some(value)` is still useful when the compiler can't infer `T`
— for example inside a `var`-typed collection expression where every
element is `Maybe.None`.

### Equality and comparison work through implicits too

Every wrapper implements `IEquatable<TUnderlying>` and `IComparable<TUnderlying>`
on top of `IEquatable<Self>` / `IComparable<Self>`. You do not need to
unwrap before comparing:

```csharp
NonEmptyString.Create("alice") == "alice";   // true
2 == Positive<int>.Create(2);                // true
Positive<int>.Create(4) > 2;                 // true
Maybe.Some(3) == 3;                          // true
Maybe<int>.None < 0;                         // true (None sorts before any value)
```

### When to keep the wrapper visible

One common mistake is unwrapping too eagerly. Once you have a
`NonEmptyString`, pass it around as `NonEmptyString` — don't call `.Value`
just because a helper signature takes `string`. If the helper should enforce
non-empty input, change its signature. If it really takes any string, the
implicit conversion handles it at the call site.

```csharp
// Good — downstream stays typed.
public void Greet(NonEmptyString name) => Console.WriteLine($"hi, {name}");

// Wrong — unwraps for no reason.
public void Greet(NonEmptyString name) => Console.WriteLine($"hi, {name.Value}");
```

## `NonEmptyString`

Wraps a `string` that is non-null, non-empty, and not whitespace-only.
Construction is always via the factory pair — constructors are private.

### Factories

```csharp
NonEmptyString? name = NonEmptyString.TryCreate(input);   // null when null / empty / whitespace
NonEmptyString  name = NonEmptyString.Create(input);      // throws ArgumentException

// Extensions — identical semantics, nicer syntax at the call site.
NonEmptyString? name = input.AsNonEmpty();                // null on failure
NonEmptyString  name = input.ToNonEmpty();                // throws on failure
```

`AsNonEmpty()` is the one you want 90% of the time — pattern-match with
`is not { } name` and fall through to a 400 response / early return.

### String-like surface

`NonEmptyString` exposes enough of the `string` API that you rarely need
to unwrap it. Returned types keep the invariant where it still holds —
`ToUpper()` returns `NonEmptyString`, but `Substring(...)` returns a
plain `string` because a substring could be empty.

- `Value` — the underlying `string`.
- `Length` — `int`.
- Case conversions returning `NonEmptyString`: `ToLower()`, `ToLower(CultureInfo)`,
  `ToLowerInvariant()`, `ToUpper()`, `ToUpper(CultureInfo)`, `ToUpperInvariant()`.
- Containment: `Contains(string)`, `Contains(string, StringComparison)`,
  `Contains(char)`, `Contains(char, StringComparison)`.
- Replacement returning `string`: `Replace(char, char)`, `Replace(string, string)`,
  `Replace(string, string, StringComparison)`.
- `Trim()` returning `string` (because trimming can empty the string).
- Indexing: `IndexOf` / `LastIndexOf` overloads for `string` and `char`,
  with optional `startIndex` and `StringComparison`.
- `Substring(int)`, `Substring(int, int)` returning `string`.
- `StartsWith(...)`, `EndsWith(...)` overloads mirroring `string`.
- Implicit conversion to `string` — you can pass a `NonEmptyString` to any
  `string` parameter without `.Value`.
- Full equality / comparison operators against `NonEmptyString` *and*
  `string` — no `.Value` needed for `==`, `!=`, `<`, `<=`, `>`, `>=`.

### `Unwrap()` — the EF-Core marker

```csharp
string raw = nonEmpty.Unwrap();
```

In-memory `Unwrap()` just returns `Value`. The interesting use case is
inside an EF Core LINQ predicate, where the EfCore package rewrites
`property.Unwrap()` into a direct column reference so string operators
(`StartsWith`, `EF.Functions.Like`, `Collate`, …) translate server-side.
Outside EF Core predicates, `Unwrap()` and `Value` are interchangeable —
pick whichever reads better.

### Parse extensions

Both `string?` and `NonEmptyString` have a set of nullable-returning `As…`
and throwing `To…` conversions:

```csharp
int?       id  = header.AsInt();
int        id  = header.ToInt();
decimal?   amt = body.AsDecimal();
DateTime?  t   = header.AsDateTime();
TimeSpan?  ts  = header.AsTimeSpan();
Guid?      g   = header.AsGuid();
Guid?      g2  = header.AsGuidExact("D");     // strict format
bool?      b   = flag.AsBool();
Roles?     r   = header.AsEnum<Roles>();
byte?      bt  = header.AsByte();
short?     sh  = header.AsShort();
long?      lg  = header.AsLong();
float?     f   = header.AsFloat();
double?    d   = header.AsDouble();
```

Each `AsX` has a `ToX` counterpart that throws `FormatException` /
`OverflowException` on failure. `As…`/`To…` on `string?` accept null
input and return null from `As…` or throw from `To…`.

### Typical patterns

```csharp
// Controller validation — unwrap with is not { } v
[HttpPost]
public IActionResult Create(CreateRequest request)
{
    if (request.Name.AsNonEmpty() is not { } name)
        return BadRequest("name required");
    if (request.Age.AsPositive() is not { } age)
        return BadRequest("age must be positive");

    _service.Create(name, age);
    return NoContent();
}

// Interop with string APIs — implicit conversion handles it.
string json = JsonSerializer.Serialize(name);      // NonEmptyString → string implicit

// Keep invariants on a record.
public record User(NonEmptyString Name, NonEmptyString? Nickname);
```

### JSON

`[JsonConverter(typeof(NonEmptyStringJsonConverter))]` is attached on the
type. Serialises as a plain JSON string. Deserialising `""`, a
whitespace-only string, or `null` (into the non-nullable form) throws
`JsonException`. No `JsonSerializerOptions` registration required.

## Numeric wrappers — `Positive<T>`, `NonNegative<T>`, `Negative<T>`, `NonPositive<T>`

Four generic `readonly struct` wrappers that enforce a sign invariant on any
`INumber<T>`. `T` can be `int`, `long`, `short`, `byte`, `decimal`, `float`,
`double`, `BigInteger`, and any other `INumber<T>`.

| Type              | Invariant                         | `default(T)` |
| ----------------- | --------------------------------- | ------------ |
| `Positive<T>`     | `> 0`                             | `1`          |
| `NonNegative<T>`  | `>= 0`                            | `0`          |
| `Negative<T>`     | `< 0`                             | `-1`         |
| `NonPositive<T>`  | `<= 0`                            | `0`          |

`default(Positive<int>) == 1` and `default(Negative<int>) == -1` — the struct
layout makes the default value still satisfy the invariant, not a zero
that would break it.

### Factories

Same pattern as `NonEmptyString`:

```csharp
Positive<int>?       p   = Positive<int>.TryCreate(input);   // null if invalid
Positive<int>        p   = Positive<int>.Create(input);      // throws if invalid

// Extensions
Positive<int>?       p   = input.AsPositive();               // null on failure
Positive<int>        p   = input.ToPositive();               // throws on failure
NonNegative<decimal> nn  = price.ToNonNegative();
Negative<int>?       n   = drift.AsNegative();
NonPositive<decimal> np  = correction.ToNonPositive();
```

All four types have `AsX` / `ToX` extension methods available on any
`INumber<T>`.

### What you get for free on every wrapper

- `.Value` — the underlying `T`.
- `.Unwrap()` — synonym of `.Value`, but translated to a bare column
  reference by the EfCore package inside LINQ predicates.
- Implicit conversion `Positive<int> → int`, `NonNegative<decimal> → decimal`,
  etc. No `.Value` needed to interoperate with numeric APIs.
- Explicit conversion back (`(Positive<int>)x`) that throws on failure —
  prefer `AsPositive()` / `ToPositive()` extensions instead.
- `IEquatable<Self>`, `IEquatable<T>`, `IComparable<Self>`, `IComparable<T>`
  and matching `==`, `!=`, `<`, `<=`, `>`, `>=` operators on both sides —
  so `Positive<int>.Create(4) > 2` is just a comparison, no unwrap.
- `GetHashCode`, `Equals(object?)`, `ToString()` delegating to the value.
- `System.Text.Json` converter via `[JsonConverter]` — serialises as the
  underlying primitive.

### Arithmetic

The wrappers deliberately do **not** overload arithmetic operators — a
`Positive<int> + Positive<int>` is `Positive<int>` (ok), but
`Positive<int> - Positive<int>` is not; not worth the cliff. Unwrap via
the implicit conversion (or `.Value` / `.Unwrap()`) and re-wrap explicitly
when you need the invariant back:

```csharp
Positive<int> a = 3;
Positive<int> b = 5;

int sum = a + b;                           // implicit → int
Positive<int> wrapped = sum.ToPositive();  // re-wrap; throws if invariant broke
```

### Division helpers on `int` / `decimal`

`NumberExtensions` adds two small helpers for the common "divide by something
that might be zero" case:

```csharp
decimal? safe1  = 100.Divide(divisor);               // null when divisor == 0
decimal? safe2  = amount.Divide(divisor);
decimal fallback1 = 100.SafeDivide(divisor);           // returns 0 (default) on zero
decimal fallback2 = amount.SafeDivide(divisor, otherwise: -1m);
```

These sit on `int` and `decimal` — not on the wrappers — because the
division result can be negative or fractional regardless of the operands.

### JSON

Every numeric wrapper carries a JSON converter that (de)serialises as the
underlying primitive. `Positive<int>` on the wire is `42`, not
`{ "Value": 42 }`. Invalid values (`0` for `Positive<int>`, `-1` for
`NonNegative<int>`, …) fail with `JsonException` at deserialization.

### Modelling tips

```csharp
public record OrderLine(
    NonEmptyString    Sku,
    Positive<int>     Quantity,           // always at least 1
    NonNegative<decimal> UnitPrice        // 0 is a legit promo price
);

public record StockAdjustment(
    NonEmptyString  Sku,
    Negative<int>   Removed,              // must be a real decrement
    DateTime        At
);
```

Use the narrowest invariant that actually holds. If zero is a legitimate
price, `NonNegative<decimal>` is correct — don't reach for `Positive<decimal>`
because "prices are usually positive".

## `NonEmptyEnumerable<T>` and `INonEmptyEnumerable<T>`

`NonEmptyEnumerable<T>` is a sealed reference type that wraps a sequence
guaranteed to have at least one element. `INonEmptyEnumerable<out T>` is
a covariant interface for when you need to assign
`NonEmptyEnumerable<Dog>` into `INonEmptyEnumerable<Animal>`.

### Construction

```csharp
// Params / collection expressions
var list = NonEmptyEnumerable.Create(1, 2, 3);
NonEmptyEnumerable<int> list = [1, 2, 3];      // empty [] throws at construction

// Runtime sequences — use CreateRange (Create is taken by the collection-expression form)
NonEmptyEnumerable<int>  thrown    = NonEmptyEnumerable.CreateRange(source);
NonEmptyEnumerable<int>? nullable  = NonEmptyEnumerable.TryCreateRange(source);

// Extensions on IEnumerable<T>? — same semantics, nicer syntax
NonEmptyEnumerable<int>? maybe     = values.AsNonEmpty();   // null if empty/null
NonEmptyEnumerable<int>  required  = values.ToNonEmpty();   // throws if empty/null
```

Throws / returns null identically for null input and for empty input.

### Guaranteed shape

```csharp
T                head  = list.Head;    // always defined
IReadOnlyList<T> tail  = list.Tail;    // everything after Head
int              count = list.Count;   // always >= 1
```

`NonEmptyEnumerable<T>` implements `IReadOnlyList<T>` and has a struct
enumerator — no allocations on `foreach`.

### Invariant-preserving LINQ

These operations keep the non-empty guarantee and return
`NonEmptyEnumerable<TResult>`:

- `Select(...)`, including the indexed overload.
- `SelectMany(...)` *when the inner result is itself non-empty*.
- `Distinct()`, `Distinct(IEqualityComparer<T>)`.
- `Concat(params T[])`, `Concat(IEnumerable<T>)`, both on
  `NonEmptyEnumerable<T>` and `INonEmptyEnumerable<T>`.
- `Flatten()` on `INonEmptyEnumerable<INonEmptyEnumerable<T>>`.
- `T head.Concat(params IEnumerable<T>[] tails)` — builds a
  `NonEmptyEnumerable<T>` from a head + tails.
- `Prepend(T)`, `Append(T)`.
- `Reverse()`.
- `Take(Positive<int> count)` — the count wrapper enforces `>= 1` so
  the result cannot be empty. The `int` overload of `Take` is also
  available but prefer the `Positive<int>` one.

Operations that can produce an empty sequence (`Where`, `Skip`, `TakeWhile`,
`GroupBy`, …) fall through to standard LINQ and return `IEnumerable<T>`.
Re-wrap with `.AsNonEmpty()` / `.ToNonEmpty()` if you need the guarantee
back.

### Total aggregates

Because the sequence is known non-empty, aggregates that usually throw
`InvalidOperationException` on empty input are total here. For value
types, they return `T` directly, not `T?`:

```csharp
int max   = list.Max();
int min   = list.Min();
int last  = list.Last();
int sum   = list.Aggregate((a, b) => a + b);
int avg   = list.Average();             // where T : INumber<T>
T   maxBy = list.MaxBy(x => x.Key);
T   minBy = list.MinBy(x => x.Key);
```

All of these have overloads on both `NonEmptyEnumerable<T>` and
`INonEmptyEnumerable<T>`, so chains on either receiver type work.

### Covariance

```csharp
NonEmptyEnumerable<Dog>     dogs    = [new Dog()];
INonEmptyEnumerable<Animal> animals = dogs;   // ok — `out T` on the interface
```

Use `INonEmptyEnumerable<T>` in method parameters when you want callers
to be free to pass the more-derived form.

### JSON

Serialises as a JSON array. An empty JSON array (`[]`) is rejected with
`JsonException`. `NonEmptyEnumerable<T?>` accepts JSON `null` elements —
`[1, null, 3]` round-trips into `NonEmptyEnumerable<int?>` faithfully.

> ⚠ The usual C# caveat about `null` inside a reference-typed collection
> still applies: a JSON `[null]` will deserialize into
> `NonEmptyEnumerable<string>` even though `string` isn't annotated
> nullable. That's the same hole `List<string>` has — the library doesn't
> plug it.

### When to use it

- Method parameters where "must have at least one" is the real
  contract: `Notify(INonEmptyEnumerable<UserId> recipients)`, aggregate
  operations over a batch, etc. No defensive empty check inside the body.
- Return types from operations that always produce at least one element
  (a decomposition, a fan-out, a header + body shape).

Don't wrap lists where empty is a perfectly valid state — that just
pushes a `.ToNonEmpty()` or an awkward `if` to every caller.

## Parsing helpers

Three small surfaces: `Digit`, enum extension members, and string parsers.
All three follow the `As… returns T?` / `To… throws` pattern.

### `Digit`

A `readonly struct` wrapping a single `'0'`–`'9'` character.

```csharp
Digit? d = Digit.TryCreate('7');   // null if not '0'..'9'
Digit  d = Digit.Create('7');      // throws on invalid

Digit? d = '7'.AsDigit();          // extension on char
```

Conversions and invariants:

- `Value` — the underlying `byte` (0–9).
- Implicit `Digit → byte` and `Digit → int` — drop a `Digit` into any
  numeric slot without `.Value`.
- `IEquatable` / `IComparable` against `Digit`, `byte`, and `int`.
- `default(Digit)` is `0`.

Also a string helper for extracting digits:

```csharp
IEnumerable<Digit> digits = "a1b2c3".FilterDigits();   // Digit 1, 2, 3
```

Use `Digit` for parsing character streams (phone numbers, postal codes,
VINs) where each character must be a digit and the downstream code
benefits from that being typed.

### Enum extensions

`EnumExtensions` adds methods directly onto the enum type. You call
`Roles.Parse(...)`, not `EnumExtensions.Parse<Roles>(...)`.

```csharp
[Flags]
public enum Roles { None = 0, Reader = 1, Writer = 2, Admin = 4 }

// Factories — BCL-style naming
Roles  r1 = Roles.Parse("Reader");                    // throws on failure
Roles  r2 = Roles.Parse("reader", ignoreCase: true);
Roles? r3 = Roles.TryParse(userInput);                // null on failure
Roles? r4 = Roles.TryParse(userInput, ignoreCase: true);

// Same under StrongTypes' Create / TryCreate naming
Roles  r5 = Roles.Create("Reader");
Roles? r6 = Roles.TryCreate(userInput);

// All declared members (cached — safe in hot paths)
IReadOnlyList<Roles> all = Roles.AllValues;           // [None, Reader, Writer, Admin]
```

For `[Flags]` enums:

```csharp
IReadOnlyList<Roles> flags = Roles.AllFlagValues;     // [Reader, Writer, Admin] (excludes 0 and composites)
Roles                super = Roles.AllFlagsCombined;  // Reader | Writer | Admin

// Decompose a combined value back into declaration-order flags.
foreach (var flag in (Roles.Reader | Roles.Admin).GetFlags()) { ... }
```

`AllFlagValues`, `AllFlagsCombined`, and `GetFlags()` throw
`InvalidOperationException` if the enum is not `[Flags]` — so a typo at
declaration fails on first use, not silently.

There's also a typed converter pair for generic code:

```csharp
long raw   = EnumExtensions<Roles>.ToLong(value);
Roles back = EnumExtensions<Roles>.FromLong(raw);
```

### String parsers

The open-generic enum variant (useful when you only know the enum type
via a generic parameter, where `Roles.TryParse(...)` isn't reachable):

```csharp
TEnum? e1 = input.AsEnum<TEnum>();                // null on failure
TEnum  e2 = input.ToEnum<TEnum>();                // throws on failure
TEnum? e3 = input.AsEnum<TEnum>(ignoreCase: true);
```

Full set on `string?` (all return `T?` / throw):

| `As…` (nullable)            | `To…` (throwing)             |
| --------------------------- | ---------------------------- |
| `AsNonEmpty()`              | `ToNonEmpty()`               |
| `AsByte()`                  | `ToByte()`                   |
| `AsShort()`                 | `ToShort()`                  |
| `AsInt()`                   | `ToInt()`                    |
| `AsLong()`                  | `ToLong()`                   |
| `AsFloat()`                 | `ToFloat()`                  |
| `AsDouble()`                | `ToDouble()`                 |
| `AsDecimal()`               | `ToDecimal()`                |
| `AsBool()`                  | `ToBool()`                   |
| `AsDateTime()`              | `ToDateTime()`               |
| `AsTimeSpan()`              | `ToTimeSpan()`               |
| `AsGuid()`                  | `ToGuid()`                   |
| `AsGuidExact(format)`       | `ToGuidExact(format)`        |
| `AsEnum<TEnum>()`           | `ToEnum<TEnum>()`            |

Numeric parsers accept optional `IFormatProvider` and `NumberStyles`.
Date/time parsers accept `IFormatProvider` and `DateTimeStyles`. The same
extensions are available on `NonEmptyString` (calling them with `.Value`
is unnecessary).

### Common pattern

```csharp
public IActionResult Get([FromQuery] string? id, [FromQuery] string? sort)
{
    if (id.AsGuid() is not { } guid)             return BadRequest("id must be a GUID");
    if (sort.AsEnum<SortOrder>() is not { } so)  return BadRequest("invalid sort");

    return Ok(_service.List(guid, so));
}
```

## `T?.Map` and `bool.MapTrue` / `MapFalse`

Two tiny extensions that close the last ergonomics gap in nullable /
boolean composition. They mean you rarely need `Maybe<T>` or `Result<T>`
just to chain an optional computation.

### `T?.Map(...)`

C# already lets you *read* through a null (`user?.Name?.Trim()`), but
*passing* a nullable into a non-null-accepting function used to require
a ternary:

```csharp
// Before
MailAddress? email = text is null ? null : new MailAddress(text);

// After
MailAddress? email = text.Map(t => new MailAddress(t));
```

The mapper only runs when the input is non-null; otherwise the whole
expression is `null`. Works for both reference and value types, with sync
and async variants:

```csharp
int?    doubled = maybeInt.Map(x => x * 2);
string? upper   = maybeText.Map(s => s.ToUpperInvariant());

// Mapper may itself return a nullable — same outer nullability is preserved.
int?    parsed  = input.Map(s => int.TryParse(s, out var n) ? n : (int?)null);

// Async
int?    await   = await maybeId.MapAsync(id => _store.CountAsync(id));
```

Overloads cover the four combinations of value-type / reference-type input
and value-type / reference-type result — picking the right one is the
compiler's problem, not yours.

### `bool.MapTrue` / `bool.MapFalse`

Same idea for bools. "Compute this only when the flag is true (or false)":

```csharp
MyResult? result = featureFlag.MapTrue(CallSomeService);
MyResult? fallback = featureFlag.MapFalse(UseOlderPath);

// Async variants
MyResult? r2 = await featureFlag.MapTrueAsync(() => _svc.CallAsync());
```

- `MapTrue(Func<T>)`, `MapTrue(Func<T?>)` — value or reference `T`.
- `MapFalse(...)` — same overload set.
- `MapTrueAsync(...)`, `MapFalseAsync(...)` — `Task<T>` and `Task<T?>` variants.

Returns `T?` for reference types and `T?` (`Nullable<T>`) for value types.
A `false` flag (for `MapTrue`) or a `true` flag (for `MapFalse`) returns
`null` without invoking the callback.

### Performance note

`Map` / `MapTrue` / `MapFalse` are slower than the equivalent ternary —
they go through a delegate invocation. On hot paths (tight loops,
per-element allocations), prefer the ternary. Everywhere else, prefer
these for readability.

### Decision guide

- You want "run this only if non-null / true / false, and produce another
  nullable" → `Map`, `MapTrue`, `MapFalse`.
- You want "run this unconditionally and handle null at call site" →
  plain method with `T?` parameter and `is not { } v` pattern.
- You want "two-state vs three-state tracking for an update-style field"
  (skip / clear / set — HTTP PATCH or any plain update DTO / service
  parameter) → `Maybe<T>?`, not `Map`.

## `Maybe<T>`

A `readonly struct` that is either `Some(T)` or `None`. Constrained to
`T : notnull` — `Maybe<int?>` and `Maybe<string?>` are disallowed because
they would collapse the `None` and `Some(null)` cases.

> **Before reaching for `Maybe<T>`, re-read the design-philosophy section.**
> Most optional fields should be plain `T?`. `Maybe<T>` exists for the
> three-state case — wherever a field or parameter needs to mean "skip",
> "clear", *and* "set" at the same time. HTTP `PATCH` DTOs are the most
> visible example, but the same pattern applies to plain in-process
> update methods (service calls, builders, message payloads) with no
> HTTP involved. It is also a total replacement for the throwing
> `First` / `Single` / `Max` LINQ calls.

### Construction — prefer implicit operators

```csharp
Maybe<int>    some = 42;              // implicit operator T → Maybe<T>
Maybe<int>    none = Maybe.None;      // untyped None, binds to the slot type

// Explicit only when inference can't help.
Maybe<int>    some = Maybe.Some(42);          // T inferred
Maybe<int>    some = Maybe<int>.Some(42);     // fully explicit

// From a nullable
Maybe<int>    m1 = nullableInt.ToMaybe();     // Some when HasValue, None otherwise
Maybe<string> m2 = nullableString.ToMaybe();  // Some when non-null, None otherwise
```

`Maybe.None` is a separate `MaybeNone` value with an implicit conversion
to any closed `Maybe<T>`, so collection expressions can mix literals and
None markers cleanly:

```csharp
Maybe<int>[] xs = [1, 2, Maybe.None, 4];
IEnumerable<int> values = xs.Values();   // [1, 2, 4]
```

### Inspection — `is { } v` on `.Value`

The idiomatic unwrap uses the `Value` extension property. For value types
it returns `Nullable<T>`; for reference types it returns `T?`. Either way
the `is { } v` pattern unwraps to the underlying `T`:

```csharp
if (maybe.Value is { } v)
{
    // v is int (not int?) / string (not string?)
}
```

Other inspection surface:

- `IsSome`, `IsNone`, `HasValue` — all `bool`.
- `Match(Func<T, R> ifSome, Func<R> ifNone)` — fold to a value.
- `Match(Action<T>? ifSome = null, Action? ifNone = null)` — side-effect form.

### Composition — `Map`, `FlatMap`, `Where`

```csharp
Maybe<int>     doubled = Maybe.Some(3).Map(x => x * 2);          // Some(6)
Maybe<int>     gone    = Maybe<int>.None.Map(x => x * 2);        // None

Maybe<int>     parsed  = Maybe.Some("42").FlatMap(Parse);        // Some(42)
Maybe<int>     filtered = Maybe.Some(4).Where(x => x % 2 == 0);  // Some(4)

// LINQ query syntax works via Select / SelectMany.
var sum = from a in Maybe<int>.Some(2)
          from b in Maybe<int>.Some(3)
          select a + b;                                          // Some(5)
```

Async counterparts: `MapAsync`, `FlatMapAsync`, `MatchAsync`.

### Collection helpers — `SafeX` and `Values`

These are drop-in replacements for throwing LINQ operators. Use them when
an empty sequence (or no match) is a legitimate result, not an error:

```csharp
Maybe<User> m = users.SafeFirst(u => u.Age > 18);
Maybe<User> m = users.SafeSingle(u => u.Id == target);
Maybe<User> m = users.SafeLast();
Maybe<int>  hi = scores.SafeMax();
Maybe<int>  lo = scores.SafeMin();
Maybe<Score> worst = results.SafeMin(r => r.Value);

// Drop Nones in one shot.
IEnumerable<int> values = maybes.Values();
```

(When the source sequence is already non-empty — i.e. a
`NonEmptyEnumerable<T>` — use the total `Max`, `Min`, `Last` that return
`T` directly. `SafeX` is for the general `IEnumerable<T>` case.)

### JSON

`Maybe<T>` serialises as `{ "Value": x }` for `Some` and either
`{ "Value": null }` or `{}` for `None`. The converter is on the type, so
no registration is needed. The same `[JsonConverter]` attribute is also
what makes the `Maybe<T>?` update pattern work over the wire — a missing
property deserializes as `null` on the outer nullable; a `{}` or
`{ "Value": null }` deserializes as `Maybe<T>.None`; anything else
becomes `Maybe.Some(value)`.

### Three-state updates — the canonical case

The pattern applies wherever an update needs to distinguish "skip",
"clear", and "set" — HTTP `PATCH` or pure in-process code alike.

**In-process service call — no HTTP involved.** An `ApplyChanges` method
whose caller can leave a field alone, clear it, or set a new value:

```csharp
public record ProfileChanges(
    Maybe<string>? Nickname,        // null = skip; None = clear; Some(x) = set
    Maybe<DateOnly>? Birthday       // same three states
);

public sealed class ProfileService(IProfileRepository repo)
{
    public async Task ApplyChanges(Guid userId, ProfileChanges changes)
    {
        var profile = await repo.LoadAsync(userId);

        if (changes.Nickname is { } nick)
            profile.Nickname = nick.Value;       // nick.Value: string?
        if (changes.Birthday is { } bday)
            profile.Birthday = bday.Value;

        await repo.SaveAsync(profile);
    }
}
```

The caller of `ApplyChanges` may be a controller, a background job, a
message consumer, another service — none of that matters. The three
states exist purely in the domain.

**HTTP PATCH — the same pattern once more, across the wire.** The JSON
converter takes care of the mapping; the handler body is identical in
shape:

```csharp
public record PatchRequest(
    Maybe<string>? Nickname   // null = skip; None = clear; Some(x) = set
);

[HttpPatch("{id:guid}")]
public async Task<IActionResult> Patch(Guid id, PatchRequest request)
{
    var entity = await _db.FindAsync<Entity>(id);
    if (entity is null) return NotFound();

    if (request.Nickname is { } nick)
        entity.Nickname = nick.Value;   // Value: string? — null when None, value when Some

    await _db.SaveChangesAsync();
    return Ok();
}
```

Note the double unwrap in both examples: `request.Nickname is { } nick`
tells you the caller sent *something*; `nick.Value` is then the intended
new value (possibly null for the "clear" intent).

## `Result<T, TError>`

Either a success carrying a `T` or an error carrying a `TError`.
`Result<T>` is the alias for `Result<T, Exception>` — use it when you just
want to represent "might throw" without picking a specific exception type.

Use `Result` when the caller needs to distinguish *why* something failed,
aggregate multiple failures together, or translate a domain failure into a
user-facing error. Otherwise use `T?` (see design-philosophy section).

### Construction — `return value;`

Implicit operators convert both a `T` and a `TError` into the appropriate
`Result<T, TError>` branch. Once the method's return type is declared,
`return someT;` or `return someError;` is enough — **do not** write
`Result<T, TError>.Success(x)` or `Result.Error<T, TError>(e)`. Inference
handles it.

```csharp
public Result<int, string> Parse(string s)
    => int.TryParse(s, out var n) ? n : "not a number";

public Result<Order, OrderError> Place(OrderData data)
{
    if (data.Items.Count == 0)
        return OrderError.EmptyCart;       // implicit → error branch

    return new Order(data);                // implicit → success branch
}
```

Explicit factories exist for the rare case where inference can't pick a
branch (usually when `T` and `TError` are the same type):

```csharp
var ok  = Result.Success<int, string>(42);
var err = Result.Error<int, string>("bad");
```

For `Result<T>` (the `Exception`-flavoured alias), the implicit operators
convert from `T` and from any `Exception`:

```csharp
public Result<string> Read(string path) => File.ReadAllText(path);   // success

public Result<string> ReadOrFail(string path)
    => !File.Exists(path)
        ? new FileNotFoundException(path)   // implicit → error branch
        : File.ReadAllText(path);
```

### Access

```csharp
Result<int, string> r = Parse(input);

if (r.Success is { } value) { /* value is int */ }
if (r.Error   is { } msg)   { /* msg is string */ }

bool ok = r.IsSuccess;
bool no = r.IsError;
```

`Success` / `Error` are extension properties. They return
`Nullable<T>` or `T?` so `is { } v` unwraps to the bare type.

### Fold — `Match`

```csharp
string message = r.Match(
    success: x => $"got {x}",
    error:   e => $"oops: {e}");

await r.MatchAsync(
    success: async x => await logger.LogAsync("ok", x),
    error:   async e => await logger.LogAsync("bad", e));
```

`Match` exists because C# does not yet let you switch on `Result<T, TError>`
by branch.

### Transform — `Map`, `MapError`, `FlatMap`

```csharp
// Success side — Map
Result<int, string> doubled = r.Map(x => x * 2);

// Error side — MapError
Result<int, ApiError> translated = r.MapError(msg => ApiError.Parse(msg));

// Both sides in one pass
Result<int, ApiError> both = r.Map(x => x * 2, msg => ApiError.Parse(msg));

// FlatMap — chain an operation that itself returns a Result.
Result<int, string> positive = r.FlatMap<int>(x => x > 0 ? x : "must be positive");
```

All of the above have `MapAsync`, `MapErrorAsync`, `FlatMapAsync`
counterparts.

### From nullable

When a nullable validation is the *source* of a `Result`:

```csharp
// Result<T, TError> variants (two shapes each: value and factory)
Result<NonEmptyString, string> a = name.AsNonEmpty().ToResult("name required");
Result<NonEmptyString, string> b = name.AsNonEmpty().ToResult(() => BuildMessage());

// Result<T> (Exception-flavoured)
Result<NonEmptyString> c = name.AsNonEmpty().ToResult();                        // throws default Exception on null
Result<NonEmptyString> d = name.AsNonEmpty().ToResult(new ArgumentException()); // custom exception
Result<NonEmptyString> e = name.AsNonEmpty().ToResult(() => new Ex("..."));     // factory
```

Both the value-type and reference-type overloads are present, so you can
call `ToResult` on any `T?` or `Nullable<T>`.

### Aggregate multiple validations

`Result.Aggregate` collects *all* errors, not just the first. Tuple-style
overloads up to 8 inputs, plus an `IEnumerable` overload for dynamic lists:

```csharp
record User(NonEmptyString Name, Positive<int> Age);

Result<User, string> ParseUser(string? nameInput, int ageInput)
{
    Result<NonEmptyString, string> name = nameInput.AsNonEmpty().ToResult("name must not be empty");
    Result<Positive<int>, string>  age  = ageInput.AsPositive().ToResult("age must be positive");

    return Result.Aggregate(name, age,
        (n, a) => new User(n, a),
        errors => string.Join("; ", errors));
}

// Pass the raw error list through if you don't want to merge
Result<User, string[]> u = Result.Aggregate(name, age, (n, a) => new User(n, a));

// Dynamic count
Result<Positive<int>[], string> parsed = Result.Aggregate(
    inputs.Select(i => i.AsPositive().ToResult(i)),
    invalid => $"not positive: [{string.Join(", ", invalid)}]");
```

### Catch exceptions into a Result

```csharp
Result<string>                  r = Result.Catch(() => File.ReadAllText(path));
Result<int, FormatException>    r = Result.Catch<int, FormatException>(() => int.Parse(input));

// Async
Result<string> r = await Result.CatchAsync(() => File.ReadAllTextAsync(path));
```

`OperationCanceledException` (and `TaskCanceledException`) are **not**
captured by default — cancellation unwinds normally. Opt in with
`propagateCancellation: false` if you want cancellation to surface as a
`Result` error.

### Escape hatches

```csharp
T value1 = r.ThrowIfError();                            // throws the TError directly (if it's an Exception)
T value2 = r.ThrowIfError(e => new DomainException(e)); // wrap into an exception of your choice
T value3 = aggregated.ThrowIfError();                   // for Result<T, IReadOnlyList<Exception>>
Result<T, TError> flat = nested.Flatten();              // Result<Result<T, E>, E> → Result<T, E>
```

### Flow-of-control

- Controllers **consume** `Result`s from services and convert them into
  HTTP responses (`BadRequest`, `Problem`, `Ok`, …) — they rarely
  construct a `Result` themselves.
- Services **return** `Result<T, TError>` where `TError` is a domain
  enum. The calling controller maps the enum to a user-facing code.
- Pure-C# validation in a controller (e.g. `input.AsNonEmpty() is not { }
  name`) does **not** need a `Result`.

### JSON

`Result<T, TError>` has **no** JSON converter — deliberately. Don't
serialise it. If you need to ship a result over the wire, translate it
into a dedicated response DTO first.

## Collection and exception helpers

Utility extensions that don't fit a "strong type" bucket but round out the
library.

### `IEnumerable<T>` extensions

- `ExceptNulls()` — filter out nulls in one step:
  ```csharp
  IEnumerable<string> names = source.ExceptNulls();   // source : IEnumerable<string?>
  ```
  Works for both reference-nullable and `Nullable<T>` sources.

- `Except(params T[] items)` — exclude a known handful of elements by
  value, without building a set yourself.

- `Concat(params T[] items)` and `Concat(params IEnumerable<T>[] others)`
  — flatten a few extras into a sequence without `.Concat(new[] { ... })`:
  ```csharp
  var all = existing.Concat(1, 2, 3);
  var all = existing.Concat(list1, list2, list3);
  ```

- `Flatten()` on `IEnumerable<IEnumerable<T>>` — an alias for
  `SelectMany(x => x)` that reads better at a call site.

- `OrEmptyIfNull()` — coalesce a null collection reference to an empty
  one of the same interface (`IEnumerable<T>`, `List<T>`,
  `IReadOnlyList<T>`, `ICollection<T>`). No allocation when the input
  is non-null.

- `Partition(Func<T, bool> predicate)` — split in a single pass into two
  `IReadOnlyList<T>`:
  ```csharp
  var (passing, violating) = users.Partition(u => u.IsActive);
  ```

- `ToReadOnlyList()` / `AsReadOnlyList()` / `AsList()` — cheap view
  conversions. `AsReadOnlyList` and `AsList` are zero-alloc if the source
  already implements the target interface.

### `ReadOnlyList`

```csharp
IReadOnlyList<int> list = ReadOnlyList.Create(1, 2, 3);
IReadOnlyList<int> flat = ReadOnlyList.CreateFlat(a, b, c);   // IEnumerable<T>[]
```

Lightweight factories when you want an `IReadOnlyList<T>` and don't care
about the concrete implementation.

### `Result` partition helpers

For a collection of `Result<T, TError>`:

```csharp
var (successes, errors) = results.Partition();   // IReadOnlyList<T>, IReadOnlyList<TError>

// Side-effect fold
results.PartitionMatch(
    successes: ok   => Save(ok),
    errors:    bad  => Log(bad));

// Projection fold — returns R[]
R[] merged = results.PartitionMatch(
    successes: ok   => Summarise(ok),
    errors:    bad  => Describe(bad));
```

### `Exception` aggregate

Turn a collection of exceptions into a single `AggregateException`,
returning null when the source is empty (or the single exception when
there's only one):

```csharp
Exception? agg = exceptions.Aggregate();                 // null on empty
Exception? agg = list.Aggregate();                       // IReadOnlyList<Exception>
Exception  agg = nonEmptyList.Aggregate();               // INonEmptyEnumerable<Exception>
```

This is what `Result.Aggregate(...)` uses under the hood when `TError`
is `Exception`.

### Boolean helper

```csharp
bool ok = condition.Implies(consequence);           // !condition || consequence
bool ok = condition.Implies(() => ExpensiveCheck()); // short-circuiting
```

Reads cleanly in guard clauses (`Debug.Assert(isLoaded.Implies(size > 0))`).

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

## Anti-patterns — common misuses to avoid

A cheat sheet of mistakes that show up when people adopt the library
before they've internalised the design philosophy.

### 1. Using `Maybe<T>` for a plain optional field

```csharp
// Wrong — no three-state intent; the field just might not exist.
public record Profile(Maybe<string> Bio);

// Right — T? captures "might be absent" already.
public record Profile(string? Bio);
```

`Maybe<T>` is for the **three-state** case (skip / clear / set). HTTP
`PATCH` is the visible example, but the rule applies to any update —
service method, builder, message payload, internal DTO. If there's no
"clear" intent, `T?` is enough.

### 2. Using `Maybe<T>` for update DTOs that can't remove the value

```csharp
// Wrong — Price cannot be "removed". Only "skipped" or "set".
public record OrderUpdate(Maybe<decimal> Price);

// Right — null means "don't update", decimal value means "set".
public record OrderUpdate(decimal? Price);
```

The rule: reach for `Maybe<T>?` only when the field supports all three of
"don't touch", "clear to null", and "set to value". If "clear to null"
is meaningless, plain `T?` is correct.

### 3. Using `Result<T, E>` for single-reason validations

```csharp
// Wrong — the caller doesn't need the reason; "name must not be empty"
// is already implied by the NonEmptyString name.
public Result<NonEmptyString, string> ParseName(string? input) =>
    input.AsNonEmpty().ToResult("name required");

// Right — let the caller decide how to react to null.
public NonEmptyString? ParseName(string? input) => input.AsNonEmpty();

// Call site
if (ParseName(request.Name) is not { } name)
    return BadRequest("name required");
```

`Result<T, TError>` earns its keep when the caller needs to distinguish
*which* failure occurred — typically an enum with multiple cases, often
aggregated across several inputs. A single-reason parse is cleaner as
`T?` + pattern matching.

### 4. Spelling out explicit factories when implicit operators suffice

```csharp
// Wrong — unnecessary ceremony.
return Result<Order, OrderError>.Success(order);
return Result.Error<Order, OrderError>(OrderError.OutOfStock);

// Right — return-type inference picks the branch.
return order;
return OrderError.OutOfStock;

// Same for Maybe<T>
Maybe<int> x = Maybe<int>.Some(42);   // unnecessary
Maybe<int> x = 42;                     // idiomatic
```

Explicit factories (`Result.Success<T, TError>`, `Maybe<T>.Some`) are for
the rare inference-collision case (e.g. `T == TError`). Default is `return
value;`.

### 5. Unwrapping a wrapper the moment you can

```csharp
// Wrong — loses the invariant on the very next line.
public void Greet(NonEmptyString name)
    => _downstream.Greet(name.Value);

// Right — implicit conversion already exists. Keep NonEmptyString flowing.
public void Greet(NonEmptyString name)
    => _downstream.Greet(name);
```

If the downstream signature is `string`, the implicit conversion handles
the interop. If the downstream should enforce non-empty, change its
signature. Reaching for `.Value` should be the exception, not the norm.

### 6. Constructing wrappers through the throwing factory in a controller

```csharp
// Wrong — throws into ASP.NET's exception pipeline for user input.
var name = NonEmptyString.Create(request.Name);

// Right — treat user input with the nullable-returning factory.
if (request.Name.AsNonEmpty() is not { } name)
    return BadRequest("name required");
```

`Create` / `ToX` is for *internal* code where invalid input is a bug and
throwing is the correct response. `TryCreate` / `AsX` is for *external*
input where invalid means "reply with a 400".

### 7. Writing your own JSON converter for a wrapper

Don't. Every wrapper in the library already ships a converter. If you
write a custom one, you likely have a bug (e.g. your converter doesn't
validate) or you're fighting a configuration issue elsewhere.

### 8. Using `NonEmptyEnumerable<T>` for "probably not empty, usually"

```csharp
// Wrong — tags is naturally allowed to be empty.
public record Article(string Title, NonEmptyEnumerable<string> Tags);

// Right — an empty tag list is valid.
public record Article(string Title, IReadOnlyList<string> Tags);
```

Use `NonEmptyEnumerable<T>` only where "zero elements" really is an
error — batch recipients, decomposed paths, aggregate inputs. Otherwise
every caller pays a `.ToNonEmpty()` tax.

### 9. Forgetting to call `.Unwrap()` in EF LINQ

```csharp
// Doesn't translate — EF can't call string.StartsWith on a NonEmptyString in SQL.
db.Users.Where(u => u.Name.StartsWith("ali"))

// Right — .Unwrap() rewrites to a bare column reference for SQL.
db.Users.Where(u => u.Name.Unwrap().StartsWith("ali"))
```

Equality / ordering / null-checks on the wrapper work directly. Anything
using the underlying type's operators (`Contains`, arithmetic,
`EF.Functions.*`) needs `.Unwrap()`.

