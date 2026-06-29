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
- Implicit widening to the less-constrained variants (see below).

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

## Widening between variants

A more-constrained variant widens **implicitly** to a less-constrained one —
the conversion is total and lossless, so it never throws:

- `ClosedInterval<T>` → `IntervalFrom<T>`, `IntervalUntil<T>`, or `Interval<T>`
- `IntervalFrom<T>` → `Interval<T>`
- `IntervalUntil<T>` → `Interval<T>`

(There is no implicit `IntervalFrom` ↔ `IntervalUntil`: swapping which end is
open would require the optional end to be present, which isn't guaranteed.)

Because the conversion is implicit, this is also how you hold mixed variants in
one collection — widen them all to `Interval<T>`. These are distinct `struct`
types with no inheritance, but the operator runs on each element:

```csharp
Interval<int>[] windows =
[
    ClosedInterval<int>.Create(0, 10),   // widened
    IntervalFrom<int>.Create(5, null),   // widened
    Interval<int>.Create(null, null),
];

var matching = windows.Where(w => w.Contains(7));
```

`List<Interval<int>>` works the same way (`Add` takes `Interval<T>`). The
elements are stored inline as structs — no boxing. To recover "is this fully
bounded?" later, pattern-match the value (`w is ({ } s, { } e)`), which keys on
the actual endpoints rather than how the value was originally constructed.

Going the other way is **partial** — narrowing can fail when an endpoint that
the target requires is open — so it follows the library's `As…` convention and
returns a nullable, mirroring `TryCreate`:

| On | Method | `null` when |
| -- | ------ | ----------- |
| `Interval<T>` | `AsClosed()` → `ClosedInterval<T>?` | either endpoint open |
| `Interval<T>` | `AsFrom()` → `IntervalFrom<T>?` | lower endpoint open |
| `Interval<T>` | `AsUntil()` → `IntervalUntil<T>?` | upper endpoint open |
| `IntervalFrom<T>` | `AsClosed()` → `ClosedInterval<T>?` | upper endpoint open |
| `IntervalUntil<T>` | `AsClosed()` → `ClosedInterval<T>?` | lower endpoint open |

```csharp
Interval<int> i = Interval<int>.Create(1, 10);
ClosedInterval<int>? bounded = i.AsClosed();     // [1, 10]
ClosedInterval<int>? open    = IntervalFrom<int>.Create(1, null).AsClosed();   // null
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
