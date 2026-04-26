---
name: strongtypes
description: Write idiomatic Kalicz.StrongTypes code (NonEmptyString, numeric wrappers, NonEmptyEnumerable, Maybe<T>, Result<T, TError>, EF Core and FsCheck integrations). Invoke when editing C# files that import the StrongTypes namespace, when designing DTOs / service signatures in projects that use the Kalicz.StrongTypes NuGet package, or when deciding between plain T?, Maybe<T>, and Result<T, TError>.
---

# StrongTypes — skill

Library of focused C# value wrappers (`NonEmptyString`, `Positive<T>`,
`NonEmptyEnumerable<T>`, …) and algebraic types (`Maybe<T>`,
`Result<T, TError>`) that push invariants into the type system. Every
wrapper ships a `System.Text.Json` converter, so invalid input fails at
deserialization before any endpoint code runs.

Per-type detail lives in `references/*.md` — load the relevant file on
demand when about to write code against that surface.

## Packages

| Package                       | What it gives you                                                                                                  |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `Kalicz.StrongTypes`          | The core types (`NonEmptyString`, numeric wrappers, `NonEmptyEnumerable<T>`, `Maybe<T>`, `Result<T, TError>`, …).  |
| `Kalicz.StrongTypes.EfCore`   | EF Core value converters + a `.Unwrap()` LINQ translator so strong types sit directly on entity properties.        |
| `Kalicz.StrongTypes.FsCheck`  | FsCheck `Arbitrary<T>` generators registered via `[Properties(Arbitrary = new[] { typeof(Generators) })]`.         |

Add EfCore / FsCheck only when you hit those stacks.

## Design philosophy — picking the right wrapper

Most misuses of StrongTypes come from reaching for `Maybe<T>` or
`Result<T, TError>` when a plain `T?` would do. Read the decision trees
before writing a new DTO field or service signature.

### Decision tree for "optional / nullable" fields

1. **Can the field be absent, but never be explicitly cleared?** → use `T?`.
   Example: an `OrderUpdate` DTO's `Price` property. Null means "don't touch
   the price". You can't "remove" a price, so there are only two states,
   and `decimal?` captures them both.

2. **Can the field be absent *and* be explicitly cleared to null?** → use
   `Maybe<T>?`. The pattern applies to *any* update operation that needs
   those three states — DTO, service signature, message payload, builder
   parameter — wherever "don't touch", "clear", and "set" all need to
   coexist. `null` skips, `Maybe<T>.None` clears, `Maybe.Some(x)` sets.
   HTTP `PATCH` is the most visible case (JSON distinguishes "property
   omitted" from "property sent as null"); the same shape applies to
   any in-process update method.

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

   No `Result` is needed because the caller already knows *why* the
   parse failed — the rule is encoded in the wrapper's name.

2. **Does the caller need to distinguish between multiple failure
   reasons, translate them to user-facing codes, or aggregate them with
   other failures?** → return `Result<T, TError>`. `TError` is normally an
   enum, occasionally a string if you don't need localisation.

   ```csharp
   public enum OrderError { PaymentFailed, OutOfStock, InvalidAddress }

   public Result<Order, OrderError> CreateOrder(OrderData data) { ... }
   ```

3. **Does an exception fit the failure better?** → use the throwing
   `ToX` extensions (`ToNonEmpty`, `ToPositive`, …), which throw
   `ArgumentException`. Don't invent a `Result<Order, Exception>` to
   smuggle an exception through.

### Summary rule

> **`T?` is the default.** Reach for `Maybe<T>?` only when the field
> genuinely has three states (skip / clear / set). Reach for
> `Result<T, TError>` only when the caller needs the error *reason*, not
> just the fact of failure — typically because there are multiple reasons
> to distinguish or aggregate. Everything else is a primitive or a
> strong wrapper.

### Result flow in practice

Services return `Result<T, TError>`. Controllers *consume* those results
and turn them into HTTP responses, but rarely *construct* a `Result` —
controller-level validation just returns `BadRequest(...)` directly.

```csharp
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

## Implicit operators — Maybe and Result

`Maybe<T>` and `Result<T, TError>` accept a plain value (or error)
through implicit operators. **Prefer `return value;` over the explicit
factories** — `Maybe<int> m = 42;` instead of `Maybe<int>.Some(42)`,
`return value;` / `return error;` instead of `Result.Success<T, TError>(value)`
/ `Result.Error<T, TError>(error)`. Detail and edge cases in
`references/maybe.md` and `references/result.md`. Reach for the explicit
factories only when type inference can't pick a branch (typically when
`T == TError`).

(`NonEmptyString` and the numeric wrappers expose only a *wrapper →
underlying* implicit conversion — the reverse is `explicit` because not
every string/int passes the invariant. Construct them through the
`AsX` / `ToX` extensions (`input.AsNonEmpty()`, `value.ToPositive()`) —
prefer those over the static `Create` / `TryCreate` factories. See
anti-pattern #5 for keeping the wrapped type flowing through your code
instead of unwrapping eagerly.)

## JSON — zero setup

Every wrapper except `Result<T, TError>` carries `[JsonConverter(...)]`.
Consequences:

- No `JsonSerializerOptions.Converters.Add(...)` calls. It just works.
- On-the-wire format matches the underlying primitive: `"hello"`,
  `42`, `[1, 2, 3]`. The exception is `Maybe<T>`, which serialises as
  `{ "Value": x }` / `{ "Value": null }` (or accepts `{}` for `None`).
- Invalid payloads throw `JsonException` at deserialization — in
  ASP.NET Core that's *before* your endpoint runs.
- `Result<T, TError>` has **no** converter by design. Translate to a
  response DTO before serialising.

## Per-type references — load on demand

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

The first four restate the philosophy above; the remaining five flag
mistakes you wouldn't catch from the decision trees alone.

1. **`Maybe<T>` for a plain optional field** — `T?` already captures
   "might be absent". Reserve `Maybe<T>?` for the three-state case.
2. **`Maybe<T>?` for an update field that can't be cleared** — `decimal?
   Price` is correct; `Maybe<decimal>? Price` invents a meaningless
   "clear" state.
3. **`Result<T, E>` for single-reason validations** — return `T?` from
   the parser, let the caller turn null into a 400.
4. **Spelling out explicit factories** — `return value;` over
   `Result<T, TError>.Success(value)`; `Maybe<int> x = 42;` over
   `Maybe<int>.Some(42)`. Use the explicit form only when inference
   collides.
5. **Downstream signatures that take the underlying type.** Once a
   value has been validated into a `NonEmptyString` / `Positive<int>`,
   keep that type flowing through the code you own — service methods,
   DB entity properties, message payloads. A downstream `string` /
   `int` parameter throws away the invariant and forces every caller
   to re-validate (or skip validation and hope).

   ```csharp
   // Wrong — caller has a NonEmptyString but the signature accepts
   // any string, so the invariant is gone at the next layer.
   public void Greet(string name) { ... }

   // Right — the signature documents and enforces the invariant.
   public void Greet(NonEmptyString name) { ... }
   ```

   `.Value` and the implicit conversion are interchangeable — neither
   is "wrong". The fix is to widen the wrapper's reach in your own
   types, not to police how you cross into BCL / third-party APIs that
   take primitives.

6. **Constructing wrappers through the throwing factory in a controller.**
   `ToX` is for *internal* code where invalid input is a bug. `AsX` is
   for *external* input where invalid means "reply with a 400". And
   prefer the extensions (`input.ToNonEmpty()`, `input.AsNonEmpty()`)
   over the static `NonEmptyString.Create` / `TryCreate` — the
   extensions read better at the call site and chain naturally.

   ```csharp
   // Wrong — throws into ASP.NET's exception pipeline for user input.
   var name = request.Name.ToNonEmpty();

   // Wrong — verbose static factory for what `input.ToNonEmpty()` does.
   var name = NonEmptyString.Create(request.Name);

   // Right — extension + nullable form for user input.
   if (request.Name.AsNonEmpty() is not { } name)
       return BadRequest("name required");
   ```

7. **Writing your own JSON converter for a wrapper.** Don't. Every
   wrapper already ships one. A custom converter is either a bug (no
   validation) or a config issue elsewhere.

8. **`NonEmptyEnumerable<T>` for "probably not empty, usually".** Use it
   only where "zero elements" really *is* an error (batch recipients,
   decomposed paths). Otherwise every caller pays a `.ToNonEmpty()` tax.

   ```csharp
   // Wrong — tags is naturally allowed to be empty.
   public record Article(string Title, NonEmptyEnumerable<string> Tags);

   // Right — an empty tag list is valid.
   public record Article(string Title, IReadOnlyList<string> Tags);
   ```

9. **Forgetting `.Unwrap()` in EF LINQ.** Equality / ordering / null
   checks on the wrapper translate. Anything using the *underlying*
   type's operators (`Contains`, arithmetic, `EF.Functions.*`) needs
   `.Unwrap()`.

   ```csharp
   // Doesn't translate — EF can't call string.StartsWith on a NonEmptyString.
   db.Users.Where(u => u.Name.StartsWith("ali"))

   // Right — .Unwrap() rewrites to a bare column reference for SQL.
   db.Users.Where(u => u.Name.Unwrap().StartsWith("ali"))
   ```
