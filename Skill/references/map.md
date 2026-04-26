# `T?.Map` and `bool.MapTrue` / `MapFalse`

Two tiny extensions that close the last ergonomics gap in nullable /
boolean composition. They mean you rarely need `Maybe<T>` or `Result<T>`
just to chain an optional computation.

## `T?.Map(...)`

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

## `bool.MapTrue` / `bool.MapFalse`

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

## Performance note

`Map` / `MapTrue` / `MapFalse` are slower than the equivalent ternary —
they go through a delegate invocation. On hot paths (tight loops,
per-element allocations), prefer the ternary. Everywhere else, prefer
these for readability.

## Decision guide

- You want "run this only if non-null / true / false, and produce another
  nullable" → `Map`, `MapTrue`, `MapFalse`.
- You want "run this unconditionally and handle null at call site" →
  plain method with `T?` parameter and `is not { } v` pattern.
- You want "two-state vs three-state tracking for an update-style field"
  (skip / clear / set — HTTP PATCH or any plain update DTO / service
  parameter) → `Maybe<T>?`, not `Map`.
