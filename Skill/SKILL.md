---
name: strongtypes
description: Write idiomatic Kalicz.StrongTypes code (NonEmptyString, numeric wrappers, NonEmptyEnumerable, Maybe<T>, Result<T, TError>, EF Core and FsCheck integrations). Invoke when editing C# files that import the StrongTypes namespace, when designing DTOs / service signatures in projects that use the Kalicz.StrongTypes NuGet package, or when deciding between plain T?, Maybe<T>, and Result<T, TError>.
---

# StrongTypes — skill

This skill teaches an AI agent to use the `Kalicz.StrongTypes` C# library
idiomatically. Use it in any C# project that depends on `Kalicz.StrongTypes`
(and optionally `Kalicz.StrongTypes.EfCore` / `Kalicz.StrongTypes.FsCheck`).

This file holds the cross-cutting guidance — what the library is for, how to
pick between `T?` / `Maybe<T>` / `Result<T, TError>`, the implicit-operator
rule, JSON behaviour, and the anti-pattern cheat sheet. Per-type reference
material lives in `references/*.md`; load the relevant file on demand when
you're about to write code against that specific surface.

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

## JSON — zero setup

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

## Per-type references — load on demand

The full surface of each type lives in a dedicated file. When you're about
to write or change code that touches a specific type, read the matching
reference first.

| Type / topic                                          | File                                         |
| ----------------------------------------------------- | -------------------------------------------- |
| `NonEmptyString` (factories, string surface, parsers) | `references/nonemptystring.md`               |
| `Positive<T>` / `NonNegative<T>` / `Negative<T>` / `NonPositive<T>` | `references/numeric.md`         |
| `NonEmptyEnumerable<T>` / `INonEmptyEnumerable<T>`    | `references/nonemptyenumerable.md`           |
| `Digit`, enum extensions, `string?` parsers           | `references/parsing.md`                      |
| `T?.Map`, `bool.MapTrue` / `MapFalse`                 | `references/map.md`                          |
| `Maybe<T>` (incl. three-state `Maybe<T>?` updates)    | `references/maybe.md`                        |
| `Result<T, TError>` and `Result<T>`                   | `references/result.md`                       |
| Collection / exception / boolean helpers              | `references/collections.md`                  |
| EF Core integration (`UseStrongTypes`, `Unwrap()`)    | `references/efcore.md`                       |
| FsCheck integration (`Generators`, arbitraries)       | `references/fscheck.md`                      |

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
