# Numeric wrappers — `Positive<T>`, `NonNegative<T>`, `Negative<T>`, `NonPositive<T>`

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

## Factories

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

## What you get for free on every wrapper

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

## Arithmetic

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

## Division helpers on `int` / `decimal`

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

## JSON

Every numeric wrapper carries a JSON converter that (de)serialises as the
underlying primitive. `Positive<int>` on the wire is `42`, not
`{ "Value": 42 }`. Invalid values (`0` for `Positive<int>`, `-1` for
`NonNegative<int>`, …) fail with `JsonException` at deserialization.

## Modelling tips

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
