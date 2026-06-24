# Interval types — `ClosedInterval<T>`, `Interval<T>`, `IntervalFrom<T>`, `IntervalUntil<T>`

Four generic `readonly struct` types holding an ordered pair of endpoints with
the invariant `Start <= End` (checked whenever both endpoints are present).
`T` is any `struct, IComparable<T>` — `int`, `long`, `decimal`, `DateTime`,
`DateOnly`, `TimeOnly`, and so on.

The variant fixes **which endpoints are bounded**, so the compiler — not a
runtime check — guarantees a bounded endpoint is never `null`. Pick the variant
by which ends are open:

| Type                | `Start` | `End` | Meaning                       |
| ------------------- | ------- | ----- | ----------------------------- |
| `ClosedInterval<T>` | `T`     | `T`   | bounded both ends             |
| `IntervalFrom<T>`   | `T`     | `T?`  | "from X" — open upper bound   |
| `IntervalUntil<T>`  | `T?`    | `T`   | "until Y" — open lower bound  |
| `Interval<T>`       | `T?`    | `T?`  | either / both ends optional   |

Every variant's `default` satisfies its invariant (`default(ClosedInterval<int>)`
is `[0, 0]`; `default(Interval<int>)` is unbounded).

## Factories

Same `TryCreate` / `Create` split as the rest of the library. They reject
`Start > End` (only when both endpoints are present — an open endpoint can't
violate ordering). Construct through the static factories; these types have no
`AsX` / `ToX` extensions because they take two arguments.

```csharp
ClosedInterval<int>?  r  = ClosedInterval<int>.TryCreate(1, 10);   // null if start > end
ClosedInterval<int>   r  = ClosedInterval<int>.Create(1, 10);      // throws if start > end

IntervalFrom<DateOnly>  fromToday = IntervalFrom<DateOnly>.Create(today, null);  // open-ended
IntervalUntil<DateOnly> untilXmas = IntervalUntil<DateOnly>.Create(null, xmas);  // open start
Interval<int>           anything  = Interval<int>.Create(null, null);            // unbounded
```

## What you get on every variant

- `.Start` / `.End` — typed per the variant (bounded endpoints are `T`,
  optional ones `T?`).
- `Contains(T value)` — membership; an open end is treated as unbounded
  in that direction.
- `Deconstruct` — enables pattern matching. For `Interval<T>` this gives the
  exhaustive four-arm switch over the bound cases.
- `IEquatable<Self>`, `==` / `!=`, `GetHashCode`, `Equals(object?)`.
- `ToString()` — interval notation, e.g. `[1, 10]`, `[1, +∞)`, `(-∞, 10]`,
  `(-∞, +∞)`.
- `System.Text.Json` converter via `[JsonConverter]`.

```csharp
var interval = Interval<DateTime>.Create(start, end);

string label = interval switch
{
    (null, null)         => "unbounded",
    (null, { } e)        => $"up to {e}",
    ({ } s, null)        => $"from {s}",
    ({ } s, { } e)       => $"{s}..{e}",
};

bool open = IntervalFrom<int>.Create(0, null).Contains(int.MaxValue);   // true
```

## JSON

Each interval serialises as an object with **both** keys always present; an
open endpoint is `null`:

```json
{ "Start": 1, "End": 10 }      // ClosedInterval<int>
{ "Start": 1, "End": null }    // IntervalFrom<int>, open-ended
{ "Start": null, "End": null } // Interval<int>, unbounded
```

Property names honour the active `JsonNamingPolicy` (camelCase under the web
defaults). A payload that violates the invariant — `Start > End`, a missing
key, or `null` for a bounded endpoint — fails with `JsonException` at
deserialization, which ASP.NET Core surfaces as a `400` keyed by the property
path (`$.value`).

## EF Core

`Kalicz.StrongTypes.EfCore` maps an interval to a single JSON column,
round-tripping through the validating converter:

```csharp
modelBuilder.Entity<Booking>()
    .HasIntervalJsonConversion(b => b.Window);
```

For a nullable interval property (`ClosedInterval<int>?`), map it with the
underlying converter directly —
`.Property(e => e.Window).HasConversion(new IntervalJsonValueConverter<ClosedInterval<int>>())`.

Don't reach for EF Core's `ComplexProperty` (two scalar columns): the interval's
private constructor can't be bound as a complex type, and column-by-column
materialization would skip the `Start <= End` validation the JSON converter
enforces.

## Modelling tips

Pick the variant that states the truth. A subscription that has started but
may run forever is `IntervalFrom<DateOnly>`, not `Interval<DateOnly>` with a
`Start` you have to remember is always set — the type already says so, and the
compiler stops you constructing it without a start.

```csharp
public record Subscription(
    NonEmptyString          Plan,
    IntervalFrom<DateOnly>  ActivePeriod);   // started; open-ended until cancelled
```
